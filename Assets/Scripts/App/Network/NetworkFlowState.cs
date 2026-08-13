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

        /// <summary>
        /// ルール選択: ホストがカーソルを動かした回数
        ///
        /// 追従する側は、これが増えたときだけカーソルを動かす。
        ///
        /// 選んでいる項目そのものは、前にこの画面へ来たときの値が残っている。
        /// それに合わせるだけでカーソルを動かすと、
        /// 誰も操作していないのに勝手に動いたように見える。
        /// </summary>
        [Networked]
        public int RuleMenuMoveCount { get; set; }

        /// <summary>
        /// スコア表を閉じて次へ進んだ回数
        ///
        /// 真偽値にすると前のラウンドの値が残り、
        /// 次のラウンドでスコア表が即座に閉じてしまう。
        /// 数えることで「今回進んだか」を毎回見分けられる。
        /// </summary>
        [Networked]
        public int ScoreAdvanceCount { get; set; }

        /// <summary>
        /// 最終結果: 選んでいる項目 (0: もう一度、1: メインメニュー)
        /// </summary>
        [Networked]
        public int FinishMenuItemIdx { get; set; }

        /// <summary>
        /// 最終結果: 決定した回数
        /// もう一度遊ぶと再び最終結果に来るため、こちらも数える
        /// </summary>
        [Networked]
        public int FinishDecidedCount { get; set; }
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
        /// ルール選択でカーソルを動かしたことを伝える
        /// </summary>
        public void NotifyRuleMenuMoved()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            ++RuleMenuMoveCount;
        }

        /// <summary>
        /// スコア表を閉じて次へ進むことを全員に伝える
        /// </summary>
        public void AdvanceScorePanel()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            ++ScoreAdvanceCount;
        }

        public void SetFinishMenuItemIdx(int idx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            FinishMenuItemIdx = idx;
        }

        /// <summary>
        /// 最終結果で決定したことを全員に伝える
        /// </summary>
        public void DecideFinishMenu()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            ++FinishDecidedCount;
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

        /// <summary>
        /// 部屋を解散したことを全員に知らせる (ホストのみ)
        ///
        /// ホストが抜けると Fusion は別の台をホストに繰り上げるが、
        /// 解散は「この部屋を畳む」ことなので、全員に抜けてもらう。
        /// </summary>
        public void NotifyRoomClosed()
        {
            if (Object == null || !Object.IsValid || !HasStateAuthority)
            {
                return;
            }

            RPC_NotifyRoomClosed();
        }

        /// <summary>
        /// 解散の知らせを受ける
        ///
        /// 送ったホストは自分で抜けるため、そこでは呼ばない。
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyRoomClosed()
        {
            Ui.CharaSelect.NetworkRoomWindow.NotifyRoomClosedByHost();
        }

        /// <summary>
        /// 次の遷移で使う演出の長さを全員に知らせる
        ///
        /// 遷移の決定はホストが行うが、演出は各台で再生する。
        /// 長さは画面ごとに違うため、遷移の直前に配る。
        /// </summary>
        public void NotifyFadeDurations(float fadeInDurationSec, float fadeOutDurationSec, bool isReverse)
        {
            if (Object == null || !Object.IsValid || !HasStateAuthority)
            {
                return;
            }

            RPC_NotifyFadeDurations(fadeInDurationSec, fadeOutDurationSec, isReverse);
        }

        /// <summary>
        /// 演出の長さを受け取る
        ///
        /// 送った台では既に覚えているため、そこでは呼ばない。
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyFadeDurations(float fadeInDurationSec, float fadeOutDurationSec, NetworkBool isReverse)
        {
            NetworkTransition.ApplyFadeDurations(fadeInDurationSec, fadeOutDurationSec, isReverse);
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
