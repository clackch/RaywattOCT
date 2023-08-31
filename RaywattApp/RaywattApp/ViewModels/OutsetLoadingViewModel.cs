using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using static RaywattOCT.Ray3DWrapper;

namespace RaywattApp.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        public static Thread threadFuncLiveAngioImage;

        public static bool threadOnLiveAngioImage;

        [ObservableProperty]
        private double _progress;

        public OutsetLoadingViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("OutsetLoadingViewModel");

            Constants.CurrentPage = Constants.OutsetLoadingPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            // Terms and Contidions 확인
            IList<Configuration> tnCs = _sqlManager.SelectConfigurationTnC();
            if (tnCs != null || tnCs.Count == 1)
            {
                if ("N".Equals(tnCs[0].Value))
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["tnC"] = tnCs[0];
                    var result = _dialogService.OpenDialog(new TermsConditionsControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.No)
                    {
                        CommonUtil.Exit(DeviceStatus);
                    }
                }
            }

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(ProgressTest);
            timer.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (Progress >= 100)
            {
                IntPtr hWnd = new WindowInteropHelper(Constants.mainWindow).Handle;
                ODSOCT_CreateDll(hWnd);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.CutView, (int) Constants.CutView3dX, (int) Constants.CutView3dY, 
                    (int) Constants.CutView3dWidth, (int) Constants.CutView3dHeight);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.FlyThrough, (int)Constants.FlyThroughView3dX, (int)Constants.FlyThroughView3dY,
                    (int)Constants.FlyThroughView3dWidth, (int)Constants.FlyThroughView3dHeight);
                ODSOCT_StartRendering();

                timer.Stop();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));

                // Server On
                ProcessStartInfo psi = new ProcessStartInfo();
                Process p = new Process();
                psi.FileName = "C:\\github\\Sejong\\FrameGrabber\\FGServer\\x64\\Debug\\FGServer.exe";
                psi.CreateNoWindow = true;
                p.StartInfo = psi;
                p.Start();

                // Client On
                ConnectServer();
                threadOnLiveAngioImage = true;
                threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
                threadFuncLiveAngioImage.Start();
            }

            Progress += 0.5;
        }

        private void ThreadFuncLiveAngioImage()
        {
            while (threadOnLiveAngioImage)
            {
                DrawAngioImage();
            }
        }

        private void ConnectServer()
        {
            Array.Fill<byte>(TcpClientSingleton.tmpBuffer, 0);

            if(TcpClientSingleton.Instance.Connected == true)
                TcpClientSingleton.isConnected = true;
        }

        protected bool DrawAngioImage()
        {
            try
            {
                TcpClientSingleton.bytesRead = TcpClientSingleton.Instance.GetStream().Read(TcpClientSingleton.buffer, 0, 10000000);
                
                Array.Copy(TcpClientSingleton.buffer, 0, TcpClientSingleton.tmpBuffer, TcpClientSingleton.tmpBufferLen, TcpClientSingleton.bytesRead);
                TcpClientSingleton.tmpBufferLen += TcpClientSingleton.bytesRead;

                while (true)
                {
                    PacketType type = FrameGrabber.checkPacketType(TcpClientSingleton.tmpBuffer);
                    if (type == PacketType.Command)
                        commandPacketProcess();
                    else if (type == PacketType.Image)
                        imagePacketProcess();
                    else if (type == PacketType.Nothing)
                        break;
                }
            }
            catch (Exception ex)
            {
                TcpClientSingleton.isConnected = false;
                return false;
            }

            return true;
        }

        static byte CalcCheckSum(byte[] buffer, int size)
        {
            size--;
            byte csum = 0;
            for (; size >= 0; size--)
            {
                csum += buffer[size];
            }
            return (byte)~csum;
        }

        private void imagePacketProcess()
        {
            int offset = 2;
            short height = BitConverter.ToInt16(TcpClientSingleton.tmpBuffer, offset);
            offset += sizeof(short);
            short width = BitConverter.ToInt16(TcpClientSingleton.tmpBuffer, offset);
            offset += sizeof(short);
            char BitsPerPixel = (char)TcpClientSingleton.tmpBuffer[offset++];
            int imageSize = height * width * BitsPerPixel / 8;
            if (TcpClientSingleton.tmpBuffer[imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize - 2] == CalcCheckSum(TcpClientSingleton.tmpBuffer, offset + imageSize)
                && TcpClientSingleton.tmpBuffer[imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(height, width, MatType.CV_8UC(BitsPerPixel / 8));
                Marshal.Copy(TcpClientSingleton.tmpBuffer, offset, image.Data, imageSize);
                offset += imageSize;
                char checksum = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);
                char eof = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);

                Array.Copy(TcpClientSingleton.tmpBuffer, imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize, TcpClientSingleton.tmpBuffer, 0, 20000000 - imageSize - Sizes.imageHeaderSize - Sizes.imageTailSize);
                TcpClientSingleton.tmpBufferLen -= imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize;

                Cv2.Flip(image, image, 0);

                Mat imgRecv = CommonUtil.ByteMemoryToCvMat(image.Data, width, height, BitsPerPixel / 8);
                TcpClientSingleton.imgAngio = imgRecv;
            }
        }

        private void commandPacketProcess()
        {
            int offset = 2;
            int command = TcpClientSingleton.tmpBuffer[offset++];
            char checksum = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);
            char eof = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);

            if ((byte)checksum == CalcCheckSum(TcpClientSingleton.tmpBuffer, 3))
            {
            if (command == (int)CommandType.FGDisconnected)
            {
                TcpClientSingleton.portConnection = false;
                TcpClientSingleton.imgAngio = TcpClientSingleton.ShowNoSignal();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DeviceStatus.IsAngioConnected = TcpClientSingleton.portConnection;
                });
            }
            else if (command == (int)CommandType.FGConnected)
            {
                TcpClientSingleton.portConnection = true;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DeviceStatus.IsAngioConnected = TcpClientSingleton.portConnection;
                });
            }

            Array.Copy(TcpClientSingleton.tmpBuffer, Sizes.commandPacketSize, TcpClientSingleton.tmpBuffer, 0, 20000000 - Sizes.commandPacketSize);
            TcpClientSingleton.tmpBufferLen -= Sizes.commandPacketSize;
            }
        }
    }
}
