# xreal-webrtc-test

unity xrealのキャプチャした画面をブラウザで表示したい

* XREAL Air2 Pro
* Beam Pro

## 環境構築(Unity)

### インストール

* Unity 6000.0.36f1
* NRSDK 2.4.1
* com.unity.webrtc@3.0.0-pre.8

1. UnityにNRSDKをインポートする
2. NuGetをインストールする
   [Unity で NuGet パッケージを利用する #.NET - Qiita](https://qiita.com/akiojin/items/ac05392d97abb8797dcd)
   `Packages/manifest.json`にスコープを追加する

   ```json
    {
        "scopedRegistries": [
            {
            "name": "Unity NuGet",
            "url": "https://unitynuget-registry.azurewebsites.net",
            "scopes": [
                "org.nuget"
            ]
            }
        ],
        "dependencies": {
            ...
        }
    }
   ```

3. UnityにWebRTCをインポートする
   [パッケージのインストール | WebRTC | 3.0.0-pre.7](https://docs.unity3d.com/ja/Packages/com.unity.webrtc@3.0/manual/install.html)

   ```console
   com.unity.webrtc@3.0.0-pre.8
   ```

4. UnityにWebsocketをインポートする
   NuGetで`websocketsharp.core`をインストールする

### 準備

* `Assets/Sample/WebRTCStreamer.cs`の`serverUrl`をサーバのURLに変更する
  `serverUrl = "ws://192.168.x.x:3001/";`

## 環境構築(サンプルサーバ)

Node.jsで実施する

```bash
cd server
npm install
```

## 使い方

1. サンプルサーバを起動する

   ```bash
   cd server
   npm start
   ```

2. ブラウザで`viewer.html`を開く
3. Andoroidでアプリを起動する

次のように表示されれば成功

<https://github.com/user-attachments/assets/3ee96af2-5695-4295-93fe-2df4caecff66>

## メモ

* mp4ファイルを作成するNRSDKのサンプルを使って、mp4作成時にそのファイルをWebRTCで送信している

## 注意事項

* Unity Editor上では動作しない。Beam Pro上で動作確認すること
* AndroidManifest.xmlの権限は縮小できるかも（元々NRSDKのPlay VideoがXREAL Air2 Proで上手く扱えなくてその時は下記の3つの権限で上手くいったため）

  ```xml
  <uses-permission android:name="android.permission.RECORD_AUDIO" />
  <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
  <uses-permission android:name="android.permission.FOREGROUND_SERVICE_MEDIA_PROJECTION" />
  ```

* 終了した後にNode.jsが終了しているか確認すること

## 参考

* [チュートリアル | WebRTC | 3.0.0-pre.7](https://docs.unity3d.com/ja/Packages/com.unity.webrtc@3.0/manual/tutorial.html)
