using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦時に Player をネットワークへ橋渡しするコンポーネント
    ///
    /// やること
    /// - 席番号 (playerIdx) を DataHolder に流し込む
    /// - 権威を持たない側で、ローカルにシミュレートしてはいけないコンポーネントを止める
    ///
    /// 既存のローカル対戦・CPU 対戦に影響を与えないよう、
    /// これらを持つのは Player プレハブのネットワーク用バリアントだけにしている。
    /// </summary>
    public class NetworkPlayerBinder
        : NetworkBehaviour
    {
        #region プロパティ
        /// <summary>
        /// この Player が担当する席番号
        /// Spawn 時に onBeforeSpawned で渡すこと (Spawned() より後に代入しても間に合わない)
        /// </summary>
        [Networked]
        public int SeatIdx { get; set; }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            // 既存コードはすべて playerIdx を見て動くので、まずこれを確定させる
            GetComponent<Actor.Player.DataHolder>().SetPlayerIdx(SeatIdx);

            if (HasStateAuthority)
            {
                return;
            }

            DisableLocalSimulation();
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 権威を持たない側で、ローカルのシミュレーションを止める
        ///
        /// 位置は NetworkTransform が同期するため、自前で座標を動かすものが残っていると衝突する。
        /// 入力も、止めないと手元のコントローラでリモートのキャラが動いてしまう。
        /// </summary>
        void DisableLocalSimulation()
        {
            // 自前で transform.position を書き換えるもの
            var moveCtrl = GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null)
            {
                moveCtrl.enabled = false;
            }

            var rigidbody = GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.enabled = false;
            }

            // InputUtil は「有効な IInput を最初に見つけた 1 つ」を返すため、
            // ローカル入力を無効にしておかないと NetworkInput が使われるとは限らない
            foreach (var input in GetComponents<TadaLib.Input.IInput>())
            {
                if (input is NetworkInput)
                {
                    continue;
                }

                input.ActionEnabled = false;
            }

            Debug.Log($"[NetworkPlayerBinder] リモートのため自前シミュレーションを停止しました: seatIdx={SeatIdx}");
        }
        #endregion
    }
}
