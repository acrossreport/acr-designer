# ACR Designer

English | [日本語](README.ja.md) | [Français](README.fr.md)

ACR Designer is a desktop application for creating and editing ACR (AcrossReport) report definitions (JSON).

## Features

- Report design (placing and configuring bands and controls, paper size)
- Choose Section or Free Canvas when creating a new report
- Save and load templates as JSON (JSON created with [ACR Generator](https://github.com/acrossreport/acr-generator) can also be opened)
- Database connections: SQLite / SQL Server / PostgreSQL / MySQL / Oracle / Access (Access on Windows only)
- Preview and output to PDF, PNG, and HTML
- Japanese / English / Français

## System Requirements

| OS | Status |
|---|---|
| Windows x64 | Supported |
| Windows ARM64 | Supported |
| macOS (Apple Silicon) | Supported |
| macOS (Intel) | Supported |
| Linux x64 | Supported |

- Supported Windows versions: Windows 11
- The .NET runtime is included, so no separate installation is required
- macOS builds are signed with a Developer ID and notarized by Apple

## Download

Download the file for your OS from [Releases](https://github.com/acrossreport/acr-designer/releases).

- Windows x64: `AcrossReportDesigner-v0.1.2-win-x64.zip` (sample templates and data included)
- Windows ARM64: `AcrossReportDesigner-v0.1.2-win-arm64.zip` (sample templates and data included)
- macOS (Apple Silicon): `AcrossReportDesigner-v0.1.2-osx-arm64.zip`
- macOS (Intel): `AcrossReportDesigner-v0.1.2-osx-x64.zip`
- Linux x64: `AcrossReportDesigner-v0.1.2-linux-x64.zip`

> **Note (macOS):** Settings, templates, data, and output are stored under `~/Desktop/AcrossReportDesigner`. The macOS version does not yet include the sample files.
> **Note (Linux):** The Linux version does not yet include the sample files.

## Installation and Launch

1. Extract the downloaded zip to any folder
2. Windows: run `AcrossReportDesigner.exe` in the extracted folder. macOS/Linux: run `./AcrossReportDesigner` (on Linux you may need `chmod +x AcrossReportDesigner` first)

On first launch, the `Output` folder (`PDF`, `PNG`, `html`) and other folders are created automatically next to the executable (on macOS, under `~/Desktop/AcrossReportDesigner` instead).

## Usage

1. Create a new report (Section or Free Canvas) or open an existing template (JSON)
2. Place bands and controls and set their properties
3. Connect to a database if needed and preview with data
4. Output to PDF, PNG, or HTML (the `Output` folder next to the exe)
5. Save the template as JSON

## About Output

ACR Designer is intended for checking reports, so PDF and PNG output always includes a watermark, whether or not you are registered. For production printing and output, please use ACR Engine or ACR Viewer. See the [official website](https://acrossreport.com) for details.

## Links

- ACR Samples (sample templates/data, try it right away): https://github.com/acrossreport/acr-data-samples
- ACR Generator: https://github.com/acrossreport/acr-generator
- ACR specification (JSON template): https://github.com/acrossreport/acr-spec
- Official website: https://acrossreport.com

## License

The source code of this software is not publicly available. Please see [LICENSE](LICENSE) for the terms of use.

## Contact

across.support@gmail.com

---

© Across Systems Corporation
The intermediate drawing instruction architecture of ACR is patent pending.
