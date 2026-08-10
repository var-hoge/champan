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
        /// <summary>
        /// シールドの残りを配る
        ///
        /// シールドを減らすのはホストの担当だが、
        /// 減らしているのは Crown.Manager が持つ通常の値で、
        /// 配られる値とは別物になっている。
        /// 配り直さないと、他の台は最初の値のまま見た目が変わらない。
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            var manager = Actor.Gimmick.Crown.Manager.Instance;
            if (manager == null)
            {
                return;
            }

            ShieldValue = manager.ShieldValue;
            ExShieldValue = manager.ExShieldValue;
            InitShieldValue = manager.InitShieldValue;
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

            // 値だけでなく見た目も更新する
            if (manager.CrownBubble != null)
            {
                manager.CrownBubble.RefreshCrownVisual();
            }
        }

        void OnCrownBubbleChanged()
        {
            // @memo: 調査用。原因が判明したら削除する
            Debug.Log($"[王冠調査] 王冠バブルの通知を受けました 権威={HasStateAuthority} 対象={(CrownBubble != null ? "あり" : "無し")}");

            if (HasStateAuthority)
            {
                return;
            }

            var manager = Actor.Gimmick.Crown.Manager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[王冠調査] Crown.Manager が見つかりません");
                return;
            }

            var bubble = CrownBubble != null
                ? CrownBubble.GetComponent<Actor.Gimmick.Bubble.Bubble>()
                : null;

            if (bubble == null)
            {
                manager.CrownBubble = null;
                return;
            }

            // 管理情報だけでなく見た目も更新する必要がある。
            // ホストと同じ経路を通すことで、王冠とシールドの表示が揃う。
            Actor.Gimmick.Bubble.Bubble.SetupCrown(bubble);
        }
        #endregion
    }
}
