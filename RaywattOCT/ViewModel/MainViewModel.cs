using System;
using System.ComponentModel;
using System.Windows.Threading;
using RaywattOCT.Controller;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using System.IO;
using System.Windows;

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
        private const int IMAGE_BACKGROUND_COLOR = 0x161518;

        private const string TEST_FILE_PATH = "C:\\DataSave\\test\\0710_145631_6028rpm_20mms_2000Aline_ch1.bin";

        private const int nCrossSectionHeight = 800;
        private const int nCrossSectionWidth = 860;
        private const int nLModeWidth = 820;
        private const int nLModeIndicatorWidth = 3;

        private string playIcon = ICON_RESOURCE_PLAY;
        public string PlayIcon
        {
            get { return playIcon; }
            set { playIcon = value; OnPropertyChanged(nameof(PlayIcon)); }
        }

        private string playIconOv = ICON_RESOURCE_PLAY_OV;
        public string PlayIconOv
        {
            get { return playIconOv; }
            set { playIconOv = value; OnPropertyChanged(nameof(PlayIconOv)); }
        }

        private bool isPaused;
        public bool IsPaused
        {
            get { return isPaused; }
            set
            {
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

        private double degree = 90;
        public double Degree
        {
            get { return degree; }
            set { degree = value; OnPropertyChanged(nameof(Degree)); RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.LongitudeDegree, degree); }
        }

        private string systemMessage = "Press Initialize Button";
        public string SystemMessage
        {
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
        public string PatientName
        {
            get { return patientName; }
            set { if (!CommonUtil.ValidateInput(value)) return; patientName = value; OnPropertyChanged(nameof(PatientName)); }
        }

        private bool requirePatientName = false;
        public bool RequirePatientName
        {
            get => requirePatientName;
            set { requirePatientName = value; OnPropertyChanged(nameof(RequirePatientName)); }
        }

        private string frameInfo = "";
        public string FrameInfo
        {
            get { return frameInfo; }
            set { frameInfo = value; OnPropertyChanged(nameof(FrameInfo)); }
        }

        private string viewMode = "";
        public string ViewMode
        {
            get { return viewMode; }
            set { viewMode = value; OnPropertyChanged(nameof(ViewMode)); }
        }

        private BitmapSource crossSectionImage = null;
        public BitmapSource CrossSectionImage
        {
            get { return crossSectionImage; }
            set { crossSectionImage = value; OnPropertyChanged(nameof(CrossSectionImage)); }
        }
        private Mat imgCrossSection;

        private RayCoreWrapper.FrameInfo crossSectionFrameInfo;
        private RayCoreWrapper.FrameInfo longitudeFrameInfo;

        private BitmapSource longitudeImage = null;
        public BitmapSource LongitudeImage
        {
            get { return longitudeImage; }
            set { longitudeImage = value; OnPropertyChanged(nameof(LongitudeImage)); }
        }
        private Mat imgLongitude;

        private bool bCaptured = false;

        private double pointerX;
        public double PointerX
        {
            get { return pointerX; }
            set { if (value.Equals(pointerX)) return; pointerX = value; OnPropertyChanged(nameof(PointerX)); }
        }
        private double pointerY;
        public double PointerY
        {
            get { return pointerY; }
            set { if (value.Equals(pointerY)) return; pointerY = value; OnPropertyChanged(nameof(PointerY)); }
        }


        private bool bLModeCaptured = false;

        private double lModePointerX;
        public double LModePointerX
        {
            get { return lModePointerX; }
            set { if (value.Equals(lModePointerX)) return; lModePointerX = value; OnPropertyChanged(nameof(lModePointerX)); }
        }

        private double lModeLocationX;
        public double LModeLocationX
        {
            get { return lModeLocationX; }
            set { if (value.Equals(lModeLocationX)) return; lModeLocationX = value; OnPropertyChanged(nameof(lModeLocationX)); }
        }

        private string isVisibleIndicator = "Hidden";
        public string IsVisibleIndicator
        {
            get { return isVisibleIndicator; }
            set { isVisibleIndicator = value; OnPropertyChanged(nameof(isVisibleIndicator)); }
        }

        private string isVisibleNavigator = "Hidden";
        public string IsVisibleNavigator
        { 
            get { return isVisibleNavigator; }
            set { isVisibleNavigator = value; OnPropertyChanged(nameof(isVisibleNavigator)); }
        }

        public DelegateCommand moveIndicator;
        public DelegateCommand MoveIndicator
        {
            get
            {
                return (this.moveIndicator) ?? (this.moveIndicator = new DelegateCommand(calculateDegree));
            }
        }
        public DelegateCommand captureTrue;
        public DelegateCommand CaptureTrue
        {
            get
            {
                return (this.captureTrue) ?? (this.captureTrue = new DelegateCommand(captureSetTrue));
            }
        }

        public DelegateCommand captureFalse;
        public DelegateCommand CaptureFalse
        {
            get
            {
                return (this.captureFalse) ?? (this.captureFalse = new DelegateCommand(captureSetFalse));
            }
        }

        public DelegateCommand moveNavigator;
        public DelegateCommand MoveNavigator
        {
            get
            {
                return (this.moveNavigator) ?? (this.moveNavigator = new DelegateCommand(calculateNavigatorPosition));
            }
        }

        public DelegateCommand lModeCaptureTrue;
        public DelegateCommand LModeCaptureTrue
        {
            get
            {
                return (this.lModeCaptureTrue) ?? (this.lModeCaptureTrue = new DelegateCommand(lModeCaptureSetTrue));
            }
        }

        public DelegateCommand lModeCaptureFalse;
        public DelegateCommand LModeCaptureFalse
        {
            get
            {
                return (this.lModeCaptureFalse) ?? (this.lModeCaptureFalse = new DelegateCommand(lModeCaptureSetFalse));
            }
        }

        private DelegateCommand cmdInitialize;
        public DelegateCommand CmdInitialize
        {
            get
            {
                return (this.cmdInitialize) ?? (this.cmdInitialize = new DelegateCommand(Initialize));
            }
        }

        private DelegateCommand cmdAdmin;
        public DelegateCommand CmdAdmin
        {
            get
            {
                return (this.cmdAdmin) ?? (this.cmdAdmin = new DelegateCommand(Admin));
            }
        }

        private DelegateCommand cmdFFR;
        public DelegateCommand CmdFFR
        {
            get 
            {
                return (this.cmdFFR) ?? (this.cmdFFR = new DelegateCommand(FFR));
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

        private Window3DRender window3D;
        private Raywatt3DRenderer renderer;
        private IntPtr hWnd3D;

        public MainViewModel()
        {
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timerUpdateTime);
            timer.Start();

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(5);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();

            RayCoreWrapper.RayStartSystem();
            RayCoreWrapper.RayRegisterCallback(Marshal.GetFunctionPointerForDelegate(CBFunction));
            RayCoreWrapper.RayRegisterImageCallback(
                Marshal.GetFunctionPointerForDelegate(CBCrossSection),
                Marshal.GetFunctionPointerForDelegate(CBLongitude));

            RayCoreWrapper.RaySetProperty(RayCoreWrapper.Property.LongitudeBackgroundColor, IMAGE_BACKGROUND_COLOR);
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

                if (crossSectionFrameInfo.totalFrame == 0)
                {
                    FrameInfo = "";
                    ViewMode = "Live View";
                }
                else
                {
                    FrameInfo = String.Format("{0} / {1}", crossSectionFrameInfo.curFrame + 1, crossSectionFrameInfo.totalFrame);                    
                    ViewMode = "Review";

                    if(!bLModeCaptured) updateNavigator(crossSectionFrameInfo.curFrame, crossSectionFrameInfo.totalFrame);
                }
            }
            if (imgLongitude != null)
            {
                RayCoreWrapper.RayScannerState state = (RayCoreWrapper.RayScannerState)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.CurrentState);

                if (state == RayCoreWrapper.RayScannerState.Review)
                {
                    LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
                    if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame) updateNavigatorVisibility(true);
                }
                else { 
                    LongitudeImage = new BitmapImage(GetResourceURI(null, "res/bg/bottom_bg.png"));
                }
            }
            if (renderer != null) {
                renderer.Render();
            }
        }
        private void Initialize()
        {
            RayCoreWrapper.RayConnectDevices();

            getBrightnessContrast();
            updateMotorState();

            ScanProgress = 0;
        }
        private void Admin()
        {
            RayCoreWrapper.RayStartReview(TEST_FILE_PATH);
        }

        private void FFR() { 
            
        }

        private void Exit()
        {
            RayCoreWrapper.RayStopSystem();
            RayCoreWrapper.RayDisconnectDevices();
            if (window3D != null)
            {
                window3D.Close();
            }
            if (renderer != null)
            {
                renderer.Dispose();
            }
            Environment.Exit(0);
        }
        private void MotorOnOff()
        {
            if (MotorOn)
            {
                RayCoreWrapper.RayStopLiveView();
            }
            else {
                RayCoreWrapper.RayStartLiveView();
            }
            updateMotorState();
        }
        private void Scan()
        {
            PatientName = PatientName.Trim();
            if (PatientName.Length == 0)
            {
                RequirePatientName = true;
                RequirePatientName = false;
                return;
            }

            string filename = generateFileName("bin");
            RayCoreWrapper.RayError result = (RayCoreWrapper.RayError)RayCoreWrapper.RayPullbackScan(filename);

            if (result == RayCoreWrapper.RayError.OK)
            {
                ReviewFileName = filename;
            }
        }

        private void EndReview()
        {
            RayCoreWrapper.RayScannerState state = (RayCoreWrapper.RayScannerState)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.CurrentState);

            if (state == RayCoreWrapper.RayScannerState.Review)
            {
                if (!IsPaused)
                {
                    RayCoreWrapper.RayPlayPause();
                    updatePlayPauseState();
                }
            }

            RayCoreWrapper.RayError result = (RayCoreWrapper.RayError)RayCoreWrapper.RayEndReview();
            if (result == RayCoreWrapper.RayError.OK)
            {
                ReviewFileName = "OCT File";
            }
        }

        private void LoadCatheter()
        {
            RayCoreWrapper.RayLoadCatheter();
        }
        private void UnloadCatheter()
        {
            RayCoreWrapper.RayUnloadCatheter();
        }

        private void Playback(object param)
        {
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

        private void OnMsgCallback(int request, int response)
        {
            handleState((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayScannerState)response);
            handleProgress((RayCoreWrapper.RayCallbackRequest)request, response);
            handleError((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayError)response);
            handleWorkDone((RayCoreWrapper.RayCallbackRequest)request, (RayCoreWrapper.RayWorkItem)response);
        }

        private void OnRecvCrossSection(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgCrossSection = imgRecv.Clone();
            crossSectionFrameInfo = new RayCoreWrapper.FrameInfo(frameInfo);
        }

        private void OnRecvLongitude(int session, IntPtr data, int width, int height, int ch, int frameInfo)
        {
            Mat imgRecv = byteMemoryToCvMat(data, width, height, ch);
            imgLongitude = imgRecv.Clone();
            longitudeFrameInfo = new RayCoreWrapper.FrameInfo(frameInfo);
        }

        private void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState response)
        {
            if (request != RayCoreWrapper.RayCallbackRequest.State) return;

            updateIndicatorVisibility(false);
            updateNavigatorVisibility(false);

            switch (response)
            {
                case RayCoreWrapper.RayScannerState.Initial:
                    break;
                case RayCoreWrapper.RayScannerState.Default:
                    break;
                case RayCoreWrapper.RayScannerState.Scanning:
                    SystemMessage = "Scanning..";
                    break;
                case RayCoreWrapper.RayScannerState.Review:
                    RayCoreWrapper.RayShowCalibrationGuide(true);
                    updatePlayPauseState();
                    updateIndicatorVisibility(true);
                    SystemMessage = "Review";
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

            ScanProgress = (int)((double)curFrame / (double)totalFrame) * 100;
        }

        private void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError response) 
        {
            if (request != RayCoreWrapper.RayCallbackRequest.Error) return;

            switch (response) {
                case RayCoreWrapper.RayError.DeviceNotConnected:
                    SystemMessage = "Devices Not Connected";
                    break;
                default:
                    break;
            }
        }

        private void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem response)
        {
            if (request != RayCoreWrapper.RayCallbackRequest.WorkDone) return;

            switch (response)
            {
                case RayCoreWrapper.RayWorkItem.GenerateVolume:
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (window3D == null)
                            {
                                window3D = new Window3DRender();
                                window3D.Show();
                                hWnd3D = new System.Windows.Interop.WindowInteropHelper(window3D).Handle;
                            }

                            int volumeWidth = (int)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.VolumeWidth);
                            int volumeHeight = (int)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.VolumeHeight);
                            int volumeDepth = (int)RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.VolumeDepth);

                            Raywatt3DRenderer _3drenderer = new Raywatt3DRenderer();
                            _3drenderer.Init((int)window3D.RenderSize.Width, (int)window3D.RenderSize.Height, hWnd3D);

                            Raywatt3DRenderer.CreateVolumeData(volumeWidth, volumeHeight, volumeDepth, RayCoreWrapper.RayGetVolumeData());
                            renderer = _3drenderer;
                        }));                        
                    }
                    break;
                default:
                    break;
            }
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

        private void updateMotorState()
        {
            double motorState = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.MotorOnOff);
            MotorOn = (bool)(motorState != 0);
        }

        private void updatePlayPauseState()
        {
            double pauseState = RayCoreWrapper.RayGetProperty(RayCoreWrapper.Property.IsPaused);
            IsPaused = (bool)(pauseState != 0);
        }

        private void updateIndicatorVisibility(bool onOff)
        {
            if (onOff)
                IsVisibleIndicator = "Visible";
            else
                IsVisibleIndicator = "Hidden";                
        }

        private void updateNavigatorVisibility(bool onOff)
        {
            if (onOff)
                IsVisibleNavigator = "Visible";
            else
                IsVisibleNavigator = "Hidden";
        }

        private void updateNavigator(int curFrame, int totalFrame) {
            double curPosition = (double)curFrame / totalFrame;
            curPosition = (curFrame == totalFrame - 1) ? 1 : curPosition;
            curPosition *= nLModeWidth;
            LModeLocationX = curPosition + nLModeIndicatorWidth / 2;
        }

        private void setCurrentFrame(double navigatorPosition) {
            double curPosition = (navigatorPosition + nLModeIndicatorWidth / 2) / (double)nLModeWidth;

            if (longitudeFrameInfo != null) {
                curPosition *= longitudeFrameInfo.totalFrame;
                RayCoreWrapper.RayMoveToFrame((int) curPosition);
            }
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

        private void captureSetTrue()
        {
            bCaptured = true;
        }

        private void captureSetFalse()
        {
            if (bCaptured)
                bCaptured = false;
        }

        private void calculateDegree()
        {
            if (bCaptured)
            {
                double pointX = nCrossSectionWidth/2 - PointerX;
                double pointY = nCrossSectionHeight/2 - PointerY;
                Degree = (int)(Math.Atan2(pointY, pointX) * 180 / Math.PI);
            }
        }

        private void lModeCaptureSetTrue()
        {
            if (IsPaused)
            {
                bLModeCaptured = true;
            }
        }

        private void lModeCaptureSetFalse()
        {
            bLModeCaptured = false;
        }

        private void calculateNavigatorPosition()
        {
            if (bLModeCaptured && LModePointerX >= 0)
            {
                //indicator bar width(3), add 1.5
                LModeLocationX = LModePointerX + nLModeIndicatorWidth/2;
                setCurrentFrame(LModeLocationX);
            }
        }
    }
}
