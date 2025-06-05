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
    
    send_progress_to_csharp("_converting_ " + str(idx))

    return indices.reshape(image.shape[:2]).astype(np.uint8)

def apply_palette(image, colormap):
    palette = [v for rgb in colormap for v in rgb]
    result = Image.fromarray(image, mode="P")
    result.putpalette(palette)
    return result

def move_file_with_progress(source_path, destination_path, buffer_size=1024 * 1024 * 16, min_update_interval=0.01):
    """
    파일 이동 중 진행 상태를 표시하는 함수 (작은 파일에서도 적절히 동작).

    Parameters:
        source_path (str): 원본 파일 경로.
        destination_path (str): 대상 파일 경로.
        buffer_size (int): 파일을 읽고 쓰는 버퍼 크기 (기본값: 16MB).
        min_update_interval (float): 진행 상태를 업데이트하는 최소 간격(초, 기본값: 1).
    """
    try:
        # 파일 크기 확인
        total_size = os.path.getsize(source_path)
        copied_size = 0

        # 복사 상태 변수 초기화
        last_update_progress = -0.01

        # 파일 복사
        with open(source_path, "rb") as src, open(destination_path, "wb") as dest:
            while chunk := src.read(buffer_size):
                dest.write(chunk)
                copied_size += len(chunk)

                # 진행 상태 계산
                progress = copied_size / total_size

                # 상태 업데이트 (변화가 1% 이상일 때만 출력)
                if progress - last_update_progress >= min_update_interval:
                    send_progress_to_csharp(f"_moving_ {progress:.2f}")
                    last_update_progress = progress

        # 복사 완료 후 원본 삭제
        os.remove(source_path)

    except Exception as e:
        send_progress_to_csharp(f"_error_ Error occurred: {e}")
        # 복사가 실패한 경우, 복사된 파일 삭제
        if os.path.exists(destination_path):
            os.remove(destination_path)

def process_images(py_list, width, height, output_tiff_path):
    send_progress_to_csharp("_start_ Processing images...")

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
    send_progress_to_csharp(f"_convert_ Converting all images to 8-bit... Using max_workers={max_workers} based on system performance")

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
                send_progress_to_csharp(f"_error_ Failed to process image at index {idx}: {e}")

    # 순서가 유지된 결과를 converted_images에 저장
    converted_images = results

    # 팔레트 적용 후 최종 TIFF 저장
    send_progress_to_csharp("_save_ Saving final TIFF...")
    pil_images = [apply_palette(img, colormap) for img in converted_images]
    pil_images[0].save(".\\temp.tif", save_all=True, append_images=pil_images[1:])
    send_progress_to_csharp("_move_ Moving final TIFF...")
    #shutil.move(".\\temp.tif", output_tiff_path)
    move_file_with_progress(".\\temp.tif", output_tiff_path)
    send_progress_to_csharp(f"_end_ Final 8-bit TIFF saved to: {output_tiff_path}")

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
        print(f"_error_ Failed to send progress to C#: {e}")
