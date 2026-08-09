using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦時に Player をネットワークへ橋渡しするコンポーネント
    ///
    /// Main シーンの Player はシーンに直接置かれているため、
    /// 席が決まったピアが後から権威を取りに行く形になる。
    /// そのため権威の有無は Spawned() の時点で確定せず、途中で変わる。
    /// </summary>
    public class NetworkPlayerBinder
        : NetworkBehaviour
        , IStateAuthorityChanged
    {
        #region プロパティ
        /// <summary>
        /// 実行時生成の場合に渡される席番号
        /// シーン配置の Player は DataHolder に設定済みの playerIdx を使うため、これを使わない
        /// </summary>
        [Networked]
        public int SeatIdxOverride { get; set; }

        /// <summary>
        /// SeatIdxOverride を使うかどうか
        /// </summary>
        [Networked]
        public NetworkBool UseSeatOverride { get; set; }

        /// <summary>
        /// この Player が担当する席番号
        /// </summary>
        public int SeatIdx
        {
            get
            {
                var seatIdxFromData = GetComponent<Actor.Player.DataHolder>().PlayerIdx;

                // Spawn される前は Networked なプロパティを読めない
                if (Object == null || !Object.IsValid)
                {
                    return seatIdxFromData;
                }

                return UseSeatOverride ? SeatIdxOverride : seatIdxFromData;
            }
        }

        /// <summary>
        /// 左を向いているか
        ///
        /// 向きは速度から決まるが、権威を持たない側では MoveCtrl を止めているため
        /// 速度が変化せず、向きも変わらない。そのため別途配る。
        /// </summary>
        /// 反映は NetworkPlayerPositionApplier が IProcPostMove で行う。
        /// 自分のキャラの向きを決める RotateCtrl と同じ位相にそろえるため。
        [Networked]
        public NetworkBool IsFacingLeft { get; set; }

        /// <summary>
        /// 権威を持つ側の座標
        ///
        /// NetworkTransform は自身が座標を管理する前提で、描画時に自分の持つ値で上書きする。
        /// champan の MoveCtrl は Update で transform を直接動かすため、
        /// その移動が毎フレーム打ち消されてしまう。
        /// そのため NetworkTransform は使わず、キャラセレクトと同じく座標を配る。
        /// </summary>
        [Networked]
        public Vector3 SyncPosition { get; set; }

        /// <summary>
        /// 今乗っている足場
        ///
        /// 乗車の登録は TadaRigidbody2D が行うが、
        /// 権威を持たない側ではその処理を止めているため登録されない。
        /// そのままではホストから見て「誰も乗っていない」ことになり、
        /// バブルがはじけず、王冠の獲得者も分からない。
        /// </summary>
        [Networked]
        public NetworkObject RidingObject { get; set; }

        /// <summary>
        /// 権威を持つ側の速度
        ///
        /// 拡縮アニメや表情、オノマトペは、どれも速度と接地から毎フレーム決まる。
        /// 演出を一つずつ配るより、元になるこの値を配って
        /// 各台に同じ計算をさせるほうが、ずれようがなく数も増えない。
        /// </summary>
        [Networked]
        public Vector2 SyncVelocity { get; set; }

        /// <summary>
        /// 権威を持つ側の接地状態
        /// </summary>
        [Networked]
        public NetworkBool IsGrounded { get; set; }

        /// <summary>
        /// 権威を持つ側の大きさ
        ///
        /// 着地・ジャンプ・高速落下・踏まれたときの拡縮は、どれも出どころが違うが
        /// 最後は必ず TotalScaleCtrl を通って反映される。
        /// そこで、個々の演出ではなく通り道の結果を配る。
        /// 演出が増えても配るものは増えない。
        ///
        /// z は常に 1 のため運ばない。
        /// </summary>
        [Networked]
        public Vector2 SyncScale { get; set; }

        /// <summary>
        /// 権威を持つ側の見た目の大きさ
        /// </summary>
        [Networked]
        public Vector2 SyncViewScale { get; set; }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            if (UseSeatOverride)
            {
                // 既存コードはすべて playerIdx を見て動くので、まずこれを確定させる
                GetComponent<Actor.Player.DataHolder>().SetPlayerIdx(SeatIdxOverride);
            }

            CacheLocalInputEnabled();
            ApplyAuthorityState();
        }

        /// <summary>
        /// @memo: 同期状況の調査用。原因が判明したら削除する
        /// </summary>
        /// <summary>
        /// 権威を持つ側が自分の向きを配る
        ///
        /// 権威を持たないオブジェクトでは呼ばれないため、受け取り側の反映は
        /// OnFacingChanged で行う。
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            // 拡縮はキャラセレクトでも起きるため、シーンを問わず配る。
            // (座標だけは、キャラセレクトでは NetworkCharaSelectState が配る)
            PublishScale();

            // キャラセレクトの座標は NetworkCharaSelectState が配る。
            // ここで扱うと二重になる。
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                return;
            }

            SyncPosition = transform.position;

            // 乗っている足場を配る
            var rigidbody = GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            var ridingMover = rigidbody != null ? rigidbody.RidingMover : null;

            // MoveInfoCtrl はバブルの子オブジェクトに付いているため、
            // NetworkObject は親をたどって探す
            RidingObject = ridingMover != null
                ? ridingMover.GetComponentInParent<NetworkObject>()
                : null;

            // 見た目の計算に使う元の値を配る
            IsGrounded = rigidbody != null && rigidbody.IsGround;

            var moveCtrl = GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null)
            {
                SyncVelocity = moveCtrl.Velocity;
            }


            var rotateCtrl = GetComponent<Actor.Player.RotateCtrl>();
            if (rotateCtrl == null)
            {
                return;
            }

            if (IsFacingLeft != rotateCtrl.IsFacingLeft)
            {
                IsFacingLeft = rotateCtrl.IsFacingLeft;
            }
        }

        // @memo: 配られた座標の反映は NetworkPlayerPositionApplier が行う。
        //        当たり判定と噛み合わせるために、反映する位相が重要になる。

        // @memo: 配られた向きの反映も NetworkPlayerPositionApplier が行う。

        // @memo: 権威の要求は NetworkGameLauncher が行う。
        //        権威を持たないオブジェクトでは FixedUpdateNetwork が呼ばれないため、
        //        ここで取りに行くことはできない。
        #endregion

        #region Fusion.IStateAuthorityChanged の実装
        /// <summary>
        /// 権威が移ったとき (席が決まったピアが権威を取りに来たときなど)
        /// </summary>
        public void StateAuthorityChanged()
        {
            ApplyAuthorityState();
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 大きさを配る
        ///
        /// 着地・ジャンプ・高速落下・踏まれたときの拡縮は出どころが違うが、
        /// 最後は必ず TotalScaleCtrl を通って反映される。
        /// 個々の演出ではなく、その通り道の結果を配る。
        /// </summary>
        void PublishScale()
        {
            var totalScaleCtrl = GetComponentInChildren<TadaLib.ActionStd.TotalScaleCtrl>(true);
            if (totalScaleCtrl == null)
            {
                return;
            }

            SyncScale = totalScaleCtrl.CurrentScale;
            SyncViewScale = totalScaleCtrl.CurrentViewScale;
        }

        /// <summary>
        /// 権威の有無に応じて、自前のシミュレーションを止める / 動かす
        ///
        /// 位置は NetworkTransform が同期するため、権威を持たない側で自前に座標を動かすと衝突する。
        /// 入力も、止めないと手元のコントローラでリモートのキャラが動いてしまう。
        /// </summary>
        void ApplyAuthorityState()
        {
            // NetworkTransform は描画時に自分の持つ座標で上書きするため、
            // Update で transform を動かす MoveCtrl と噛み合わない。
            // 座標は SyncPosition で自前に配るので、常に無効にする。
            DisableNetworkTransform();

            // 対戦シーン以外 (キャラセレクトなど) の Player はネットワーク制御しない。
            // キャラセレクトの位置は NetworkCharaSelectState で配る。
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                return;
            }

            var isLocal = HasStateAuthority;

            if (isLocal)
            {
                ApplyLocalInputIdx();
            }

            var moveCtrl = GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null)
            {
                moveCtrl.enabled = isLocal;
            }

            var rigidbody = GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.enabled = isLocal;
            }

            // InputUtil は「有効な IInput を最初に見つけた 1 つ」を返すため、
            // ローカル入力を無効にしておかないと NetworkInput が使われるとは限らない
            var inputs = GetComponents<TadaLib.Input.IInput>();
            for (int idx = 0; idx < inputs.Length; ++idx)
            {
                if (inputs[idx] is NetworkInput)
                {
                    continue;
                }

                // 権威を取り戻したときは元の有効状態に戻す
                // (CPU かどうかで元の値が変わるため、単純に true にはできない)
                inputs[idx].ActionEnabled = isLocal && _localInputEnabledCache[idx];
            }

            Debug.Log(
                $"[NetworkPlayerBinder] 権威を{(isLocal ? "取得" : "喪失")}しました:"
                + $" seatIdx={SeatIdx} networkId={Object.Id} 権威者={Object.StateAuthority}");
        }

        /// <summary>
        /// 対戦シーン以外では NetworkTransform を止める
        ///
        /// 権威を持たない側では Fusion が毎フレーム座標を上書きするため、
        /// MoveCtrl が動いていてもキャラがその場に固定されてしまう。
        /// </summary>
        void DisableNetworkTransform()
        {
            var networkTransform = GetComponent<Fusion.NetworkTransform>();
            if (networkTransform != null && networkTransform.enabled)
            {
                networkTransform.enabled = false;
            }
        }

        /// <summary>
        /// 自分が担当する席を、この台の何番目のコントローラで操作するかを設定する
        ///
        /// 席番号とローカルのコントローラ番号は一致しない。
        /// 「2 台目の席 2」は、その台のローカル 1 人目が操作する。
        /// </summary>
        void ApplyLocalInputIdx()
        {
            if (NetworkSeatTable.Instance == null)
            {
                return;
            }

            var seat = NetworkSeatTable.Instance.Seats[SeatIdx];
            if (seat.IsEmpty)
            {
                return;
            }

            var reader = GetComponent<TadaLib.Input.PlayerInputReader>();
            if (reader == null)
            {
                return;
            }

            reader.SetLocalInputIdx(seat.LocalSlot);

            Debug.Log($"[NetworkPlayerBinder] 操作の割り当て: seatIdx={SeatIdx} ローカル番号={seat.LocalSlot}");
        }

        /// <summary>
        /// ローカル入力の元々の有効状態を控えておく
        /// CPU かどうかによって変わるため、復帰時に単純に true へ戻せない
        /// </summary>
        void CacheLocalInputEnabled()
        {
            var inputs = GetComponents<TadaLib.Input.IInput>();
            _localInputEnabledCache = new bool[inputs.Length];

            for (int idx = 0; idx < inputs.Length; ++idx)
            {
                _localInputEnabledCache[idx] = inputs[idx].ActionEnabled;
            }
        }
        #endregion

        #region private フィールド
        bool[] _localInputEnabledCache = new bool[0];
        #endregion
    }
}
