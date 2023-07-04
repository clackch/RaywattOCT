using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using System;
using System.Windows.Threading;

namespace RaywattApp.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private double _progress;

        public OutsetLoadingViewModel()
        {
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(ProgressTest);
            timer.Start();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (Progress >= 100)
            {
                timer.Stop();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }

            Progress += 0.5;
        }
    }
}
