using System;
using System.ComponentModel;
using System.Windows.Threading;
using RaywattOCT.Controller;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using System.IO;

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

        private const string ICON_RESOURCE_PLAY = "/res/icon/play.png";
        private const string ICON_RESOURCE_PLAY_OV = "/res/icon/play_ov.png";
        private const string ICON_RESOURCE_PAUSE = "/res/icon/pause.png";
        private const string ICON_RESOURCE_PAUSE_OV = "/res/icon/pause_ov.png";

        private string playIcon = ICON_RESOURCE_PLAY;
        public string PlayIcon {
            get { return playIcon; }
            set { playIcon = value; OnPropertyChanged(nameof(PlayIcon)); }
        }

        private string playIconOv = ICON_RESOURCE_PLAY_OV;
        public string PlayIconOv {
            get { return playIconOv; }
            set { playIconOv = value; OnPropertyChanged(nameof(PlayIconOv)); }
        }

        private bool isPaused;
        public bool IsPaused {
            get { return isPaused; }
            set {
                isPaused = value;
                PlayIcon = (isPaused) ? ICON_RESOURCE_PLAY : ICON_RESOURCE_PAUSE;
                PlayIconOv = (isPaused) ? ICON_RESOURCE_PLAY_OV : ICON_RESOURCE_PAUSE_OV;
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

        private int brightness;
        public int Brightness
        {
            get { return brightness; }
            set { brightness = value; OnPropertyChanged(nameof(Brightness)); setBrightnessContrast(); }
        }

        private int contrast;
        public int Contrast
        {
            get { return contrast; }
            set { contrast = value; OnPropertyChanged(nameof(Contrast)); setBrightnessContrast(); }
        }

        private string systemMessage = "Press Initialize Button";
        public string SystemMessage {
            get { return systemMessage; }
            set { systemMessage = value; OnPropertyChanged(nameof(SystemMessage)); }
        }

        private string reviewFileName = "OCT File";
        public string ReviewFileName 
        { 
            get { return reviewFileName; }
            set { reviewFileName = value; OnPropertyChanged(nameof(ReviewFileName)); }
        }

        private string patientName = "";
        public string PatientName {
            get { return patientName; }
            set { if (!CommonUtil.ValidateInput(value)) return; patientName = value; OnPropertyChanged(nameof(PatientName)); }
        }

        private bool requirePatientName = false;
        public bool RequirePatientName {
            get => requirePatientName;
            set { requirePatientName = value; OnPropertyChanged(nameof(RequirePatientName)); }
        }

        private string frameInfo = "";
        public string FrameInfo {
            get { return frameInfo; }
            set { frameInfo = value; OnPropertyChanged(nameof(FrameInfo)); }
        }

        private string viewMode = "";
        public string ViewMode
        {
            get { return viewMode; }
            set { viewMode = value; OnPropertyChanged(nameof(ViewMode)); }
        }

        private BitmapSource crossSectionImage = new BitmapImage(GetResourceURI(null, "res/bg/body_bg.png"));
        public BitmapSource CrossSectionImage {
            get { return crossSectionImage; }
            set { crossSectionImage = value; OnPropertyChanged(nameof(CrossSectionImage)); }
        }
        private Mat imgCrossSection;
        private int frameInformation;

        private BitmapSource longitudeImage = new BitmapImage(GetResourceURI(null, "res/bg/bottom_bg.png"));
        public BitmapSource LongitudeImage
        {
            get { return longitudeImage; }
            set { longitudeImage = value; OnPropertyChanged(nameof(LongitudeImage)); }
        }
        private Mat imgLongitude;

        private double pointerX;
        public double PointerX
        {
            get { return pointerX; }
            set { pointerX = value; OnPropertyChanged(nameof(PointerX)); }
        }
        private double pointerY;
        public double PointerY
        {
            get { return pointerY; }
            set { pointerY = value; OnPropertyChanged(nameof(PointerY)); }
        }

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

        private DelegateCommand cmdEndReview;
        public DelegateCommand CmdEndReview
        {
            get
            {
                return (this.cmdEndReview) ?? (this.cmdEndReview = new DelegateCommand(EndReview));
            }
        }

        private DelegateCommand cmdPlayback;
        public DelegateCommand CmdPlayback
        {
            get { return (this.cmdPlayback) ?? (this.cmdPlayback = new DelegateCommand(Playback)); }
        }

        private DelegateCommand cmdLoadCatheter;
        public DelegateCommand CmdLoadCatheter
        {
            get { return (this.cmdLoadCatheter) ?? (this.cmdLoadCatheter = new DelegateCommand(LoadCatheter)); }
        }

        private DelegateCommand cmdUnloadCatheter;
        public DelegateCommand CmdUnloadCatheter
        {
            get { return (this.cmdUnloadCatheter) ?? (this.cmdUnloadCatheter = new DelegateCommand(UnloadCatheter)); }
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
            getBrightnessContrast();
            updateMotorState();

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

                if (frameInformation == 0)
                {
                    FrameInfo = "";
                    ViewMode = "Live View";
                }
                else
                {
                    int curFrame = (frameInformation >> 16) & 0x00FFFF;
                    int totalFrame = (frameInformation) & 0x00FFFF;
                    FrameInfo = String.Format("{0} / {1}", curFrame + 1, totalFrame);
                    ViewMode = "Review";
                }
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
            RayCoreWrapper.RayMotorOnOff(!MotorOn);
            updateMotorState();
        }
        private void Scan()
        {
            PatientName = PatientName.Trim();
            if (PatientName.Length == 0) {
                RequirePatientName = true;
                RequirePatientName = false;
                return;
            }

            string filename = generateFileName("bin");
            RayCoreWrapper.RayError result = (RayCoreWrapper.RayError) RayCoreWrapper.RayPullbackScan(filename);

            if (result == RayCoreWrapper.RayError.OK)
            {
                ReviewFileName = filename;
            }
        }

        private void EndReview() {
            RayCoreWrapper.RayScannerState state = (RayCoreWrapper.RayScannerState) RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.CurrentState);

            if (state == RayCoreWrapper.RayScannerState.SaveDone) {
                if (!IsPaused)
                {
                    RayCoreWrapper.RayPlayPause();
                    updatePlayPauseState();
                }
            }

            RayCoreWrapper.RayError result = (RayCoreWrapper.RayError) RayCoreWrapper.RayEndReview();
            if (result == RayCoreWrapper.RayError.OK)
            {
                ReviewFileName = "OCT File";
            }
        }

        private void LoadCatheter() {
            RayCoreWrapper.RayLoadCatheter();
        }
        private void UnloadCatheter()
        {
            RayCoreWrapper.RayUnloadCatheter();
        }

        private void Playback(object param) {
            string action = (string)param;
            RayCoreWrapper.RayError result = RayCoreWrapper.RayError.OK;

            if (action.ToLower().Equals("prev"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayPrevFrame();
            }
            else if (action.ToLower().Equals("next"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayNextFrame();
            }
            else if (action.ToLower().Equals("play"))
            {
                result = (RayCoreWrapper.RayError)RayCoreWrapper.RayPlayPause();
                if (result == RayCoreWrapper.RayError.OK)
                {
                    updatePlayPauseState();
                }
            }
        }

        private void OnMsgCallback(int request, int response) {
            handleState((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayScannerState)response);
            handleProgress((RayCoreWrapper.RayCallbackRequest)request, response);
        }

        private void OnRecvCrossSection(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgCrossSection = imgRecv.Clone();
            frameInformation = frameInfo;
        }

        private void OnRecvLongitude(IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
        }

        private void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState response) {
            if (request != RayCoreWrapper.RayCallbackRequest.State) return;

            switch (response)
            {
                case RayCoreWrapper.RayScannerState.IntitializeFailed:
                    SystemMessage = "Initialize Failed";
                    break;
                case RayCoreWrapper.RayScannerState.Initializing:
                    SystemMessage = "Initializing..";
                    break;
                case RayCoreWrapper.RayScannerState.Homing:
                    SystemMessage = "Homing..";
                    break;
                case RayCoreWrapper.RayScannerState.Ready:
                    SystemMessage = "Ready";
                    break;
                case RayCoreWrapper.RayScannerState.LoadCatheter:
                    {
                        double loadCatheterTime = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.LoadCatheterTime);
                        SystemMessage = String.Format("Load Catheter in {0} sec", (int)loadCatheterTime);
                    }
                    break;
                case RayCoreWrapper.RayScannerState.Scanning:
                    SystemMessage = "Scanning..";
                    break;
                case RayCoreWrapper.RayScannerState.Review:
                    {
                        updatePlayPauseState();
                        SystemMessage = "Scan Done";
                    }
                    break;
                case RayCoreWrapper.RayScannerState.SaveDone:
                    break;
                default:
                    break;
            }
        }
        private void handleProgress(RayCoreWrapper.RayCallbackRequest request, int response)
        {
            if (request != RayCoreWrapper.RayCallbackRequest.Progress) return;

            int curFrame = (response >> 16) & 0x00FFFF;
            int totalFrame = (response) & 0x00FFFF;

            ScanProgress = (int) ((double) curFrame / (double) totalFrame) * 100;
        }

        private string generateFileName(string ext)
        {
            string filename = "";
            try
            {
                var parser = new IniParser.Parser.IniDataParser();
                var config = parser.Parse(File.ReadAllText(RayCoreWrapper.ConfigFilePath));
                string rootPath = config["Patient"]["RootPath"];

                string folderName = string.Format("{0}{1}", rootPath, PatientName);
                Directory.CreateDirectory(folderName);

                filename = string.Format("{0}\\{1}.{2}", folderName, DateTime.Now.ToString("yyyyMMdd_HHmmss"), ext);
            }
            catch (Exception e) { }

            return filename;
        }

        private void getBrightnessContrast()
        {
            double propBrightness = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.Brightness);
            double propContrast = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.Contrast);

            Brightness = (int)((propBrightness - RayCoreWrapper.BrightnessMin) / (RayCoreWrapper.BrightnessMax - RayCoreWrapper.BrightnessMin) * 100);
            Contrast = (int)((propContrast - RayCoreWrapper.ContrastMin) / (RayCoreWrapper.ContrastMax - RayCoreWrapper.ContrastMin) * 100);
        }

        private void setBrightnessContrast()
        {
            double propBrightness = ((double)Brightness / 100) * (RayCoreWrapper.BrightnessMax - RayCoreWrapper.BrightnessMin) + RayCoreWrapper.BrightnessMin;
            double propContrast = ((double)Contrast / 100) * (RayCoreWrapper.ContrastMax - RayCoreWrapper.ContrastMin) + RayCoreWrapper.ContrastMin;

            RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.Brightness, propBrightness);
            RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.Contrast, propContrast);
        }

        private void updateMotorState() {
            double motorState = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.MotorOnOff);
            MotorOn = (bool)(motorState != 0);
        }

        private void updatePlayPauseState()
        {
            double pauseState = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.IsPaused);
            IsPaused = (bool)(pauseState != 0);
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
