using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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

        private DispatcherTimer timer = new DispatcherTimer();
        public MainViewModel()
        {
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timerUpdateTime);
            timer.Start();
        }

        private void timerUpdateTime(object sender, EventArgs e)
        {
            SystemDate = DateTime.Now.ToString("yyyy-MM-dd");
            SystemTime = DateTime.Now.ToString("HH:mm:ss");
        }
        private void Initialize()
        {
            Trace.WriteLine("Initialize");
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
    }
}
