# AcrossReportDesigner

> **ACR エンジン対応の WYSIWYG 帳票デザイナー（Avalonia UI）**

---

## これは何か

AcrossReportDesigner は ACR（Across Report Renderer）エンジンと連携する帳票デザイナーです。
Avalonia UI を採用しており、Windows / macOS / Linux でネイティブ動作します。

JSON テンプレートを視覚的に編集し、リアルタイムで HTML プレビューを確認しながら帳票を設計できます。

---

## 主な機能

- **WYSIWYGデザイン** — テキスト・図形・バーコード・罫線をドラッグ＆ドロップで配置
- **リアルタイムプレビュー** — `axr_layout.dll` 経由で HTML プレビューを即時表示
- **PDF/PNG 出力** — `axr_runtime.dll`（Skia）経由で高精度レンダリング
- **データバインディング** — CSV / SQLite / SQL Server / PostgreSQL / MySQL / Oracle 対応
- **多言語対応** — 日本語 / 英語
- **Undo/Redo** — 全操作に対応
- **セクション構造** — ActiveReports 互換（ページヘッダー・詳細・グループ・フッター）

---

## フォルダ構成

```
AcrossReportDesigner/
├── Assets/          # アイコン・画像リソース
├── Converters/      # Avalonia バインディングコンバーター
├── Data/            # データソース（CSV・各種DB）
├── Designer/        # デザインキャンバス・選択・ズーム制御
├── Engines/         # レイアウトエンジン・セクションビルダー
├── Logic/           # フォントモード解決
├── Models/          # 帳票モデル（テキスト・線・矩形・バーコード等）
├── Native/          # axr_runtime.dll / axr_layout.dll（配置先）
├── Rendering/       # PDF/PNG エクスポーター・Skia レンダラー
├── Resources/       # 多言語リソース（日本語・英語）
├── Services/        # エンジン・ライセンス・設定・履歴管理
├── UndoRedo/        # Undo/Redo フレームワーク
├── Utils/           # フォントプロバイダー等
├── ViewModels/      # MVVM ViewModel
└── Views/           # Avalonia AXAML ビュー
```

---

## 必要な環境

| 環境 | バージョン |
|------|-----------|
| .NET | 8.0 以上 |
| Avalonia UI | 11.x |
| OS | Windows 10/11 / macOS / Linux |

---

## ACR エンジンとの連携

本デザイナーは ACR エンジン（別リポジトリ）が生成する DLL を `Native/` フォルダに配置して使用します。

```
Native/
├── axr_runtime.dll   ← acr_dll （Skia PDF/PNG レンダリング）
└── axr_layout.dll    ← acr_html_engine （HTML プレビュー）
```

### DLL のビルドと配置

```bash
# ACR リポジトリで
cd Acr
cargo build --release

# DLL を Native/ にコピー
copy target\release\axr_runtime.dll ..\AcrossReportDesigner\Native\
copy target\release\axr_layout.dll  ..\AcrossReportDesigner\Native\
```

---

## ビルド方法

```bash
cd AcrossReportDesigner
dotnet build
dotnet run
```

または Visual Studio 2022 で `AcrossReportDesigner.sln` を開いてビルドしてください。

---

## データソース対応

| データソース | 対応状況 |
|---|---|
| CSV | ✅ 対応済み |
| SQLite | ✅ 対応済み |
| SQL Server | ✅ 対応済み |
| PostgreSQL | ✅ 対応済み |
| MySQL | ✅ 対応済み |
| Oracle | ✅ 対応済み |

---

## アーキテクチャ

```
ユーザー操作（View）
      ↓
  ViewModel（MVVM）
      ↓
  DesignerEngine / LayoutEngine
      ↓
  axr_layout.dll（HTML プレビュー）
  axr_runtime.dll（PDF/PNG 出力）
```

Undo/Redo は `IUndoableCommand` インターフェースで統一管理されており、全操作が取り消し可能です。

---

## 関連リポジトリ

| リポジトリ | 内容 |
|---|---|
| [acr](https://github.com/acrossreport/acr) | ACR Rust エンジン本体 |
| [acr-designer](https://github.com/acrossreport/acr-designer) | 本リポジトリ |

---

## ライセンス

Private repository — All rights reserved.

本リポジトリのコード・設計・アーキテクチャは著作権および特許により保護されています。
無断複製・転用を禁じます。

