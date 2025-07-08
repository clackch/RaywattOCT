using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using System.Collections.Generic;
using System;
using RaywattApp.Common.Util;
using static RaywattOCT.Ray3DWrapper;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class Review3dViewMenuViewModel : ModelessViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dViewMenuViewModel));

        private bool isTissueOn;
        public bool IsTissueOn { 
            get { return isTissueOn; } 
            set { isTissueOn = value; OnPropertyChanged(nameof(IsTissueOn)); updateVisibility(Ray3DObject.Tissue, value); }
        }

        private bool isLumenOn;
        public bool IsLumenOn
        {
            get { return isLumenOn; }
            set { isLumenOn = value; OnPropertyChanged(nameof(IsLumenOn)); updateVisibility(Ray3DObject.Lumen, value); }
        }

        private bool isStentOn;
        public bool IsStentOn
        {
            get { return isStentOn; }
            set { isStentOn = value; OnPropertyChanged(nameof(IsStentOn)); updateVisibility(Ray3DObject.Stent, value); }
        }

        private bool isPostCase;
        public bool IsPostCase
        {
            get { return isPostCase; }
            set { isPostCase = value; OnPropertyChanged(nameof(isPostCase));}
        }

        private bool isGuidewireOneOn;
        public bool IsGuidewireOneOn
        {
            get { return isGuidewireOneOn; }
            set { isGuidewireOneOn = value; OnPropertyChanged(nameof(IsGuidewireOneOn)); updateVisibility(Ray3DObject.GuideWire, value); }
        }

        private bool isGuidewireTwoOn;
        public bool IsGuidewireTwoOn
        {
            get { return isGuidewireTwoOn; }
            set { isGuidewireTwoOn = value; OnPropertyChanged(nameof(IsGuidewireTwoOn)); updateVisibility(Ray3DObject.GuideWire2, value); }
        }

        [ObservableProperty]
        private PatientCase _patientCase;

        public Review3dViewMenuViewModel()
        {
            IsTissueOn = ray3DStatus.IsObjectVisible(Ray3DObject.Tissue);
            IsLumenOn = ray3DStatus.IsObjectVisible(Ray3DObject.Lumen);
            IsStentOn = ray3DStatus.IsObjectVisible(Ray3DObject.Stent);
            IsGuidewireOneOn = ray3DStatus.IsObjectVisible(Ray3DObject.GuideWire);
            IsGuidewireTwoOn = ray3DStatus.IsObjectVisible(Ray3DObject.GuideWire2);
        }

        private static void updateVisibility(Ray3DObject obj, bool visible)
        {

            Ray3DObjectMode mode = (visible) ? ((ray3DStatus.CutViewOn) ? Ray3DObjectMode.Cut : Ray3DObjectMode.Full) : Ray3DObjectMode.Hide;
            ray3DStatus.ShowObject(obj, mode);
            int ray3DResult = ODSOCT_Render();
            if (ray3DResult == 0)
            {
                _log.Error("ODSOCT_Render Error");
            }
        }

        public override void SetParameter(IModelessPatient parent, object parameter)
        {
            _log.Debug("3D public override void SetParameter(IModelessPatient parent, object parameter)");
            Parent = parent;
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            PatientCase = (PatientCase)data["patientCase"];
            updateStentToggleButton();
        }

        public void updateStentToggleButton()
        {
            if (CommonUtil.IsPostCase(PatientCase.Procedure))
            {
                isPostCase = true;
            }
            else
            {
                isPostCase = false;
            }
        }
    }
}
