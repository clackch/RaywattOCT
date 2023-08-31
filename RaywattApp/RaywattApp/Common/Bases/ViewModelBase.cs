using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Localization;
using RaywattApp.Models;
using System;
using System.Net.Sockets;
using OpenCvSharp;

namespace RaywattApp.Common.Bases
{
    /// <summary>
    /// ViewModelBase
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject, INavigationAware
    {
        protected readonly DynamicResource _l10n;

        [ObservableProperty]
        protected static DeviceStatus _deviceStatus = new DeviceStatus();

        public ViewModelBase()
        {
            _l10n = (DynamicResource)App.Current.Resources["L10N"];
        }

        /// <summary>
        /// Navigation 시작시 - 이동 시작하는 화면에서 발생
        /// </summary>
        public virtual void OnNavigating(object sender, object navigationEventArgs)
        {
        }

        /// <summary>
        /// Navigation 완료시 - 이동 완료된 화면에서 발생
        /// </summary>
        public virtual void OnNavigated(object sender, object navigatedEventArgs)
        {
        }
    }
    public sealed class TcpClientSingleton
    {
        static string serverIP = "127.0.0.1";
        static int serverPort = 8888; 
        public static bool isConnected = false; // Server - Client Connection
        public static bool portConnection = false; // FG Conenction
        public static byte[] buffer = new byte[10000000];
        public static byte[] tmpBuffer = new byte[20000000];
        public static int bytesRead;
        public static int tmpBufferLen = 0;
        public static byte[] startCommand = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGStarted, 0x07, 0xA3 };
        public static byte[] stopCommand = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGStopped, 0x07, 0xA3 };
        public static Mat imgAngio = ShowNoSignal();

        private static Lazy<TcpClient> lazyInstance = new Lazy<TcpClient>(() => new TcpClient(serverIP, serverPort));

        public static TcpClient Instance => lazyInstance.Value;

        public static Mat ShowNoSignal()
        {
            Mat image = new Mat(800, 1000, MatType.CV_8UC3);
            image.SetTo(new Scalar(0, 0, 0));

            Scalar textColor = new Scalar(0, 0, 255);
            HersheyFonts fontFace = HersheyFonts.HersheyComplex;
            double fontScale = 2.0;
            int thickness = 5;

            Point textPosition = new Point(300, 500);
            Cv2.PutText(image, "No Signal", textPosition, fontFace, fontScale, textColor, thickness);

            return image;
        }
    }
}
