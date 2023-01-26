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

namespace RaywattApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            this.InitializeComponent();
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
            services.AddTransient(typeof(PatientListViewModel));
            services.AddTransient(typeof(PatientNewViewModel));
            services.AddTransient(typeof(PatientEditViewModel));
            services.AddTransient(typeof(PatientDetailViewModel));
            services.AddTransient(typeof(LiveViewViewModel));
            services.AddTransient(typeof(CalibrationViewModel));
            services.AddTransient(typeof(RecordingViewModel));
            services.AddTransient(typeof(RecordingSetupViewModel));
            services.AddTransient(typeof(ReviewViewModel));
            services.AddTransient(typeof(Review3dViewModel));
            services.AddTransient(typeof(ReviewCompareViewModel));
            services.AddTransient(typeof(ReviewFfrViewModel));
            services.AddTransient(typeof(ReviewPresetViewModel));
            services.AddTransient(typeof(ReviewAngioCoRegViewModel));

            //Setting
            services.AddTransient(typeof(SettingAcquisitionViewModel));
            services.AddTransient(typeof(SettingLocalizationViewModel));
            services.AddTransient(typeof(SettingDatabaseViewModel));
            services.AddTransient(typeof(SettingPhysicianViewModel));

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

            //IDatabaseService 등록 (Singleton 사용 안함 => Connection Pooling을 Default로 사용)
            services.AddTransient<IDatabaseService, SqlService>(obj => new SqlService(connectionString));
            services.AddTransient(typeof(SqlManager));

            return services.BuildServiceProvider();
        }
    }
}
