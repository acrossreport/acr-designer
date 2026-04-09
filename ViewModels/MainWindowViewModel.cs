using System.ComponentModel;
using AcrDesigner.Services;
using AcrDesigner.ViewModels;

namespace AcrossReportDesigner.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public LanguagePanelViewModel LanguagePanel { get; } = new LanguagePanelViewModel();

        private readonly LicenseService _licenseService;
        private bool _watermarkEnabled;

        public MainWindowViewModel(LicenseService licenseService, bool watermarkEnabled)
        {
            _licenseService = licenseService;
            _watermarkEnabled = watermarkEnabled;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}