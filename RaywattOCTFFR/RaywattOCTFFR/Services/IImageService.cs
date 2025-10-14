using OpenCvSharp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaywattOCTFFR.Services
{
    public interface IImageService
    {
        Task<int> CountFramesAsync(string path, CancellationToken ct = default);

        IAsyncEnumerable<(int Index, Mat Frame)> StreamEnumerableAsync(string path, CancellationToken ct = default);
    }
}
