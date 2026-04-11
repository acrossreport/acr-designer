using AcrDesigner.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        // ✅ 日本語・英語のみサポート
        public ObservableCollection<LanguageItem> SupportedLanguages { get; } = new()
        {
            new LanguageItem { Code = "ja", DisplayName = "日本語" },
            new LanguageItem { Code = "en", DisplayName = "English" },
        };

        private LanguageItem _selectedLanguage;
        private bool _initialized = false;

        public LanguageItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();

                if (!_initialized) return;

                // ✅ 設定を保存してから再起動フローをリクエスト
                SettingsService.SaveLanguage(value.Code);
                RestartRequested?.Invoke(value.Code);
            }
        }

        // ✅ MainWindow が購読して再起動フローを実行する
        public event Action<string>? RestartRequested;

        public LanguagePanelViewModel()
        {
            var saved = SettingsService.LoadLanguage() ?? "ja";
            _selectedLanguage = SupportedLanguages
                .FirstOrDefault(l => l.Code == saved)
                ?? SupportedLanguages[0];

            // 起動時のみ即時適用（ダイアログなし）
            LocalizationManager.Instance.SwitchLanguage(_selectedLanguage.Code);
            _initialized = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
