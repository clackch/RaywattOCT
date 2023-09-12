using Microsoft.Extensions.DependencyInjection;
using RaywattApp.Services;
using RaywattApp.ViewModels;
using RaywattApp.ViewModels.File;
using RaywattApp.ViewModels.Setting;
using System;
using System.Configuration;
using System.Windows;
using RaywattApp.ViewModels.Dialog;
using RaywattApp.Common.Dialog;
using System.Threading.Tasks;
using log4net;
using System.Diagnostics;
using System.IO;

namespace RaywattApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        public App()
        {
            //Setting Working Directory
            Process process = Process.GetCurrentProcess();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(process.MainModule.FileName));

            Services = ConfigureServices();
            this.InitializeComponent();

            SetupExceptionHandling();
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
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            string connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;

            //ViewModel 등록
            services.AddTransient(typeof(MainViewModel));
            services.AddTransient(typeof(OutsetLoadingViewModel));
            services.AddTransient(typeof(PatientListViewModel));
            services.AddTransient(typeof(PatientNewViewModel));
            services.AddTransient(typeof(PatientEditViewModel));
            services.AddTransient(typeof(PatientDetailViewModel));
            services.AddTransient(typeof(RecordingLiveViewViewModel));
            services.AddTransient(typeof(RecordingCalibrationViewModel));
            services.AddTransient(typeof(RecordingViewModel));
            services.AddTransient(typeof(RecordingConfirmViewModel));
            services.AddTransient(typeof(RecordingSetupViewModel));
            services.AddTransient(typeof(RecordingCatheterFailViewModel));
            services.AddTransient(typeof(ReviewViewModel));
            services.AddTransient(typeof(Review3dViewModel));
            services.AddTransient(typeof(ReviewCompareViewModel));
            services.AddTransient(typeof(ReviewFfrViewModel));
            services.AddTransient(typeof(ReviewPresetViewModel));
            services.AddTransient(typeof(ReviewAngioCoRegViewModel));
            services.AddTransient(typeof(ReviewLumenEditViewModel));
            services.AddTransient(typeof(ReviewCalibrationViewModel));

            //Setting
            services.AddTransient(typeof(SettingAcquisitionViewModel));
            services.AddTransient(typeof(SettingLocalizationViewModel));
            services.AddTransient(typeof(SettingDatabaseViewModel));
            services.AddTransient(typeof(SettingPhysicianViewModel));
            services.AddTransient(typeof(SettingServiceViewModel));
            services.AddTransient(typeof(SettingLogViewModel));
            services.AddTransient(typeof(SettingTermsConditionsViewModel));

            //File
            services.AddTransient(typeof(FileExportStep1ViewModel));
            services.AddTransient(typeof(FileExportStep2NativeViewModel));
            services.AddTransient(typeof(FileExportStep2DicomViewModel));
            services.AddTransient(typeof(FileExportStep2StandardViewModel));
            services.AddTransient(typeof(FileImportViewModel));

            //Dialog 등록
            services.AddTransient<IDialogService, DialogService>();
            services.AddTransient(typeof(AlertDialogViewModel));
            services.AddTransient(typeof(ConfirmDialogViewModel));
            services.AddTransient(typeof(EditCaseInfoDialogViewModel));
            services.AddTransient(typeof(SettingDialogViewModel));
            services.AddTransient(typeof(FileDialogViewModel));
            services.AddTransient(typeof(FileFolderBrowseDialogViewModel));
            services.AddTransient(typeof(FileFolderActionDialogViewModel));
            services.AddTransient(typeof(FileImportDialogViewModel));
            services.AddTransient(typeof(FileCopyDialogViewModel));
            services.AddTransient(typeof(FileAlternateIdDialogViewModel));
            services.AddTransient(typeof(FileExportDialogViewModel));
            services.AddTransient(typeof(SettingEditPhysicianDialogViewModel));
            services.AddTransient(typeof(TermsConditionsDialogViewModel));
            services.AddTransient(typeof(Review3dViewMenuViewModel));
            services.AddTransient(typeof(Review3dPatientMenuViewModel));
            services.AddTransient(typeof(PowerOffDialogViewModel));

            //IDatabaseService 등록 (Singleton 사용 안함 => Connection Pooling을 Default로 사용)
            services.AddTransient<IDatabaseService, SqlService>(obj => new SqlService(connectionString));
            services.AddTransient(typeof(SqlManager));

            return services.BuildServiceProvider();
        }

        private void SetupExceptionHandling()
        {
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

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("{0} in {1} v{2}", message, assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                _log.Error(ex + "Exception in LogUnhandledException");
            }
            finally
            {
                _log.Error(exception + "\n" + message);
            }
        }
    }
}
