using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;

namespace RayCoreWrapper
{
    public class Raywatt3DRenderer
    {
        // Declare the device and swapChain vars
        private Device? device;
        private SwapChain? swapChain;
        private Texture2D? backBuffer;
        private RenderTargetView? renderTargetView;

        [DllImport("Raywatt3DRenderer.dll")]
        public static extern void InitializeDevice(IntPtr ptrDevice, IntPtr ptrContext, IntPtr hWnd, int widht, int height);
        [DllImport("Raywatt3DRenderer.dll")]
        public static extern void FinalizeDevice();
        [DllImport("Raywatt3DRenderer.dll")]
        public static extern void RenderVolumeData(IntPtr ptrDevice, IntPtr ptrContext, IntPtr ptrRenderTargetView);
        [DllImport("Raywatt3DRenderer.dll")]
        public static extern void CreateVolumeData(int width, int height, int frames, IntPtr data);

        public void Init(int width, int height, IntPtr hWnd)
        {
            // Create the device and swapchain
            Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DeviceCreationFlags.None,
                new[] {
            SharpDX.Direct3D.FeatureLevel.Level_11_1,
            SharpDX.Direct3D.FeatureLevel.Level_11_0,
            SharpDX.Direct3D.FeatureLevel.Level_10_1,
            SharpDX.Direct3D.FeatureLevel.Level_10_0,
                },
                new SwapChainDescription()
                {
                    ModeDescription =
                        new ModeDescription(
                            width,
                            height,
                            new Rational(60, 1),
                            //Format.R8G8B8A8_UNorm
                            Format.R8G8B8A8_UNorm_SRgb
                        ),
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = SharpDX.DXGI.Usage.BackBuffer | Usage.RenderTargetOutput,
                    BufferCount = 2,
                    Flags = SwapChainFlags.None,
                    IsWindowed = true,
                    OutputHandle = hWnd,
                    SwapEffect = SwapEffect.Discard,
                },
                out device, out swapChain
            );

            // Create references for backBuffer and renderTargetView
            backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(device, backBuffer);

            InitializeDevice(((IntPtr)device), ((IntPtr)device.ImmediateContext), hWnd, width, height);
        }

        public void Render()
        { 
            // Execute rendering commands here...
            RenderVolumeData(((IntPtr)device), ((IntPtr)device?.ImmediateContext), ((IntPtr)renderTargetView));

            // Present the frame
            swapChain?.Present(0, PresentFlags.None);
        }

        public void Dispose()
        {
            renderTargetView?.Dispose();
            backBuffer?.Dispose();
            swapChain?.Dispose();
            device?.Dispose();

            FinalizeDevice();
        }
    }
}
