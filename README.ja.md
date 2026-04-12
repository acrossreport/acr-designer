# ACR Designer

**ACR Designer** は、ACR（Across Report Renderer）エンジン向けの WYSIWYG 帳票デザイナです。
ピクセル単位で正確かつ、プリンタに依存しない帳票設計を可能にします。
Avalonia UI を採用し、Windows と macOS でネイティブに動作します。

---

## ダウンロード

[Releases](https://github.com/acrossreport/acr-designer/releases) ページからお使いのプラットフォーム向けのファイルをダウンロードしてください。

| プラットフォーム | ファイル |
|----------------|---------|
| Windows (x64) | `acr-designer-win-x64-vX.X.X.zip` |
| macOS (Apple Silicon) | `acr-designer-mac-arm64-vX.X.X.zip` |

---

## 起動方法

### Windows

1. ZIP ファイルを任意のフォルダに展開します。
2. `acrconfig.json` を `AcrossReportDesigner.exe` と同じフォルダに配置します。
3. `AcrossReportDesigner.exe` をダブルクリックして起動します。

### macOS

1. ZIP ファイルを任意のフォルダに展開します。
2. `acrconfig.json` を `AcrossReportDesigner` と同じフォルダに配置します。
3. ターミナルを開き、以下を実行します。

```bash
chmod +x AcrossReportDesigner
./AcrossReportDesigner
```

---

## 動作要件

- ランタイムのインストールは不要です。パッケージは自己完結型（Self-contained）です。
- 起動には `acrconfig.json` が必要です。

---

## ライセンス

© Across Systems Corporation. All rights reserved.


