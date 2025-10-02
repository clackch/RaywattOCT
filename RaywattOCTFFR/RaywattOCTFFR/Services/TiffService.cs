using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using OpenCvSharp;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace RaywattOCTFFR.Services
{
    public class TiffService : ITiffService
    {
        public async Task<int> CountFramesAsync(string path, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                using var fs = File.OpenRead(path);
                var decoder = new TiffBitmapDecoder(
                    fs,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
                return decoder.Frames.Count;
            }, ct).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<(int Index, Mat Frame)> StreamEnumerableAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var fs = File.OpenRead(path);
            var decoder = new TiffBitmapDecoder(
                fs,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            for (int i = 0; i < decoder.Frames.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var src = PrepareBitmap(decoder.Frames[i]);
                using var mat = BitmapSourceToMat(src);
                yield return (i, mat.Clone()); // 소비측 Dispose
                await Task.Yield();            // UI 응답성 유지
            }
        }

        // 해상도/포맷 정규화
        private static BitmapSource PrepareBitmap(BitmapSource src)
        {
            if (src.Format != PixelFormats.Bgr24)
            {
                var conv = new FormatConvertedBitmap(src, PixelFormats.Bgr24, null, 0);
                conv.Freeze();
                src = conv;
            }

            return src;
        }

        // BitmapSource → Mat
        private static Mat BitmapSourceToMat(BitmapSource bs)
        {
            int w = bs.PixelWidth;
            int h = bs.PixelHeight;

            var m = new Mat(h, w, MatType.CV_8UC3);
            int step = (int)m.Step();
            bs.CopyPixels(Int32Rect.Empty, m.Data, step * h, step);
            return m;
        }
    }
}
