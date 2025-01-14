import numpy as np
from PIL import Image
from scipy.spatial import KDTree
from concurrent.futures import ThreadPoolExecutor, as_completed
import os
import multiprocessing
import win32file
import shutil

# CPU 코어 수를 기반으로 max_workers 설정
def get_max_workers():
    total_cores = os.cpu_count() or multiprocessing.cpu_count()
    # I/O 바운드 작업의 경우 더 많은 워커 허용
    return min(32, total_cores * 2 + 4)

def add_color_weight_image(image_shape, colors):
    height, width, channels = image_shape
    color_height = height // len(colors)
    weighted_image = np.zeros((height, width, channels), dtype=np.uint8)
    for i, color in enumerate(colors):
        start_row = i * color_height
        end_row = start_row + color_height
        weighted_image[start_row:end_row, :] = color
    if height % len(colors) != 0:
        weighted_image[end_row:, :] = colors[-1]
    return weighted_image

def extract_colormap_from_samples(images):
    additional_colors = [
        (0x57, 0xFE, 0xEB), (0xFF, 0xD8, 0x00), (0xFF, 0x7D, 0x77),
        (0x76, 0x41, 0x3F), (0x1E, 0xD7, 0x60), (0x1B, 0x8E, 0x44),
        (0x13, 0x86, 0x3C), (0x15, 0x88, 0x3E), (0xB1, 0xB1, 0xB1)
    ]
    combined_image = np.vstack(images)
    weighted_image = add_color_weight_image(images[-1].shape, additional_colors)
    combined_image_with_weights = np.vstack([combined_image, weighted_image])

    combined_pil_image = Image.fromarray(combined_image_with_weights.astype(np.uint8))
    combined_8bit_image = combined_pil_image.convert("P", palette=Image.ADAPTIVE, colors=256)
    colormap = combined_8bit_image.getpalette()[:256 * 3]
    rgb_values = np.array(colormap).reshape(-1, 3)

    return rgb_values

def convert_24bit_to_8bit(image, tree, idx):
    reshaped_image = image.reshape(-1, 3)
    _, indices = tree.query(reshaped_image, workers=-1)
    
    send_progress_to_csharp("#" + str(idx))

    return indices.reshape(image.shape[:2]).astype(np.uint8)

def apply_palette(image, colormap):
    palette = [v for rgb in colormap for v in rgb]
    result = Image.fromarray(image, mode="P")
    result.putpalette(palette)
    return result

def process_images(py_list, width, height, output_tiff_path):
    send_progress_to_csharp("[START] Processing images...")

    # Python List 데이터를 NumPy 배열로 변환
    all_images = [np.array(mat_data, dtype=np.uint8).reshape((height, width, 3)) for mat_data in py_list]

    # TIFF 데이터와 동일한 차원으로 reshape (이미지를 3D로 처리)
    all_images = [img.reshape(img.shape[0], img.shape[1], -1) for img in all_images]

    # 샘플링된 이미지 선택
    step = max(1, len(all_images) // 10)
    sample_images = all_images[::step]

    # 샘플링된 이미지에서 컬러맵 생성
    rgb_values = extract_colormap_from_samples(sample_images)
    colormap = [tuple(rgb) for rgb in rgb_values]
    tree = KDTree(rgb_values)

    # 전체 이미지를 8비트로 변환
    converted_images = []
    max_workers = get_max_workers()
    send_progress_to_csharp(f"Converting all images to 8-bit...\nUsing max_workers={max_workers} based on system performance")

    # 저장할 결과를 인덱스와 함께 관리
    results = [None] * len(all_images)

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        # 인덱스를 포함하여 작업 제출
        future_to_index = {
            executor.submit(convert_24bit_to_8bit, img, tree, idx): idx
            for idx, img in enumerate(all_images)
        }

        # 완료된 작업 처리
        for future in as_completed(future_to_index):
            idx = future_to_index[future]
            try:
                results[idx] = future.result()
            except Exception as e:
                send_progress_to_csharp(f"[ERROR] Failed to process image at index {idx}: {e}")

    # 순서가 유지된 결과를 converted_images에 저장
    converted_images = results

    # 팔레트 적용 후 최종 TIFF 저장
    send_progress_to_csharp("[SAVE] Saving final TIFF...")
    pil_images = [apply_palette(img, colormap) for img in converted_images]
    pil_images[0].save(".\\temp.tif", save_all=True, append_images=pil_images[1:], compression="tiff_deflate")
    send_progress_to_csharp("[MOVE] Moving final TIFF...")
    shutil.move(".\\temp.tif", output_tiff_path)
    send_progress_to_csharp(f"[END] Final 8-bit TIFF saved to: {output_tiff_path}")

    return "success"

def send_progress_to_csharp(progress_message):
    pipe_name = r'\\.\pipe\progress_pipe'
    try:
        handle = win32file.CreateFile(
            pipe_name,
            win32file.GENERIC_WRITE,
            0,
            None,
            win32file.OPEN_EXISTING,
            0,
            None,
        )
        win32file.WriteFile(handle, f"{progress_message}\n".encode("utf-8"))
        win32file.CloseHandle(handle)
    except Exception as e:
        print(f"[ERROR] Failed to send progress to C#: {e}")
