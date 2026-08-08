# ネットワーク対戦の開発環境セットアップ

champan をクローンしてネットワーク対戦を動かすまでの手順。

## 1. Photon App ID を設定する（必須）

**`Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` は Git の管理対象外です。**
champan は public リポジトリなので、App ID をリポジトリに含めないようにしています。各自で設定してください。

未設定のまま実行すると、接続時に `StartGame` が失敗します。

### 手順

1. [Photon Dashboard](https://dashboard.photonengine.com) にログインする
2. **Fusion** タイプのアプリケーションを作る（既にあるならそれを使う）
3. アプリケーションの App ID をコピーする
4. Unity で `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` を選択する
   - ファイルが無い場合は Unity メニューの **Tools > Fusion > Realtime Settings** で生成される
5. インスペクタの **App Id Fusion** に貼り付ける

`AppIdRealtime` / `AppIdQuantum` / `AppIdChat` / `AppIdVoice` は空のままでよい。

### App ID の扱いについて

Photon の App ID は**ビルドしたゲームに必ず埋め込まれる**ため、配布物を解析すれば取り出せる。Git から外しているのはリポジトリからの流出を防ぐためであって、秘密として守り切れる類のものではない。

不正利用を実際に防ぎたい場合は、Photon Dashboard 側で対策する。

- CCU の上限を設定して被害を頭打ちにする
- 使用リージョンを絞る
- 厳密にやるならカスタム認証（Custom Authentication）を導入する

## 2. 動作確認する

### 検証シーン

`Assets/Scenes/NetworkSpike.unity` が Shared Mode の最小構成の検証シーンになっている。
再生すると自動でセッション `champan-spike` に参加し、ローカル 2 人分の四角を生成する。

- 1 人目: WASD
- 2 人目: 方向キー

### 2 ピア目を立てる（Multiplayer Play Mode）

ビルドせずにエディタ内で 2 ピア目を動かせる。

1. **Window > Multiplayer > Multiplayer Play Mode** を開く
2. **Player 2** を有効にする（初回はプロジェクトのクローン作成に数分かかる）
3. メインエディタで再生する

四角が 4 つ表示され、互いに同期していれば成功。

### 仮想プレイヤーのログの見方

仮想プレイヤーのログは**メインエディタの Console には出ない**。問題が起きたら直接ファイルを見る。

```
Library/VP/<id>/Logs/Editor.log
```

## 3. よくある問題

### `Failed to load NetworkProjectConfigAsset`

仮想プレイヤー側で出る。MPPM の仮想プレイヤーは AssetDatabase が読み取り専用のため、Fusion の設定アセットの成果物が古いと自力で再インポートできずに失敗する。

→ メインエディタで対象アセットを強制再インポートし、仮想プレイヤーを立て直す。

```csharp
AssetDatabase.ImportAsset(
    "Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion",
    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
```

### `Prefab ... has been baked with a guid ..., but such guid failed to be translated into a prefab id`

NetworkObject を持つプレハブが Fusion の Prefab Table に登録されていない。スクリプトからプレハブを生成したときに起きる。

→ **Tools > Fusion > Rebuild Prefab Table** を実行する。

### `Asset Database is set to Read Only, but it has found out-of-date assets.`

仮想プレイヤーで出る警告。Fusion のエディタ拡張が読み取り専用の AssetDatabase に対して `AssetDatabase.Refresh()` を呼ぶために出る。**無害なので対応しない**（消すには Photon の SDK 本体を書き換えることになる）。

### 接続が無言で失敗する

async な接続処理を `_ = JoinAsync()` のように fire-and-forget で呼ぶと、例外が Unity の Console に一切出ない。接続処理には必ず try/catch でログを出すこと。

## 関連

- 設計方針: [design.md](design.md)
