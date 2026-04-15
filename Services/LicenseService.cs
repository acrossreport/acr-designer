// ============================================================
// LicenseService.cs  ―  更新版
// acr_license_activate の引数が email + key の2つに変わった
// ============================================================

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AcrDesigner.Services;

public class LicenseService : ILicenseService
{
    private const string Lib = "axr_runtime";

    /// <summary>
    /// ローカルキャッシュを読んでライセンス状態を確認（ネット不要）
    /// 戻り値: 1=Licensed / 0=Watermarked
    /// </summary>
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int acr_license_check();

    /// <summary>
    /// サーバーに問い合わせて認証し、成功時はキャッシュ保存
    /// 戻り値: 1=成功 / 0=失敗
    /// </summary>
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int acr_license_activate(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

    /// <summary>最後のメッセージ（UTF-8）を返す</summary>
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr acr_license_last_message();

    // ── ILicenseService 実装 ─────────────────────────────────

    /// <summary>
    /// acr_license_check() 戻り値:
    ///   2 = DESIGNERPRO（ウォーターマークなし）
    ///   1 = Licensed / DESIGNER（ウォーターマークあり・デザイナー使用可）
    ///   0 = 未認証（ウォーターマークあり）
    /// </summary>
    public Task<LicenseCheckResult> CheckAsync() =>
        Task.Run(() =>
        {
            int result = acr_license_check();
            return result switch
            {
                2 => new LicenseCheckResult(IsLicensed: true,  Watermark: false, IsExpired: false), // Pro
                1 => new LicenseCheckResult(IsLicensed: true,  Watermark: true,  IsExpired: false), // Designer
                _ => new LicenseCheckResult(IsLicensed: false, Watermark: true,  IsExpired: false)  // 未認証
            };
        });

    public Task<LicenseActivateResult> ActivateAsync(string email, string key) =>
        Task.Run(() =>
        {
            int result = acr_license_activate(email, key);
            if (result == 1)
                return new LicenseActivateResult(IsLicensed: true, ErrorMessage: null);

            var ptr = acr_license_last_message();
            var msg = ptr != IntPtr.Zero
                ? Marshal.PtrToStringUTF8(ptr) ?? "不明なエラー"
                : "不明なエラー";
            return new LicenseActivateResult(IsLicensed: false, ErrorMessage: msg);
        });
}
