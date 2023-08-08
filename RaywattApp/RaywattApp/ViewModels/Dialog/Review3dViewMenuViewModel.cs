using log4net;
using RaywattApp.Common.Dialog;
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

        public Review3dViewMenuViewModel()
        {
            IsTissueOn = ray3DStatus.IsObjectVisible(Ray3DObject.Tissue);
            IsLumenOn = ray3DStatus.IsObjectVisible(Ray3DObject.Lumen);
            IsStentOn = ray3DStatus.IsObjectVisible(Ray3DObject.Stent);
            IsGuidewireOneOn = ray3DStatus.IsObjectVisible(Ray3DObject.GuideWire);
            IsGuidewireTwoOn = ray3DStatus.IsObjectVisible(Ray3DObject.GuideWire2);
        }

        private void updateVisibility(Ray3DObject obj, bool visible)
        {

            Ray3DObjectMode mode = (visible) ? ((ray3DStatus.CutViewOn) ? Ray3DObjectMode.Cut : Ray3DObjectMode.Full) : Ray3DObjectMode.Hide;
            ray3DStatus.ShowObject(obj, mode);
        }

        public override void SetParameter(IModelessPatient parent, object parameter)
        {
            Parent = parent;
        }
    }
}
