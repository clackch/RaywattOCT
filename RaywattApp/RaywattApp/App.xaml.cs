using Microsoft.Extensions.DependencyInjection;
using RaywattApp.Views.Controls;
using RaywattApp.Services;
using RaywattApp.ViewModels;
using RaywattApp.ViewModels.File;
using RaywattApp.ViewModels.Setting;
using System;
using System.Configuration;
using System.Windows;

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
            services.AddTransient(typeof(PatientDetailViewModel));
            services.AddTransient(typeof(RecordingViewModel));

            //Setting
            services.AddTransient(typeof(SettingAcquisitionViewModel));
            services.AddTransient(typeof(SettingLocalizationViewModel));

            //File
            services.AddTransient(typeof(FileExportStep1ViewModel));
            services.AddTransient(typeof(FileExportStep2ViewModel));
            services.AddTransient(typeof(FileImportViewModel));

            //Control 등록
            services.AddTransient(typeof(MessagePopupControl));
            services.AddTransient(typeof(SettingPopupControl));
            services.AddTransient(typeof(FilePopupControl));
            services.AddTransient(typeof(PatientEditPopupControl));

            //IDatabaseService 등록 (Singleton 사용 안함 => Connection Pooling을 Default로 사용)
            services.AddTransient<IDatabaseService, SqlService>(obj => new SqlService(connectionString));

            return services.BuildServiceProvider();
        }
    }
}
