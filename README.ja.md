# ACR Designer

[English](README.md) | 日本語 | [Français](README.fr.md)

ACR Designer は、ACR(AcrossReport)の帳票定義(JSON)を作成・編集するデスクトップアプリケーションです。

## 特長

- 帳票のデザイン(帯・コントロールの配置と設定、用紙サイズ)
- 新規作成時に、セクション型 / Free Canvas を選択
- テンプレートは JSON で保存・読み込み([ACR Generator](https://github.com/acrossreport/acr-generator) で作成した JSON も開けます)
- DB 接続:SQLite / SQL Server / PostgreSQL / MySQL / Oracle / Access(Access は Windows のみ)
- プレビュー、PDF・PNG・HTML 出力
- 日本語 / English / Français

## 動作環境

| OS | 状況 |
|---|---|
| Windows x64 | 対応 |
| Windows ARM64 | 対応 |
| macOS(Apple Silicon) | 対応 |
| macOS(Intel) | 対応 |
| Linux x64 | 対応 |
| Linux ARM64 | 対応 |

- 対応する Windows のバージョン:Windows 11
- .NET ランタイムを同梱しているため、別途インストールは不要です
- macOS 版は Developer ID で署名し、Apple の notarize 済みです

## ダウンロード

[Releases](https://github.com/acrossreport/acr-designer/releases) から、お使いの OS 用のファイルをダウンロードしてください。

- Windows x64:`AcrossReportDesigner-v0.1.2-win-x64.zip`(サンプルのテンプレート・データ付き)
- Windows ARM64:`AcrossReportDesigner-v0.1.2-win-arm64.zip`(サンプルのテンプレート・データ付き)
- macOS(Apple Silicon):`AcrossReportDesigner-v0.1.2-osx-arm64.zip`
- macOS(Intel):`AcrossReportDesigner-v0.1.2-osx-x64.zip`
- Linux x64:`AcrossReportDesigner-v0.1.2-linux-x64.zip`
- Linux ARM64:`AcrossReportDesigner-v0.1.2-linux-arm64.zip`(サンプルのテンプレート・データ付き)

> **macOS についての補足:** 設定・テンプレート・データ・出力は `~/Desktop/AcrossReportDesigner` に保存されます。macOS 版にはまだサンプルファイルは含まれていません。
> **Linux についての補足:** Linux x64 版にはまだサンプルファイルは含まれていません。Linux ARM64 版には含まれています。

## インストールと起動

1. ダウンロードした zip を任意のフォルダに展開します
2. Windows:展開したフォルダの中の `AcrossReportDesigner.exe` を実行します。macOS/Linux:`./AcrossReportDesigner` を実行します(Linux では事前に `chmod +x AcrossReportDesigner` が必要な場合があります)

初回起動時に、出力先の `Output` フォルダ(`PDF`・`PNG`・`html`)などが実行ファイルと同じ場所に自動で作られます(macOS のみ `~/Desktop/AcrossReportDesigner` 以下になります)。

## 使い方

1. 新規作成でセクション型または Free Canvas を選ぶか、既存のテンプレート(JSON)を開きます
2. 帯・コントロールを配置し、プロパティを設定します
3. 必要に応じて DB に接続し、データを結合してプレビューします
4. PDF・PNG・HTML に出力します(exe の隣の `Output` フォルダ)
5. テンプレートを JSON で保存します

## 出力について

ACR Designer は帳票の確認用のため、PDF・PNG の出力には、登録の有無にかかわらず常にウォーターマークが入ります。本番の印刷・出力には ACR Engine または ACR Viewer をお使いください。詳しくは [公式サイト](https://acrossreport.com) をご覧ください。

## 関連リンク

- ACR Samples(サンプルテンプレート・データ。すぐに試せます):https://github.com/acrossreport/acr-data-samples
- ACR Generator:https://github.com/acrossreport/acr-generator
- ACR 仕様(JSON テンプレート):https://github.com/acrossreport/acr-spec
- 公式サイト:https://acrossreport.com

## ライセンス

本ソフトウェアのソースコードは公開していません。利用条件は [LICENSE](LICENSE) をご確認ください。

## お問い合わせ

across.support@gmail.com

---

© Across Systems Corporation
ACR の中間描画命令アーキテクチャは特許出願中です。
