using FellowOakDicom.Imaging;
using FellowOakDicom;
using OpenCvSharp;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace RaywattOCTFFR.Services
{
    public class DicomService : IDicomService
    {
        public async Task<int> CountFramesAsync(string path, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var dicom = DicomFile.Open(path);
                var px = DicomPixelData.Create(dicom.Dataset);
                return px.NumberOfFrames;
            }, ct).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<(int Index, Mat Frame)> StreamEnumerableAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var dicom = DicomFile.Open(path);
            var ds = dicom.Dataset;
            var px = DicomPixelData.Create(ds);

            int w = px.Width;
            int h = px.Height;

            for (int i = 0; i < px.NumberOfFrames; i++)
            {
                ct.ThrowIfCancellationRequested();

                var frame = px.GetFrame(i);
                var bytes = frame.Data;

                // 안전한 행 단위 복사로 Mat 생성
                Mat mat = CopyFrameToMat(bytes, w, h, px.BitsAllocated, px.SamplesPerPixel);

                // Photometric MONOCHROME1이면 반전
                var pi = ds.GetSingleValueOrDefault(DicomTag.PhotometricInterpretation, "MONOCHROME2");
                if (pi == "MONOCHROME1") Cv2.BitwiseNot(mat, mat);

                // 색상 DICOM인 경우 RGB→BGR (표시/후처리 일관성 위해)
                if (px.SamplesPerPixel == 3)
                {
                    var bgr = new Mat();
                    Cv2.CvtColor(mat, bgr, ColorConversionCodes.RGB2BGR);
                    mat.Dispose();
                    yield return (i, bgr);
                }
                else
                {
                    yield return (i, mat);
                }

                await Task.Yield();
            }
        }

        // frame 바이트 → Mat으로 “행 단위” 복사
        private static Mat CopyFrameToMat(byte[] bytes, int w, int h, int bitsAllocated, int samplesPerPixel)
        {
            // bytesPerSample: 12bit 같은 케이스도 고려하여 ceil 나눗셈
            int bytesPerSample = (bitsAllocated + 7) / 8; // 8->1, 12->2, 16->2
            int channels = samplesPerPixel;
            int rowBytes = w * bytesPerSample * channels;
            int expected = rowBytes * h;

            if (bytes.Length != expected)
                throw new InvalidOperationException($"Size mismatch. expected={expected}, actual={bytes.Length}");

            MatType type;
            if (channels == 1)
                type = (bytesPerSample == 2) ? MatType.CV_16UC1 : MatType.CV_8UC1;
            else if (channels == 3)
                type = MatType.CV_8UC3; // fo-dicom GetFrame은 보통 8bit RGB interleaved
            else
                throw new NotSupportedException($"Unsupported pixel format: ch={channels}, bits={bitsAllocated}");

            var mat = new Mat(h, w, type);

            // 연속 메모리라도 행 단위로 안전 복사
            for (int y = 0; y < h; y++)
            {
                IntPtr dst = mat.Ptr(y);
                Marshal.Copy(bytes, y * rowBytes, dst, rowBytes);
            }

            return mat;
        }

    }
}
