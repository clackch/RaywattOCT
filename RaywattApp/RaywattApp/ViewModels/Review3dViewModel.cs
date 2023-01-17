using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattOCT;
using System;
using System.Collections.Generic;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class Review3dViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Review3dViewModel));

        public Review3dViewModel()
        {
            _log.Debug("Review3dViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.Review3dPage;
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
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError error)
        {
        }

        protected override void handleProgress(RayCoreWrapper.RayCallbackRequest request, int progress)
        {
        }

        protected override void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState state)
        {
        }

        protected override void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem work)
        {
        }
    }
}
