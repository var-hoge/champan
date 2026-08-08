# ネットワーク対戦 設計メモ（Photon Fusion 2 / Shared Mode）

## 決定事項

| 項目 | 決定 |
|---|---|
| SDK | Photon Fusion 2 |
| モード | **Shared Mode** |
| 対戦形態 | オンライン最大 4 人。**1 台にローカル 2 人 × 2 台**のような混在を許容 |
| 既存モード | ローカル対戦・CPU 対戦は**そのまま残す** |

Shared Mode を選んだ理由: champan の移動・当たり判定は自作（`TadaRigidbody2D` + ProcSystem、状態はプレーンな C# フィールド）で、Fusion の Host Mode が要求する `FixedUpdateNetwork` + `[Networked]` 状態への全面移行は事実上の作り直しになる。Shared Mode なら「自分のキャラは自分がシミュレートし、結果を配る」形になり、`MoveCtrl` / `StateMachine` / `HitSystem` をほぼ現状のまま流用できる。

代償: 高レイテンシ下で当たり判定の見え方がずれる。チート耐性はない。展示・身内対戦の用途では許容する。

## 権威（Authority）の分担

Shared Mode では NetworkObject ごとに StateAuthority を持つピアが決まる。

| 対象 | 権威 | 理由 |
|---|---|---|
| Player（キャラ本体） | **そのキャラを操作するピア** | 操作の応答性を最優先。自分のキャラだけは常にラグゼロ |
| Bubble / RespawnBubble / BubbleGenerator | **MasterClient** | 全員に同じ配置が見えないとゲームが成立しない |
| Crown（王冠） | **MasterClient** | 勝敗判定の根拠になるため |
| `GameMatchManager`（勝ち点） | **MasterClient** | 勝敗の唯一の真実 |
| `GameSequenceManager`（フェーズ） | **MasterClient** | ラウンド開始・終了のタイミングを揃える |
| 画面遷移 | **MasterClient** が決定し全員が追従 | 全員が同じ画面にいる必要がある |
| エフェクト・オノマトペ・影・カメラ | **ローカル** | 見た目だけ。同期しない |

## ローカル複数人 × 複数台 の扱い

ここが今回いちばん設計を歪めやすい点。

- **1 台 = 1 `NetworkRunner` = Fusion 上の 1 `PlayerRef`（ピア）**
- **ローカルプレイヤーの人数分だけ、そのピアが Player の NetworkObject を Spawn する**（1 ピアが複数オブジェクトの StateAuthority を持つ）

つまり Fusion の `PlayerRef` と、既存コードが全面的に使っている `playerIdx`（0〜3 の席番号）は **1 対 1 ではない**。ここを混ぜないための対応:

- 席番号 `playerIdx` は **MasterClient が割り当て**、`[Networked]` な席テーブル（`PlayerRef` + ローカル番号 → `playerIdx`）で共有する
- 既存コードは今まで通り `playerIdx` だけを見る。`PlayerRef` はネットワーク層の内側に閉じ込める

## 入力の流し方

`TadaLib.Input.IInput`（[IInput.cs](../../Assets/TadaLib/Scripts/Input/IInput.cs)）が既に入力の抽象になっていて、`App.Cpu.CpuInput` が「人間以外の入力実装」として動いている実績がある。ここを再利用する。

- **ローカルのキャラ** — 従来通り `PlayerInputProxy` をそのまま使う（変更なし）
- **リモートのキャラ** — 新規の `NetworkInput : IInput` を付ける

位置と向きは NetworkTransform で同期・補間するので、`NetworkInput` の役割は「ステートマシンとアニメーションが参照する入力値の再現」。権威側が入力スナップショットを `[Networked]` で配り、リモート側の `NetworkInput` がそれを返す。

`InputUtil.TryGetInput` は「有効な `IInput` を 1 つ返す」実装なので、`PlayerInputProxy` 側と `NetworkInput` 側の `enabled` / `ActionEnabled` を排他にするだけで切り替わる。**既存の入力コードには手を入れなくてよい。**

## 画面遷移の同期

全遷移が `TadaLib.Scene.TransitionManager.StartTransition` を通っている（Title / Credits / CharaSelect / GameModeSelect / Main / Result の各 UI Manager から計 13 箇所）。

ここに `NetworkSequenceManager` を挟む:

- オフライン時 — 今まで通り即座に `StartTransition` を呼ぶ
- オンライン時 — 遷移要求を MasterClient に投げ、MasterClient が RPC で全員に `StartTransition` を発行する

各 UI Manager の呼び出しを直接書き換えるのではなく、**ラッパー経由に差し替える**。オフライン動作を壊さないため。

## 対象シーケンスとネットワーク的な扱い

| 画面 | 扱い |
|---|---|
| Title | ここで「ローカル対戦 / オンライン対戦」を分岐。オンラインならルーム作成・参加 UI へ |
| （新規）ロビー | ルーム作成 / 参加。参加者と席の確定。**新規実装** |
| Credits | 同期不要（オフラインのまま） |
| CharaSelect | 各自のカーソルと選択キャラを同期。全員確定で MasterClient が次へ |
| GameModeSelect（ルール選択） | MasterClient のみ操作可、選択内容を同期 |
| Main（対戦） | 本丸。プレイヤー・Bubble・Crown・フェーズを同期 |
| 中間リザルト | 勝ち点を同期して MasterClient が次ラウンドを開始 |
| Result | 勝者を同期。「CharaSelect へ / Title へ」は MasterClient が決定 |

## 大前提: シーンはローカルとネットワークで共通

**Main シーンをローカル対戦とネットワーク対戦で共通のものとして使う。** ネットワーク専用のシーンを別に作らない。

これは以下を意味する。

- シーン上のオブジェクト構成はモードによって変えない
- 「ネットワーク対戦かどうか」は実行時に分岐して吸収する
- Player などのプレハブも、可能な限り 1 つに保つ

### この方針から導かれる、見直すべき判断

Phase 3 で `Player.prefab` のバリアントとして `PlayerNetwork.prefab` を作ったが、これはこの方針と相性が悪い。プレハブが 2 つに分かれると Main シーンがモードごとに別物になりかねない。

**本来は `Player.prefab` 本体に `NetworkObject` を持たせ、オフライン時は Runner を通さずに使っても無害である状態を目指すべき。** バリアントは Phase 3 時点での暫定措置として扱い、Main シーンへの統合時に解消する。

## 実装フェーズ

| # | 内容 | 既存コードへの影響 |
|---|---|---|
| 0 | Fusion 2 SDK 導入・App ID 設定・規約整備 | なし |
| 1 | 検証シーンで 2 台接続 + 四角の同期スパイク | なし（新規シーンのみ） |
| 2 | 席テーブル（`PlayerRef` → `playerIdx`）とセッション管理の土台 | 追加のみ |
| 3 | `NetworkInput : IInput` と Player の NetworkObject 化 | プレハブ改変 + 追加 |
| 4 | Bubble / RespawnBubble / Crown のネットワークスポーン化 | 中 |
| 5 | `GameMatchManager` / `GameSequenceManager` の MasterClient 権威化 | 大 |
| 6 | ロビー UI と画面遷移の同期（`NetworkSequenceManager`） | 大 |
| 7 | 切断・再接続・CPU との共存・レイテンシ調整 | — |

**Phase 1 完了時点で方向性を再判断する。** ここまでは既存コードに一切触らないので、撤退コストがゼロ。

## Phase 1 の検証結果 (2026-08-08)

`Assets/Scenes/NetworkSpike.unity` で実施。検証項目はすべて合格。

| # | 項目 | 結果 |
|---|---|---|
| 1 | 2 ピアが Shared Mode で同じセッションに接続 | OK |
| 2 | 権威側の移動がもう一方に同期される | OK |
| 3 | 1 ピアが複数体の権威を持てる | OK |
| 4 | PlayerRef と席番号を分離して扱える | OK |

実測: 仮想プレイヤーが `PlayerRef=1` で席 0/1、メインエディタが `PlayerRef=2` で席 2/3 を担当し、四角が 4 つ表示・相互に同期された。**「1 台にローカル 2 人 × 2 台」は Shared Mode で成立する。**

### 開発中の動作確認手段

Unity の Multiplayer Play Mode (`com.unity.multiplayer.playmode`) を使う。ビルド不要でエディタ内に 2 ピア目を立てられる。

- Window > Multiplayer > Multiplayer Play Mode で Player 2 を有効化
- 仮想プレイヤーのログは `Library/VP/<id>/Logs/Editor.log` にある。メインエディタの Console には出ないので、問題があればこのファイルを見る

### ハマりどころ (実際に踏んだもの)

**1. スクリプトで作った NetworkObject プレハブは Prefab Table に登録されない**

`InvalidOperationException: Prefab ... has been baked with a guid ..., but such guid failed to be translated into a prefab id` が出る。
→ `Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable()`（メニューでは Tools > Fusion > Rebuild Prefab Table）を実行する。

**2. `Runner.Spawn()` の戻り値に状態を代入しても `Spawned()` には間に合わない**

`Spawned()` は `Spawn()` が返る前に走る。戻り値に `SeatIdx` を代入する書き方だと、`Spawned()` は初期値のまま動く。
→ `Spawn()` の `onBeforeSpawned` コールバックで渡す。**本実装でも席番号・キャラ ID の受け渡しは必ずこの形にすること。**

**3. MPPM の仮想プレイヤーは AssetDatabase が読み取り専用**

Fusion の `NetworkProjectConfig.fusion` は ScriptedImporter 経由で読まれるため、成果物が古いと仮想プレイヤー側は自力で再インポートできず `Failed to load NetworkProjectConfigAsset` で `StartGame` が落ちる。
→ メインエディタ側で `AssetDatabase.ImportAsset(..., ForceUpdate | ForceSynchronousImport)` してから、仮想プレイヤーを立て直す。

**4. `Asset Database is set to Read Only, but it has found out-of-date assets.` は無害**

Fusion のエディタ拡張 ([Fusion.Unity.Editor.cs:5024](../../Assets/Photon/Fusion/Editor/Fusion.Unity.Editor.cs:5024)) が仮想プレイヤー内で `AssetDatabase.Refresh()` を呼ぶために出る警告。ゲーム動作には影響せず、ビルドにも出ない。**消すには Photon の SDK 本体を書き換えることになるため、対応しない方針。**

**5. fire-and-forget な async は例外を握り潰す**

`_ = JoinAsync()` の形だと例外が Unity の Console に一切出ず、無言で失敗する。async な接続処理では必ず try/catch でログを出す。

## 現在地 (2026-08-08 時点)

### 完了

- Phase 0: SDK 導入・規約整備
- Phase 1: Shared Mode の成立性を検証 (`Assets/Scenes/NetworkSpike.unity`)
- Phase 2: 席テーブル (`NetworkSeatTable`) — **2 ピアで動作確認済み**
- Phase 3: `NetworkInput` / `NetworkPlayerBinder` / `PlayerNetwork.prefab` — **実装のみ。未検証**

### 未検証で残っていること

`Assets/Scenes/NetworkPlayerTest.unity` で実際の Player を動かそうとしたが、
素のシーンでは Main シーン固有のマネージャが無く NullReference が多発して検証に至らなかった。

| 発生元 | 不足しているもの |
|---|---|
| `CpuInput.OnPostMove` (CpuInput.cs:163) | `CpuViewDataManager` |
| `Shadow.Requestor.OnEnable` (Requestor.cs:32) | `Shadow.Manager` |
| `Hit.HitCollider.Start` (HitCollider.cs:116) | 未特定 |

Player は Main シーンの環境とセットで成立する作りのため、**素のシーンを継ぎ足す方向は取らない**。
Phase 4 以降を進めて Main シーンで通しで確認できる状態にしてから、まとめて検証する。

そのため以下は**まだ誰も確認していない**。実装が正しい保証はない。

- `MoveCtrl` / `TadaRigidbody2D` を止めたとき、`StateMachine` や `MoveScaleAnimCtrl` がリモートで正しく動くか
- `NetworkTransform` の補間と `TotalScaleCtrl` / `RotateCtrl` の見た目が破綻しないか
- `CharaCtrl.Start()` が `DataHolder.PlayerIdx` を読むタイミングと、`NetworkPlayerBinder.Spawned()` で席番号を設定するタイミングの前後関係
  （間に合わないと全員が同じキャラで表示される）

### 次の一手 (Phase 4)

Bubble 系のネットワークスポーン化。対象は以下。

- `Assets/Scripts/App/Actor/Gimmick/Bubble/Bubble.cs` / `BubbleGenerator.cs`
- `Assets/Scripts/App/Actor/Gimmick/RespawnBubble/RespawnBubble.cs` / `PlayerSpawner.cs`
- `Assets/Scripts/App/Actor/Gimmick/Crown/` (勝敗判定の根拠)

いずれも現在は素の `Instantiate` を呼んでいる。MasterClient 権威で `Runner.Spawn` に置き換え、
オフライン時は従来通り `Instantiate` する分岐を入れる (シーンを共通にするため)。

エフェクト (`_bubPopEff` など) はローカルのままでよい。

## 既知の要注意ポイント

- `GameSequenceManager.WinnerPlayerIdx` が `public static`（[GameSequenceManager.cs:32](../../Assets/Scripts/App/GameSequenceManager.cs:32)）。ネットワーク時は `[Networked]` 経由に置き換える必要がある
- `CharaCtrl` が `CharaSelectUiManager.PlayerUseCharaIdList(playerIdx)` を static 直読みしている（[CharaCtrl.cs:25](../../Assets/Scripts/App/Actor/Player/CharaCtrl.cs:25)）。全クライアントでこの値が一致していないと別キャラが表示される
- `MoveCtrl` が `transform.position` を直接書き換えている（[MoveCtrl.cs:240](../../Assets/Scripts/App/Actor/Player/MoveCtrl.cs:240)）。NetworkTransform との併用時、リモート側では `MoveCtrl` を止める必要がある
- `GameMatchManager` / `GameSequenceManager` が `SingletonMonoBehaviour`。NetworkBehaviour 化するとライフサイクル（Spawned / Despawned）とシングルトンの初期化順が衝突しうる
- `Application.targetFrameRate = 60` 固定。可変フレームレートの相手とつながったときの `gameObject.DeltaTime()` の差異
