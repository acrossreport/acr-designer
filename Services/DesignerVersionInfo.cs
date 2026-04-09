using System.Reflection;

namespace AcrossReportDesigner.Services;

public sealed class DesignerVersionInfo
{
    public string DesignerName { get; set; } = "";
    public string DesignerVersion { get; set; } = "";

    // ================================
    // ✅ 実行中アセンブリから取得
    // ================================
    public static DesignerVersionInfo Create()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;

        return new DesignerVersionInfo
        {
            DesignerName = "AcrossReportDesigner",

            DesignerVersion = ver != null
                ? $"{ver.Major}.{ver.Minor}.{ver.Build}"
                : "0.0.0"
        };
    }
}
