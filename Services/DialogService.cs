using AcrossReportDesigner.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Threading.Tasks;

namespace AcrossReportDesigner.Services;

public static class DialogService
{
    public static async Task ShowMessageAsync(
        Control owner,
        string message,
        string title)
    {
        var dlg = new MessageDialog();
        dlg.Title = title;
        dlg.SetMessage(message);

        var ownerWindow =
            owner as Window
            ?? owner.GetVisualRoot() as Window;

        if (ownerWindow != null)
            await dlg.ShowDialog(ownerWindow);
        else
            dlg.Show();
    }

    // ============================
    // ✅ あなたの ConfirmDialog 仕様に合わせた版
    // ============================

    public static async Task<bool> ShowConfirmAsync(
        Control owner,
        string message,
        string title)
    {
        var ownerWindow =
            owner as Window
            ?? owner.GetVisualRoot() as Window;

        if (ownerWindow == null)
            return false;

        return await ConfirmDialog.Show(ownerWindow, message);
    }
}
