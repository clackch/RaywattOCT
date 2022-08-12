using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using RaywattOCT.Controller;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace RaywattOCT.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool motorOn;
        public bool MotorOn
        {
            get { return motorOn; }
            set { motorOn = value; OnPropertyChanged(nameof(MotorOn)); }
        }

        private string systemDate = DateTime.Now.ToString("yyyy-MM-dd");
        public string SystemDate
        {
            get { return systemDate; }
            set { systemDate = value; OnPropertyChanged(nameof(SystemDate)); }
        }

        private string systemTime = DateTime.Now.ToString("HH:mm:ss");
        public string SystemTime
        {
            get { return systemTime; }
            set { systemTime = value; OnPropertyChanged(nameof(SystemTime)); }
        }

        private int scanProgress = 0;
        public int ScanProgress
        {
            get { return scanProgress; }
            set { scanProgress = value; OnPropertyChanged(nameof(ScanProgress)); }
        }

        private string systemMessage = "Press Initialize Button";
        public string SystemMessage { 
            get { return systemMessage; }
            set { systemMessage = value; OnPropertyChanged(nameof(SystemMessage)); }
        }

        private BitmapSource crossSectionImage = new BitmapImage(GetResourceURI(null, "res/bg/body_bg.png"));
        public BitmapSource CrossSectionImage { 
            get { return crossSectionImage; }
            set { crossSectionImage = value; OnPropertyChanged(nameof(CrossSectionImage)); }
        }
        private Mat imgCrossSection;

        private BitmapSource longitudeImage = new BitmapImage(GetResourceURI(null, "res/bg/bottom_bg.png"));
        public BitmapSource LongitudeImage
        {
            get { return longitudeImage; }
            set { longitudeImage = value; OnPropertyChanged(nameof(LongitudeImage)); }
        }
        private Mat imgLongitude;

        private DelegateCommand cmdInitialize;
        public DelegateCommand CmdInitialize 
        {
            get
            {
                return (this.cmdInitialize) ?? (this.cmdInitialize = new DelegateCommand(Initialize));
            }
        }

        private DelegateCommand cmdExit;
        public DelegateCommand CmdExit
        {
            get
            {
                return (this.cmdExit) ?? (this.cmdExit = new DelegateCommand(Exit));
            }
        }

        private DelegateCommand cmdMotorOnOff;
        public DelegateCommand CmdMotorOnOff
        {
            get
            {
                return (this.cmdMotorOnOff) ?? (this.cmdMotorOnOff = new DelegateCommand(MotorOnOff));
            }
        }

        private DelegateCommand cmdScan;
        public DelegateCommand CmdScan
        {
            get
            {
                return (this.cmdScan) ?? (this.cmdScan = new DelegateCommand(Scan));
            }
        }

        // to avoid garbage collection
        private RayCoreWrapper.CallbackFunction cbFunction;
        public RayCoreWrapper.CallbackFunction CBFunction => (this.cbFunction) ?? (this.cbFunction = new RayCoreWrapper.CallbackFunction(OnMsgCallback));

        private RayCoreWrapper.CallbackFunctionWithImage cbCrossSection;
        public RayCoreWrapper.CallbackFunctionWithImage CBCrossSection => (this.cbCrossSection) ?? (this.cbCrossSection = new RayCoreWrapper.CallbackFunctionWithImage(OnRecvCrossSection));

        private RayCoreWrapper.CallbackFunctionWithImage cbLongitude;
        public RayCoreWrapper.CallbackFunctionWithImage CBLongitude => (this.cbLongitude) ?? (this.cbLongitude = new RayCoreWrapper.CallbackFunctionWithImage(OnRecvLongitude));

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        public MainViewModel()
        {
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timerUpdateTime);
            timer.Start();

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();
        }

        private void timerUpdateTime(object sender, EventArgs e)
        {
            SystemDate = DateTime.Now.ToString("yyyy-MM-dd");
            SystemTime = DateTime.Now.ToString("HH:mm:ss");
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);
            }
            if (imgLongitude != null) { 
                LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
            }
        }
        private void Initialize()
        {
            RayCoreWrapper.RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            RayCoreWrapper.RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));

            RayCoreWrapper.RayInitialize();

            ScanProgress = 0;
        }
        private void Exit()
        {
            Environment.Exit(0);
        }
        private void MotorOnOff()
        {
            MotorOn = !MotorOn;
            Trace.Write(((MotorOn) ? "On" : "Off"), "Motor");
        }
        private void Scan() {
            RayCoreWrapper.RayPullbackScan();

            try
            {
                var bw = new BackgroundWorker();
                bw.DoWork += (sender, args) =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        ScanProgress += 1;
                        Thread.Sleep(10);
                    }
                };
                bw.RunWorkerCompleted += (sender, args) => { };
                bw.RunWorkerAsync();
            }
            catch (Exception e) {
                Trace.WriteLine(e.StackTrace.ToString());
            }
        }

        private void OnMsgCallback(int request, int response) {
            string message = String.Format("Request {0}, Response {1}", request, response);
            SystemMessage = message;
        }

        private void OnRecvCrossSection(IntPtr data, int width, int height, int ch)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgCrossSection = imgRecv.Clone();
        }

        private void OnRecvLongitude(IntPtr data, int width, int height, int ch)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
        }

        private Mat byteMemoryToCvMat(IntPtr data, int width, int height, int ch)
        {
            int byteLength = width * height * ch;
            byte[] imgData = new byte[byteLength];
            Marshal.Copy(data, imgData, 0, byteLength);

            return new Mat(height, width, MatType.CV_8UC3, data);
        }

        public static Uri GetResourceURI(string assemblyName, string resourcePath)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                return new Uri(string.Format("pack://application:,,,/{0}", resourcePath));
            }
            else
            {
                return new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyName, resourcePath));
            }
        }
    }
}
