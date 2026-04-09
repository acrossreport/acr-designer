// ViewModels/LicenseDialogViewModel.cs
using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcrDesigner.Services;

namespace AcrDesigner.ViewModels;

public partial class LicenseDialogViewModel : ObservableObject
{
    private readonly ILicenseService _license;
    public bool CanSkip { get; private set; }

    // ダイアログを閉じる指示（true=認証成功 / false=スキップ）
    public event EventHandler<bool>? CloseRequested;

    // ─── バインディングプロパティ ──────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanActivate))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanActivate))]
    private string _licenseKey = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanActivate))]
    [NotifyPropertyChangedFor(nameof(ActivateButtonText))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = string.Empty;

    [ObservableProperty]
    private IBrush _messageColor = Brushes.Red;

    public bool HasMessage  => !string.IsNullOrEmpty(Message);
    public bool CanActivate =>
        !IsBusy &&
        Email.Trim().Contains('@') &&
        LicenseKey.Trim().Length > 0;

    public string ActivateButtonText => IsBusy ? "認証中…" : "認証する";

    // ─── コンストラクタ ────────────────────────────────────

    public LicenseDialogViewModel(ILicenseService license, bool canSkip = true)
    {
        _license = license;
        CanSkip  = canSkip;
    }

    // ─── コマンド ──────────────────────────────────────────

    [RelayCommand]
    private async Task ActivateAsync()
    {
        IsBusy  = true;
        Message = string.Empty;

        try
        {
            var result = await _license.ActivateAsync(Email.Trim(), LicenseKey.Trim());

            if (result.IsLicensed)
            {
                MessageColor = Brushes.Green;
                Message      = "ライセンス認証に成功しました。";
                await Task.Delay(800);
                CloseRequested?.Invoke(this, true);
            }
            else
            {
                MessageColor = Brushes.OrangeRed;
                Message      = result.ErrorMessage ?? "認証に失敗しました。";
            }
        }
        catch (Exception ex)
        {
            MessageColor = Brushes.Red;
            Message      = $"エラー: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Skip()
    {
        CloseRequested?.Invoke(this, false);
    }
}
