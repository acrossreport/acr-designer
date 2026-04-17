# ACR Designer

**ACR Designer** は、ACR（Across Report Renderer）エンジン向けの WYSIWYG 帳票デザイナです。  
ピクセル単位で正確かつ、プリンタに依存しない帳票設計を可能にします。  
Avalonia UI を採用し、Windows および macOS でネイティブに動作します。

---

**ACR Designer** is a WYSIWYG report designer for the ACR (Across Report Renderer) engine.  
It enables pixel-perfect, printer-independent report design.  
Built with Avalonia UI, it runs natively on both Windows and macOS.

---

## ダウンロード / Download

[Releases](https://github.com/acrossreport/acr-designer/releases) ページからお使いのプラットフォーム向けのファイルをダウンロードしてください。  
Download the appropriate file for your platform from the [Releases](https://github.com/acrossreport/acr-designer/releases) page.

| プラットフォーム / Platform | ファイル / File |
|----------------------------|----------------|
| Windows (x64) | `acr-designer-win-x64-vX.X.X.zip` |
| macOS (Apple Silicon) | `acr-designer-vX.X.X-mac-arm64.zip` |

---

## 起動方法 / Getting Started

### Windows

1. ZIP ファイルを任意のフォルダに展開します。
2. `acrconfig.json` を `AcrossReportDesigner.exe` と同じフォルダに配置します。
3. `AcrossReportDesigner.exe` をダブルクリックして起動します。

---

1. Extract the ZIP file to any folder.
2. Place `acrconfig.json` in the same folder as `AcrossReportDesigner.exe`.
3. Double-click `AcrossReportDesigner.exe` to launch.

### macOS (Apple Silicon)

1. ZIP ファイルを任意のフォルダに展開します。
2. `acrconfig.json` を `AcrossReportDesigner` と同じフォルダに配置します。
3. ターミナルで以下を実行して起動します。

---

1. Extract the ZIP file to any folder.
2. Place `acrconfig.json` in the same folder as `AcrossReportDesigner`.
3. Launch from Terminal as follows.

```bash
./AcrossReportDesigner
```

> **注意 / Note:**  
> 本アプリは Apple による公証（Notarization）を取得済みです。  
> macOS Gatekeeper により安全性が確認されており、初回起動時に警告は表示されません。  
>
> This application has been notarized by Apple.  
> It is verified by macOS Gatekeeper and will not trigger a security warning on first launch.

---

## サンプルデータ / Sample Data

サンプルの `acrconfig.json` およびテンプレートファイルは近日公開予定です。  
Sample `acrconfig.json` and template files will be available soon.

---

## 動作要件 / Requirements

- ランタイムのインストールは不要です。パッケージは自己完結型（Self-contained）です。
- 起動には `acrconfig.json` が必要です。

---

- No runtime installation is required. The package is self-contained.
- `acrconfig.json` is required to launch the application.

---

## ライセンス / License

© Across Systems Corporation. All rights reserved.
