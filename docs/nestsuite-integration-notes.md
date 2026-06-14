# NestSuite統合に向けた開発メモ

v0.4.0時点の構造と、将来のNestSuite統合を見据えた方針を記録する。

## 現在の構造（v0.4.x）

### ChatNestWorkspaceView / ChatNestWorkspaceViewModel

チャット編集本体を担う。

- メッセージ一覧 (`ObservableCollection<Message>`)
- 入力テキスト・発言者選択
- 投稿・削除コマンド
- 発言者ショートカット (`CycleSpeaker`)
- コピーテキスト生成 (`BuildMarkdownCopyText` / `BuildIdeaNestCopyText`)
- `WorkspaceModified` イベント（変更発生を外枠へ通知）

`ChatNestWorkspaceView` は WPF UserControl として実装されており、
将来のNestSuiteのタブ内への配置を想定した設計になっている。

### MainViewModel / MainWindow

単体アプリとしての外枠制御を担う。

- `.chatnest` ファイルの保存・読込
- ウィンドウタイトル管理
- 最前面トグル
- スタートダイアログ・終了処理ダイアログの制御
- 未保存変更の確認
- `WorkspaceModified` を購読して変更時保存を実行

## NestSuite統合の想定

`ChatNestWorkspaceView` をNestSuiteのタブとして配置する際は、
以下を想定している。

- `ChatNestWorkspaceViewModel` をNestSuiteのViewModelから生成して渡す
- ファイル操作・保存トリガーはNestSuite側が担う
- `WorkspaceModified` イベントをNestSuite側で購読して保存処理を呼ぶ
- `ChatNestWorkspaceView` 自体は変更しない

## 実装しないこと（v0.4.x）

- NestSuite本体の実装
- タブUI
- 共通インターフェース（`IWorkspaceViewModel` 等）の導入
- DIコンテナの導入
- 共通ライブラリへの切り出し
- 保存形式の変更

過剰な抽象化は行わない。実際にNestSuiteでChatNestWorkspaceViewを
配置できるか検証してから必要な共通化を判断する。
