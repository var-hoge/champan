using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 王冠まわりの状態を全員で一致させる
    ///
    /// Crown.Manager はシールド値を Awake で Random.Range によって決めるため、
    /// そのままでは台ごとに値が食い違い、勝敗条件がずれる。
    /// MasterClient が決めた値を配って全員に反映させる。
    ///
    /// 同期しないもの
    /// - LastCrownRidePlayerIdx: 勝敗判定は MasterClient が行うため、そちらの値が正となる
    /// - 演出系 (DoFakeFinishStaging など): 見た目だけなので一致していなくても成立する
    /// </summary>
    public class NetworkCrownState
        : NetworkBehaviour
    {
        #region プロパティ
        public static NetworkCrownState Instance { get; private set; }

        [Networked]
        [OnChangedRender(nameof(OnShieldChanged))]
        public int ShieldValue { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnShieldChanged))]
        public int ExShieldValue { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnShieldChanged))]
        public int InitShieldValue { get; set; }

        /// <summary>
        /// 王冠を持っているバブル
        /// </summary>
        [Networked]
        [OnChangedRender(nameof(OnCrownBubbleChanged))]
        public NetworkObject CrownBubble { get; set; }
        #endregion

        #region メソッド
        /// <summary>
        /// 王冠を持つバブルを設定する (権威側のみ)
        /// </summary>
        public void SetCrownBubble(Actor.Gimmick.Bubble.Bubble bubble)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            CrownBubble = bubble != null ? bubble.GetComponent<NetworkObject>() : null;
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            Instance = this;

            var manager = Actor.Gimmick.Crown.Manager.Instance;
            if (manager == null)
            {
                return;
            }

            if (HasStateAuthority)
            {
                // MasterClient が Awake で決めた値を全員に配る
                ShieldValue = manager.ShieldValue;
                ExShieldValue = manager.ExShieldValue;
                InitShieldValue = manager.InitShieldValue;
                return;
            }

            OnShieldChanged();
            OnCrownBubbleChanged();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region private メソッド
        void OnShieldChanged()
        {
            if (HasStateAuthority)
            {
                return;
            }

            var manager = Actor.Gimmick.Crown.Manager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.ApplyNetworkShieldValues(ShieldValue, ExShieldValue, InitShieldValue);
        }

        void OnCrownBubbleChanged()
        {
            if (HasStateAuthority)
            {
                return;
            }

            var manager = Actor.Gimmick.Crown.Manager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.CrownBubble = CrownBubble != null
                ? CrownBubble.GetComponent<Actor.Gimmick.Bubble.Bubble>()
                : null;
        }
        #endregion
    }
}
