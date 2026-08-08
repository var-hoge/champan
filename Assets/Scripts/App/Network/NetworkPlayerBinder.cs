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
        public int SeatIdx => UseSeatOverride
            ? SeatIdxOverride
            : GetComponent<Actor.Player.DataHolder>().PlayerIdx;
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
        public override void FixedUpdateNetwork()
        {
            // 席テーブルは後から届くため、自分の席になるまで試し続ける
            TryTakeOwnSeatAuthority();
        }
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
        /// この席が自分の担当なら権威を取りに行く
        ///
        /// ランチャー側からまとめて要求する形だと、
        /// シーン上の Player が非アクティブだったり Spawn が遅れたりしたときに取りこぼす。
        /// 各 Player が自分で取りに行くことで、順序やアクティブ状態に依存しなくなる。
        /// </summary>
        void TryTakeOwnSeatAuthority()
        {
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                return;
            }

            if (_isAuthorityRequested || HasStateAuthority)
            {
                return;
            }

            var seatTable = NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                return;
            }

            var seat = seatTable.Seats[SeatIdx];
            if (seat.IsEmpty || seat.Owner != Runner.LocalPlayer)
            {
                return;
            }

            _isAuthorityRequested = true;
            Object.RequestStateAuthority();

            Debug.Log($"[NetworkPlayerBinder] 自分の席なので権威を要求します: seatIdx={SeatIdx}");
        }

        /// <summary>
        /// 権威の有無に応じて、自前のシミュレーションを止める / 動かす
        ///
        /// 位置は NetworkTransform が同期するため、権威を持たない側で自前に座標を動かすと衝突する。
        /// 入力も、止めないと手元のコントローラでリモートのキャラが動いてしまう。
        /// </summary>
        void ApplyAuthorityState()
        {
            // 対戦シーン以外 (キャラセレクトなど) の Player はネットワーク制御しない。
            // シーン上のオブジェクトが決定時に有効化される作りのため権威の受け渡しが安定せず、
            // 何度もキャラが固まる原因になった。
            // キャラセレクトの位置は NetworkCharaSelectState で配る。
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                DisableNetworkTransform();
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
        bool _isAuthorityRequested = false;
        bool[] _localInputEnabledCache = new bool[0];
        #endregion
    }
}
