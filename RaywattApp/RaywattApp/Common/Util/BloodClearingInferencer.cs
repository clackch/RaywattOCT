using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using RaywattApp.Common.Bases;

public sealed class BloodClearingInferencer : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;

    private const int ImageSize = 224;

    // --- 재사용 리소스 (성능 핵심) ---
    private readonly Bitmap _workBitmap;                  // 224x224, 24bpp
    private readonly Graphics _g;                         // _workBitmap에 그리는 Graphics
    private readonly int _stride;                         // _workBitmap의 stride
    private readonly byte[] _pixelBuffer;                 // LockBits→Marshal.Copy용 버퍼
    private readonly float[] _inputBuffer;                // NCHW 입력 버퍼 (1*3*H*W)
    private readonly DenseTensor<float> _inputTensor;     // _inputBuffer를 감싼 텐서
    private readonly NamedOnnxValue[] _singleInput;       // Run 호출용 1원소 배열 (할당 절감)

    // 보간 방식(원 코드의 HighQualityBilinear 유지)
    private readonly InterpolationMode _interpolation = InterpolationMode.HighQualityBilinear;

    public BloodClearingInferencer(bool useCuda = true)
    {
        var options = new SessionOptions();
        try
        {
#if NET6_0_OR_GREATER
            if (useCuda) options.AppendExecutionProvider_CUDA();
#endif
        }
        catch { /* CUDA 실패시 CPU */ }

        _session = new InferenceSession(Constants.MlModelFolderPath + "\\auto_pullback\\autopullback.onnx", options);
        _inputName = _session.InputMetadata.Keys.FirstOrDefault() ?? "input";

        // --- 224x224 작업용 비트맵/Graphics 1회 생성 ---
        _workBitmap = new Bitmap(ImageSize, ImageSize, PixelFormat.Format24bppRgb);
        _g = Graphics.FromImage(_workBitmap);
        _g.CompositingMode = CompositingMode.SourceCopy;
        _g.CompositingQuality = CompositingQuality.HighQuality;
        _g.SmoothingMode = SmoothingMode.None;
        _g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        _g.InterpolationMode = _interpolation;

        // stride 측정 및 픽셀 버퍼 준비
        var rect = new Rectangle(0, 0, ImageSize, ImageSize);
        var data = _workBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try { _stride = data.Stride; }
        finally { _workBitmap.UnlockBits(data); }
        _pixelBuffer = new byte[_stride * ImageSize];

        // 입력 버퍼/텐서/입력배열 1회 생성
        _inputBuffer = new float[1 * 3 * ImageSize * ImageSize];
        _inputTensor = new DenseTensor<float>(_inputBuffer, new[] { 1, 3, ImageSize, ImageSize });
        _singleInput = new[] { NamedOnnxValue.CreateFromTensor(_inputName, _inputTensor) };
    }

    /// <summary>
    /// Mat(BGR) 프레임을 받아 추론 수행
    /// 반환: (PredictedIndex, Logits, 추론시간ms)
    ///  - PredictedIndex: 0 = Blood Present, 1 = Blood Cleared
    /// </summary>
    public (int PredictedIndex, float[] Logits, double ElapsedMs) Predict(Mat mat)
    {
        if (mat is null || mat.Empty())
            throw new ArgumentException("입력 Mat이 비어있습니다.", nameof(mat));

        var sw = Stopwatch.StartNew();

        // (옵션) 가운데 FoV 크롭
        if (Constants.MaximumFoV != Constants.DefaultFoV)
        {
            int width = mat.Width, height = mat.Height;
            double ratio = Constants.MaximumFoV / Constants.DefaultFoV;
            int cropW = (int)(width * ratio);
            int cropH = (int)(height * ratio);
            int cropX = (width - cropW) / 2;
            int cropY = (height - cropH) / 2;
            Rect roi = new Rect(cropX, cropY, cropW, cropH);
            mat = new Mat(mat, roi);
        }

        // Mat -> Bitmap (원본 색상 그대로)
        using var srcBmp = BitmapConverter.ToBitmap(mat);

        // 1) 8bit 단계에서 224x224 리사이즈 (GDI)
        _g.DrawImage(srcBmp,
            new Rectangle(0, 0, ImageSize, ImageSize),
            new Rectangle(0, 0, srcBmp.Width, srcBmp.Height),
            GraphicsUnit.Pixel);

        // 2) LockBits로 한 번에 바이트 복사 (unsafe 없음)
        var rect = new Rectangle(0, 0, ImageSize, ImageSize);
        var bmpData = _workBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            Marshal.Copy(bmpData.Scan0, _pixelBuffer, 0, _pixelBuffer.Length);
        }
        finally
        {
            _workBitmap.UnlockBits(bmpData);
        }

        // 3) byte → [0,1] → NCHW(_inputBuffer) 채우기
        // Format24bppRgb의 실제 메모리는 B,G,R 순서
        int hw = ImageSize * ImageSize;
        int rBase = 0 * hw, gBase = 1 * hw, bBase = 2 * hw;

        for (int y = 0; y < ImageSize; y++)
        {
            int rowBase = y * _stride;
            int posBase = y * ImageSize;
            for (int x = 0; x < ImageSize; x++)
            {
                int idx = rowBase + x * 3;
                float b = _pixelBuffer[idx + 0] / 255f;
                float g = _pixelBuffer[idx + 1] / 255f;
                float r = _pixelBuffer[idx + 2] / 255f;

                int pos = posBase + x;
                _inputBuffer[rBase + pos] = r;
                _inputBuffer[gBase + pos] = g;
                _inputBuffer[bBase + pos] = b;
            }
        }

        // 4) ONNX 추론 (추가 할당 없이)
        using var results = _session.Run(_singleInput);
        sw.Stop();

        float[] logits = results.First().AsTensor<float>().ToArray();
        int predictedIndex = Array.IndexOf(logits, logits.Max());

        return (predictedIndex, logits, sw.Elapsed.TotalMilliseconds);
    }

    public void Dispose()
    {
        _g?.Dispose();
        _workBitmap?.Dispose();
        _session?.Dispose();
    }
}
