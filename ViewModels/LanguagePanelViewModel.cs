using AcrDesigner.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AcrossReportDesigner.ViewModels
{
    public class LanguageItem
    {
        public string Code        { get; init; } = "";
        public string DisplayName { get; init; } = "";
    }

    public class LanguagePanelViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LanguageItem> SupportedLanguages { get; } = new()
        {
            new LanguageItem { Code = "ja", DisplayName = "日本語" },
            new LanguageItem { Code = "en", DisplayName = "English" },
            new LanguageItem { Code = "zh", DisplayName = "中文" },
            new LanguageItem { Code = "ko", DisplayName = "한국어" },
        };

        private LanguageItem _selectedLanguage;

        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();
                // UIスレッドが落ち着いてから切替
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    LocalizationManager.Instance.SwitchLanguage(value.Code);
                });
            }
        }

        public LanguagePanelViewModel()
        {
            var current = SettingsService.LoadLanguage() ?? "ja";
            _selectedLanguage = SupportedLanguages
                .FirstOrDefault(l => l.Code == current)
                ?? SupportedLanguages[0];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}