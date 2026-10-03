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
| Windows x64 | Supported (this release) |
| macOS (Apple Silicon) | Planned |
| macOS (Intel) | Planned |
| Linux x64 | Planned |

- Supported Windows versions: Windows 11
- The .NET runtime is included, so no separate installation is required

## Download

Download the file for your OS from [Releases](https://github.com/acrossreport/acr-designer/releases).

- Windows x64: `AcrossReportDesigner-v0.0.1-win-x64.zip`

## Installation and Launch

1. Extract the downloaded zip to any folder
2. Run `AcrossReportDesigner.exe` in the extracted folder

On first launch, the `Output` folder (`PDF`, `PNG`, `html`) and other folders are created automatically next to the exe.

## Usage

1. Create a new report (Section or Free Canvas) or open an existing template (JSON)
2. Place bands and controls and set their properties
3. Connect to a database if needed and preview with data
4. Output to PDF, PNG, or HTML (the `Output` folder next to the exe)
5. Save the template as JSON

## About Output

ACR Designer is intended for checking reports, so PDF and PNG output always includes a watermark, whether or not you are registered. For production printing and output, please use ACR Engine or ACR Viewer. See the [official website](https://acrossreport.com) for details.

## Links

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
