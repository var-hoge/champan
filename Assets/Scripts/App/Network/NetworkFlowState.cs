using Fusion;

namespace App.Network
{
    /// <summary>
    /// 画面の進行状況を全員で共有する
    ///
    /// タイトルやルール選択のように、ホストが操作して全員が追従する画面のための状態。
    /// 遷移そのものは Fusion のシーンロードで揃うが、
    /// それだけだとゲスト側は選択内容も遷移前の演出も分からない。
    ///
    /// 書き込みは MasterClient のみ。
    /// </summary>
    public class NetworkFlowState
        : NetworkBehaviour
    {
        #region プロパティ
        public static NetworkFlowState Instance { get; private set; }

        /// <summary>
        /// タイトルで選んでいる項目
        /// </summary>
        [Networked]
        public int TitleSelectedIdx { get; set; }

        /// <summary>
        /// タイトルのメニューが出る段階まで進んだか
        ///
        /// メニューの手前に「ボタンを押して開始」の待ちがあり、
        /// ゲストはそこで止まってしまうため、ホストの進行に合わせる
        /// </summary>
        [Networked]
        public NetworkBool IsTitleStaged { get; set; }

        /// <summary>
        /// タイトルで決定されたか
        /// ゲストはこれを見て、ホストと同じ遷移演出を再生する
        /// </summary>
        [Networked]
        public NetworkBool IsTitleDecided { get; set; }

        /// <summary>
        /// ルール選択: CPU の有無 (選択肢の番号)
        /// </summary>
        [Networked]
        public int RuleCpuOptionIdx { get; set; }

        /// <summary>
        /// ルール選択: 勝利に必要な勝ち点 (選択肢の番号)
        /// </summary>
        [Networked]
        public int RuleWinCountOptionIdx { get; set; }

        /// <summary>
        /// ルール選択: メニューで選んでいる項目
        /// </summary>
        [Networked]
        public int RuleMenuItemIdx { get; set; }
        #endregion

        #region メソッド
        public void SetTitleSelectedIdx(int idx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            TitleSelectedIdx = idx;
        }

        public void SetTitleStaged()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            IsTitleStaged = true;
        }

        public void SetTitleDecided()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            IsTitleDecided = true;
        }

        public void SetRuleCpuOptionIdx(int idx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            RuleCpuOptionIdx = idx;
        }

        public void SetRuleWinCountOptionIdx(int idx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            RuleWinCountOptionIdx = idx;
        }

        public void SetRuleMenuItemIdx(int idx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            RuleMenuItemIdx = idx;
        }

        /// <summary>
        /// タイトルに戻ったときなどに使う
        /// </summary>
        public void ResetTitle()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            TitleSelectedIdx = 0;
            IsTitleStaged = false;
            IsTitleDecided = false;
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            Instance = this;

            UnityEngine.Debug.Log($"[NetworkFlowState] 準備できました (権威: {HasStateAuthority})");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
    }
}
