# 日記作成アシスタント (DiaryAssistant)

![main_screen](https://github.com/user-attachments/assets/9620a7b6-6346-40b2-8699-a4d3da4a3fe2)

毎日の振り返りをサポートする、超軽量デスクトップアシスタントアプリです。

普段日記を書かない方でも、AIアシスタントとの会話を通じて自然に一日を振り返ることができます。

> [!IMPORTANT]
> 本アプリではGoogle Gemini APIを使用できます。APIキーは**ユーザー自身の責任**で取得・管理してください。APIキーの使用によって生じたいかなる問題（セキュリティ、APIの制限、不適切な出力など）についても、開発者は一切の責任を負いません。より安全に利用したい場合は、Ollama機能を使ってローカル環境でLLMを実行することも可能です。

## 特徴

- **超軽量**: WPFアプリケーションのため、メモリ使用量はわずか20MB程度
![taskmanager](https://github.com/user-attachments/assets/0178def7-8f57-4a33-a24b-981cf1b61739)
- **豊かな表情**: 会話の内容に応じてアシスタントの表情アイコンが変化
- **プライバシー**: ローカルLLM(Ollama)対応で、オフラインでの利用も可能
- **カスタマイズ**: アシスタントの性格や見た目、通知頻度など様々な設定が可能

## インストール方法

1. [最新版のzipファイル](https://github.com/user-attachments/files/19155373/DiaryAssistant_v1.0.0.zip)をダウンロードします
2. お好きな場所に展開します
3. `日記作成アシスタント.exe`を実行します

初回起動時に、Gemini APIキーの設定画面が表示されます。

![api_key_setup](https://github.com/user-attachments/assets/b64ae501-2f5b-4205-9d2d-a2c12ceae53b)


### Gemini APIキーの取得方法（簡易版）

1. [Google AI Studio](https://ai.google.dev/gemini-api/docs/api-key?&hl=ja)にアクセス
2. Googleアカウントでログイン
3. 「APIキーを作成」をクリック
4. 生成されたAPIキーをコピーして、アプリに貼り付け

詳しい設定方法は[ドキュメント（準備中）](https://github.com/)を参照してください。

## 使い方

起動すると、Windowsのタスクトレイに常駐します。設定された時間間隔で通知が表示され、アシスタントとの会話ができます。

![notification](https://github.com/user-attachments/assets/38169e8b-74a0-40b6-9c27-6a44a742366f)

アシスタントは、アクティブウィンドウに関連した雑談もできます。（デフォルトではオフ）

![active](https://github.com/user-attachments/assets/e3e0dc73-74ae-4306-9427-b357392b906e)

### 基本操作

- **タスクトレイアイコン**を右クリックすると操作メニューが表示されます
![tray_menu](https://github.com/user-attachments/assets/2ee676f1-152d-46c4-8027-bdbb2989ec53)
- **今すぐ通知**: アシスタントとすぐに会話を始めたいときに
- **日記閲覧**: 過去の会話や記録を閲覧・編集できます
![chat](https://github.com/user-attachments/assets/d311a6cc-ae47-4ac4-977c-03c3194e03f9)
![list](https://github.com/user-attachments/assets/5eccaf30-cf37-408e-a555-ed47bc75c343)  
- **通知の一時停止/再開**: 作業に集中したいときに便利
- **アシスタントの変更**: アシスタントの性格を変更できます
![selene](https://github.com/user-attachments/assets/d74330d2-c8bf-469f-97d8-9dc36686dbd5)

## データとプライバシーについて

- APIキーなどの設定情報はローカルに保存されます
- 開発者への送信データはありません

ログについて：
- `logs`フォルダの`YYYYMMDD.txt`ファイルに、Gemini APIへの送信プロンプトとレスポンスが記録されます
- データベースは`%LOCALAPPDATA%\DiaryAssistant\data\diary.db`に保存されます

## 対応環境

- Windows 10/11 (64bit)
- .NET Framework 4.8以上
- メモリ: 4GB以上推奨

## ローカルLLM対応（Ollama）

GeminiのAPIキーがなくても、Ollamaでローカル環境のLLMを使用することができます。
![ollama](https://github.com/user-attachments/assets/bc7d2dbf-52a3-4282-9b3e-0d23d7d95f07)

## 開発者向け情報

このプロジェクトはMITライセンスで公開されています。常駐アプリということもあり、セキュリティに関心がある方は自前でビルドすることをお勧めします。

Issue（バグ報告や機能リクエスト）は歓迎しています。

## ライセンス

[MIT License](LICENSE)

## クレジット

開発: [kame404](https://kame404.com)
