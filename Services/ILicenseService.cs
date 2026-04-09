// Services/ILicenseService.cs

using System.Threading.Tasks;

namespace AcrDesigner.Services;

public record LicenseCheckResult(bool IsLicensed, bool Watermark, bool IsExpired);
public record LicenseActivateResult(bool IsLicensed, string? ErrorMessage);

public interface ILicenseService
{
    Task<LicenseCheckResult> CheckAsync();
    Task<LicenseActivateResult> ActivateAsync(string email, string key);
}
