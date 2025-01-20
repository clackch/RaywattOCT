using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using OpenCvSharp;
using System.IO;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Angio;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Newtonsoft.Json;
using OpenCvSharp.WpfExtensions;
using RaywattApp.Common.Util;

namespace RaywattApp.ViewModels
{
    public partial class ReviewAngioCoRegViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewAngioCoRegViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private ICommand _cancleCommand;
        public ICommand CancelCommand
        {
            get { return this._cancleCommand ?? (this._cancleCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand(Ok)); }
        }

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(Reset)); }
        }

        private bool _isRendering = false;
        public bool IsRendering
        {
            get { return _isRendering; }
            set
            {
                _isRendering = value;
                OnPropertyChanged(nameof(IsRendering));
            }
        }
        public List<Mat> CrossSectionAngioImages { get; private set; }
        private List<ImageSource> crossSectionAngioImageSources { get; set; }
        public ImageSource CurrentAngioImage
        {
            get
            {
                if (crossSectionAngioImageSources != null && _angioFrameNumber >= 0 && _angioFrameNumber < crossSectionAngioImageSources.Count)
                {
                    return crossSectionAngioImageSources[_angioFrameNumber];
                }
                return null;
            }
        }

        private Point _crossSectionMousePosition;
        public Point CrossSectionMousePosition
        {
            get => _crossSectionMousePosition;
            set
            {
                _crossSectionMousePosition = value;
                OnPropertyChanged(nameof(CrossSectionMousePosition));
            }
        }

        private List<CoRegistration> _angioTrackPoints;
        public List<CoRegistration> AngioTrackPoints
        {
            get { return _angioTrackPoints; }
            set 
            {
                _angioTrackPoints = value; 
                OnPropertyChanged(nameof(AngioTrackPoints));
            }
        }

        private int _angioFrameNumber;
        public int AngioFrameNumber
        {
            get => _angioFrameNumber;
            set
            {
                _angioFrameNumber = value;
                OnPropertyChanged(nameof(AngioFrameNumber));
                OnPropertyChanged(nameof(CurrentAngioImage));
                AngioDisplayNumber = AngioFrameNumber + 1;
                OnPropertyChanged(nameof(AngioDisplayNumber));
            }
        }

        private int _angioDisplayNumber;
        public int AngioDisplayNumber
        {
            get => _angioDisplayNumber;
            set
            {
                _angioDisplayNumber = value;
            }
        }

        private int _angioFrameLength;
        public int AngioFrameLength
        {
            get => _angioFrameLength;
            set
            {
                _angioFrameLength = value;
                OnPropertyChanged(nameof(AngioFrameLength));
            }
        }

        private bool _isAngioTrackCompleted;
        public bool IsAngioTrackCompleted
        {
            get => _isAngioTrackCompleted;
            set
            {
                _isAngioTrackCompleted= value;
                OnPropertyChanged(nameof(IsAngioTrackCompleted));
            }
        }

        private bool _isReset;
        public bool IsReset
        {
            get => _isReset;
            set
            {
                _isReset = value;
                OnPropertyChanged(nameof(IsReset));
            }
        }

        private bool _isResetOn = true;
        public bool IsResetOn
        {
            get => _isResetOn;
            set
            {
                _isResetOn = value;
                OnPropertyChanged(nameof(IsResetOn));
            }
        }

        private bool _isCancel;
        public bool IsCancel
        {
            get => _isCancel;
            set
            {
                _isCancel = value;
                OnPropertyChanged(nameof(IsCancel));
            }
        }

        private List<DijkstraHeap> _dijkstraHeap;
        public List<DijkstraHeap> DijkstraHeap
        {
            get { return _dijkstraHeap; }
            set
            {
                _dijkstraHeap = value;
                OnPropertyChanged(nameof(DijkstraHeap));
            }
        }
        
        private bool _isOk;

        public bool IsOk
        {
            get => _isOk;
            set
            {
                _isOk= value;
                OnPropertyChanged(nameof(IsOk));
            }
        }

        public ReviewAngioCoRegViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewAngioCoRegViewModel");

            Constants.CurrentPage = Constants.ReviewAngioCoRegPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            CrossSectionAngioImages = new List<Mat>();
            crossSectionAngioImageSources = new List<ImageSource>();
            AngioTrackPoints = new List<CoRegistration>();
            DijkstraHeap = new List<DijkstraHeap>();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];

                for (int i = 0; i < PatientCase.AngioFrame.DijkstraHeap.Count; i++) DijkstraHeap.Add(PatientCase.AngioFrame.DijkstraHeap[i]);
                AngioFrameNumber = ReviewStatus.AngioFrameNumber;
                ReadAngioFrames();
                ReadTrackPoints();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Ok()
        {
            _log.Debug("Ok");
            IsOk = true;
            GoToPreviousPage(true);
        }

        private void Cancel()
        {
            _log.Debug("Cancel");
            GoToPreviousPage(false);
        }

        private void Reset()
        {
            _log.Debug("Reset");
            IsReset = true;
        }

        private void GoToPreviousPage(bool isSave)
        {
            _log.Debug("GoToPreviousPage");

            if (isSave)
            {
                PatientCase.AngioFrame.CoRegistration = AngioTrackPoints;
                SaveCoRegPoint();

                if (AngioTrackPoints[0].TrackPoint.Count != 0)
                {
                    UpdateCoRegStatus(true);
                }
                else
                {
                    UpdateCoRegStatus(false);
                }
            }

            ReviewStatus.AngioFrameNumber = AngioFrameNumber;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(ReviewStatus.CurrentPage) { Parameter = parameter });
        }

        private void SaveCoRegPoint()
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["co_registration"] = CommonUtil.CoRegistrationsToJson(AngioTrackPoints);
            int nRows = _sqlManager.UpsertCoRegistration(sqlParameters);
            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
        }

        private void UpdateCoRegStatus(bool angioCoRegSaved)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["angio_co_registration"] = angioCoRegSaved;
            int nRows = _sqlManager.UpdatePatientCaseAngioCoRegistration(sqlParameters);
            if (nRows == 0)
            {
                _log.Error("Update Error");
            }
        }

        private void ReadAngioFrames()
        {
            for (int i = 0; i < PatientCase.AngioFrame.AngioImage.Count; i++)
            {
                crossSectionAngioImageSources.Add(PatientCase.AngioFrame.AngioImage[i]);

                if (PatientCase.AngioFrame.AngioImage[i] is BitmapSource bitmapSource)
                {
                    CrossSectionAngioImages.Add(bitmapSource.ToMat());
                }
            }
            AngioFrameLength = crossSectionAngioImageSources.Count - 1;
        }

        private void ReadTrackPoints()
        {
            int cnt = 0;
            foreach (CoRegistration coReg in PatientCase.AngioFrame.CoRegistration)
            {
                DijkstraHeap[cnt].line = new List<List<Point>>(coReg.Line);
                DijkstraHeap[cnt].trackPoint = new List<Point>(coReg.TrackPoint);
                cnt++;
            }
        }
    }
}
