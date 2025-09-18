using log4net;
using Microsoft.Extensions.DependencyInjection;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.ViewModels;
using RaywattOCTFFR.ViewModels.Admin;
using RaywattOCTFFR.ViewModels.Dialog;
using RaywattOCTFFR.ViewModels.File;
using RaywattOCTFFR.ViewModels.Password;
using RaywattOCTFFR.ViewModels.Setting;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;

namespace RaywattOCTFFR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        public App()
        {
            _log.Debug("App");

            //Setting Working Directory
            Process process = Process.GetCurrentProcess();
            Environment.CurrentDirectory = Path.GetDirectoryName(process.MainModule.FileName);

            Services = ConfigureServices();
            this.InitializeComponent();

            SetupExceptionHandling();
            this.SessionEnding += SessionEndingCancelEventHandler;
            
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.ManipulationBoundaryFeedbackEvent, new EventHandler<ManipulationBoundaryFeedbackEventArgs>((s, e) => e.Handled = true));
        }

        private void SessionEndingCancelEventHandler(object sender, SessionEndingCancelEventArgs e)
        {
            _log.Debug("SessionEndingCancelEventHandler");

            CommonUtil.Exit();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static ServiceProvider ConfigureServices()
        {
            _log.Debug("ConfigureServices");

            var services = new ServiceCollection();

            string connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;

            //ViewModel 등록
            services.AddTransient(typeof(MainViewModel));
            services.AddTransient(typeof(OutsetLoginViewModel));
            services.AddTransient(typeof(OutsetLoadingViewModel));
            services.AddTransient(typeof(PatientListViewModel));
            services.AddTransient(typeof(PatientEditViewModel));
            services.AddTransient(typeof(PatientDetailViewModel));
            services.AddTransient(typeof(ReviewViewModel));
            services.AddTransient(typeof(ReviewFfrSettingViewModel));
            services.AddSingleton(typeof(ReviewFfrViewModel));
            services.AddTransient(typeof(ReviewPresetViewModel));
            services.AddTransient(typeof(ReviewLumenEditViewModel));
            services.AddTransient(typeof(ReviewCalibrationViewModel));
            services.AddTransient(typeof(InitialPasswordSetupViewModel));
            services.AddTransient(typeof(PasswordExpiryCheckViewModel));

            //Setting
            services.AddTransient(typeof(SettingDatabaseViewModel));
            services.AddTransient(typeof(SettingAboutViewModel));
            services.AddTransient(typeof(SettingLogViewModel));
            services.AddTransient(typeof(SettingTermsConditionsViewModel));
            services.AddTransient(typeof(SettingPasswordChangeViewModel));

            //File
            services.AddTransient(typeof(FileImportViewModel));

            //Dialog 등록
            services.AddTransient<IDialogService, DialogService>();
            services.AddTransient(typeof(AlertDialogViewModel));
            services.AddTransient(typeof(AlertTimerDialogViewModel));
            services.AddTransient(typeof(ConfirmDialogViewModel));
            services.AddTransient(typeof(EditCaseInfoDialogViewModel));
            services.AddTransient(typeof(SettingDialogViewModel));
            services.AddTransient(typeof(FileDialogViewModel));
            services.AddTransient(typeof(FileFolderBrowseDialogViewModel));
            services.AddTransient(typeof(FileFolderActionDialogViewModel));
            services.AddTransient(typeof(FileImportDialogViewModel));
            services.AddTransient(typeof(FileAlternateIdDialogViewModel));
            services.AddTransient(typeof(TermsConditionsDialogViewModel));
            services.AddTransient(typeof(PowerOffDialogViewModel));
            services.AddTransient(typeof(PasswordChangeDialogViewModel));
            services.AddTransient(typeof(EditInstituteDialogViewModel));

            //Admin
            services.AddTransient(typeof(UserListViewModel));
            services.AddTransient(typeof(UserNewViewModel));
            services.AddTransient(typeof(UserEditViewModel));

            //IDatabaseService 등록 (Singleton 사용 안함 => Connection Pooling을 Default로 사용)
            services.AddTransient<IDatabaseService, SqlService>(obj => new SqlService(connectionString));
            services.AddTransient(typeof(SqlManager));

            services.AddTransient<IPasswordService, PasswordService>();

            services.AddTransient(typeof(IdleMonitorService));

            return services.BuildServiceProvider();
        }

        private void SetupExceptionHandling()
        {
            _log.Debug("SetupExceptionHandling");

            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private static void LogUnhandledException(Exception exception, string source)
        {
            _log.Debug("LogUnhandledException");

            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                string appInfo = $"{assemblyName.Name} v{assemblyName.Version}";

                string detailedError = $"[Unhandled Exception]\n" +
                    $"Source     : {source}\n" +
                    $"App        : {appInfo}\n" +
                    $"Message    : {exception.Message}\n" +
                    $"Type       : {exception.GetType()}\n" +
                    $"TargetSite : {exception.TargetSite}\n" +
                    $"StackTrace : \n" +
                    $"{exception.StackTrace}";

                // InnerException 파고들기 (있으면)
                if (exception.InnerException != null)
                {
                    detailedError += $"-- Inner Exception --\n" +
                        $"Type       : {exception.InnerException.GetType()}\n" +
                        $"Message    : {exception.InnerException.Message}\n" +
                        $"TargetSite : {exception.InnerException.TargetSite}\n" +
                        $"StackTrace : \n" +
                        $"{exception.InnerException.StackTrace}";
                }

                _log.Error(detailedError);
            }
            catch (Exception logEx)
            {
                _log.Fatal($"Exception in LogUnhandledException: {logEx}");
            }
        }

    }
}
