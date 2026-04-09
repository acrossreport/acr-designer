// Views/LicenseDialog.axaml.cs
using Avalonia.Controls;
using AcrDesigner.ViewModels;

namespace AcrDesigner.Views;


public partial class LicenseDialog : Window
{
    private bool _isClosing = false;  // ← 追加

    public LicenseDialog()
    {
        InitializeComponent();
    }

    public LicenseDialog(LicenseDialogViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += (_, result) => Close(result);

        Closing += (_, e) =>
        {
            if (_isClosing) return;  // ← 再帰防止
            _isClosing = true;

            if (!vm.CanSkip)
            {
                e.Cancel = true;
                _isClosing = false;
            }
            else
            {
                Close(false);
            }
        };
    }
}
