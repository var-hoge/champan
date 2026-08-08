# champan

Unity 6 / URP 17 の 2D 対戦アクションゲーム。ローカル最大4人 + CPU。
Photon Fusion 2 によるネットワーク対戦を追加中（`Docs/Network/design.md` 参照）。

## プロジェクト構成

- `Assets/Scripts/App/` — ゲーム固有のコード（`App` 名前空間）
- `Assets/TadaLib/Scripts/` — 自作の汎用ライブラリ（`TadaLib` 名前空間）
- `Assets/Scenes/` — Title / Credits / CharaSelect / GameModeSelect / Main / Result

自作の基盤が多い。特に以下は Unity 標準の代替なので、標準機能で置き換えようとしないこと。

- **ProcSystem** — 更新順を制御する独自の更新機構（`Assets/TadaLib/Scripts/ProcSystem/`）
- **TadaRigidbody2D** — 自作の 2D 移動・接地判定（Unity Physics2D の Rigidbody は使わない）
- **IInput** — 入力の抽象化。人間の入力（`PlayerInputProxy`）と CPU（`App.Cpu.CpuInput`）が同じ口を実装する
- **TadaLib.Scene.TransitionManager** — 画面遷移。すべての遷移がここを通る

## 画面シーケンス

```
Title ─┬→ CharaSelect → GameModeSelect(ルール選択) → Main(対戦)
       │                                              ├→ 中間リザルト → Main
       └→ Credits → Title                             └→ Result ─┬→ CharaSelect
                                                                  └→ Title
```

## コーディング規約

### 記述順序

コンストラクタ → メソッド → 基底クラス・インターフェースの実装（MonoBehaviour の `Start` 等、ProcSystem のインターフェース実装）→ private フィールド → private メソッド

### `#region`

- インターフェースを実装するメンバーは `#region <完全修飾インターフェース名> の実装` … `#endregion` で囲う
  - 例: `#region TadaLib.ProcSystem.IProcUpdate の実装`、`#region TadaLib.Input.IInput の実装`
- 汎用の `#region プロパティ` `#region メソッド` に混ぜない

### 継承の記述

1 行に並べず、クラス名の後で改行して各行に書く。

```csharp
public class MoveCtrl
    : TadaLib.ProcSystem.BaseProc
    , TadaLib.ProcSystem.IProcMove
```

### `Update()` の禁止

`MonoBehaviour.Update()` は **UI 以外では使用しない**。代わりに `BaseProc` を継承して ProcSystem のインターフェースを実装する。

- `IProcUpdate.OnUpdate()` — 入力・ステート更新などのデフォルト処理
- `IProcMove.OnMove()` — 移動処理
- `IProcPostMove.OnPostMove()` — 座標確定後の処理（カメラ更新はここ）

`BaseProc` が毎フレーム `ProcManager` に自己登録する設計は、同種コンポーネントを更新リスト上で連続させ Script Execution Order に依存しない順序保証を得るための**意図的なもの**。GC 最適化のために永続リスト方式へ変えないこと。

### deltaTime

`gameObject.DeltaTime()`（`TadaLib.Extension` の拡張メソッド）を使う。`Time.deltaTime` を直接使わない。

### その他

- `if` 文は本体が 1 行でも必ず中括弧 `{}` を使う
- 属性（`[SerializeField]` 等）はメンバー宣言と同じ行に書かず、直前の行に独立して書く
- コンポーネントの有効フラグは `IsEnabled`（コンポーネントレベル）/ `IsEnabledState`（ステート切り替えでリセット）
- ステート切り替え（`AddStateStartCallback`）でリセットされるプロパティは末尾に `State` を付ける（`GravityRateState` など）

### 既存コードとの付き合い方

champan には規約が整備される前のコードが残っている（空の `Update()`、`using TadaLib.ProcSystem` を書いた `App` 層のファイルなど）。

- **新規コードは規約に準拠する**
- 既存コードは、その箇所を触るついでに直す。規約適合だけを目的とした一括改変はしない

## プレハブ運用

Player などの共有オブジェクトへの変更は**プレハブ本体を編集**して各シーンに反映させる。シーンインスタンス側のオーバーライドで済ませると他シーンに反映されない。

## Unity エディタとの連携

Coplay MCP でエディタを操作できる（コンパイルエラー確認、ログ取得、プレハブ・シーン編集）。コード変更後は `check_compile_errors` で確認する。

## コミット

日本語のコミットメッセージ。`feat:` / `fix:` / `refactor:` / `change:` / `chore:` のプレフィックスを付ける。
