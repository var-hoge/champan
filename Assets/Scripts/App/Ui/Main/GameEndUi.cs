using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using static App.GameSequenceManager;
using Ui.Main;
using KanKikuchi.AudioManager;

namespace App.Ui.Main
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class GameEndUi
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public async UniTask GameEndWithContinue(SimpleAnimation animation, int winnerPlayerIdx)
        {
            // この関数が呼ばれる時点で勝ち点は加算されている

            var advanceCountAtStart = GetScoreAdvanceCount();

            await UniTask.WaitForSeconds(0.25f);

            GameSequenceManager.Instance.PhaseKind = Phase.AfterBattle;

            // バブルを全部壊す
            var bubbles = GameObject.FindObjectsByType<Actor.Gimmick.Bubble.Bubble>(FindObjectsSortMode.None);
            foreach (var bubble in bubbles)
            {
                bubble.DoBurst();
            }

            await UniTask.WaitForSeconds(1.5f);

            if (GameMatchManager.Instance.TotalWinCount >= 2)
            {
                // スピーディモード
                Time.timeScale = 1.2f;
            }

            // 勝ち点を表示
            _winCountPanel.gameObject.SetActive(true);
            _continueButton.gameObject.SetActive(false);
            SEManager.Instance.Play(SEPath.CHEERING_CROWD, volumeRate: 1, delay: 1.5f);

            await UniTask.WaitForSeconds(2.2f);

            // リーチテキスト表示
            _winCountPanel.ShowReachTextIfNeed();

            _continueButton.gameObject.SetActive(true);
            _continueButton.OnSelected();

            // クリックまで待つ
            await WaitForScoreAdvanceAsync(advanceCountAtStart);

            _continueButton.OnDecided();

            await UniTask.WaitForSeconds(0.1f);

            if (GameMatchManager.Instance.TotalWinCount >= 2)
            {
                // スピーディモード解除
                Time.timeScale = 1.0f;
            }

            SEManager.Instance.FadeOut(SEPath.CHEERING_CROWD, 0.5f);

            SEManager.Instance.Play(SEPath.MENU_VALIDATION);

            BGMManager.Instance.FadeOut(0.6f);

            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.3f, 0.3f);
        }

        public async UniTask GameEnd(SimpleAnimation animation, int winnerPlayerIdx)
        {
            var advanceCountAtStart = GetScoreAdvanceCount();

            await UniTask.WaitForSeconds(0.25f);

            GameSequenceManager.Instance.PhaseKind = Phase.AfterBattle;

            // バブルを全部壊す
            var bubbles = GameObject.FindObjectsByType<Actor.Gimmick.Bubble.Bubble>(FindObjectsSortMode.None);
            foreach (var bubble in bubbles)
            {
                bubble.DoBurst();
            }

            await UniTask.WaitForSeconds(1.2f);

            // 1 点先取で勝ちの場合はテンポ重視のため、パネルを出さない
            if (GameMatchManager.Instance.TotalWinCount >= 2)
            {
                await UniTask.WaitForSeconds(0.5f);

                // 勝ち点を表示
                _winCountPanel.gameObject.SetActive(true);
                _continueButton.gameObject.SetActive(false);
                SEManager.Instance.Play(SEPath.CHEERING_CROWD, volumeRate: 1, delay: 1.5f);

                await UniTask.WaitForSeconds(2.2f);

                _continueButton.gameObject.SetActive(true);
                _continueButton.OnSelected();

                await UniTask.WaitForSeconds(0.05f);

                // クリックまで待つ
                await WaitForScoreAdvanceAsync(advanceCountAtStart);

                _continueButton.OnDecided();

                await UniTask.WaitForSeconds(0.1f);
            }

            _ = _canvas.DOFade(0.0f, 0.2f);

            SEManager.Instance.FadeOut(SEPath.CHEERING_CROWD, 0.5f);

            await UniTask.WaitForSeconds(0.5f);

            await _gameFinishUi.Staging(animation);
        }
        #endregion

        #region privateフィールド
        [System.Serializable]
        class GoalUi
        {
            public Sprite BackSprite;
            public Sprite WinnerSprite;
        }

        [SerializeField]
        List<Sprite> _charaSprites;

        [SerializeField]
        CanvasGroup _canvas;

        [SerializeField]
        List<GoalUi> _charaVarietySprites;

        [SerializeField]
        UnityEngine.UI.Image _background;

        [SerializeField]
        UnityEngine.UI.Image _chara;

        [SerializeField]
        UnityEngine.UI.Image _crown;

        [SerializeField]
        WinCountPanel _winCountPanel;

        [SerializeField]
        TadaLib.Ui.Button _continueButton;

        [SerializeField]
        GameFinishUi _gameFinishUi;
        #endregion

        #region privateメソッド
        /// <summary>
        /// スコア表を閉じてよくなるまで待つ
        ///
        /// ネットワーク対戦では、進めるかどうかをホストだけが決める。
        ///
        /// 各台のボタンで進ませると、進んだ台と待ち続ける台に分かれる。
        /// 待っている台はこの先の後始末 (拍手を止める、BGM を落とす) に進めないまま
        /// ホストにシーンを移されるため、音が鳴りっぱなしで次のラウンドに入っていた。
        /// </summary>
        /// <param name="advanceCountAtStart">
        /// 演出を始めた時点の回数。
        /// 待ち始めてから数えると、それより前にホストが進めていた場合に取りこぼす。
        /// </param>
        static async UniTask WaitForScoreAdvanceAsync(int advanceCountAtStart)
        {
            var flowState = Network.NetworkFlowState.Instance;

            // 状態が届いていないときは、待ち続けて詰まらせるより自分で進む
            var isFollower = Network.NetworkSession.IsOnline
                && !Network.NetworkSession.HasAuthority
                && flowState != null;

            if (isFollower)
            {
                // 自分では進められないことを伝える。
                // 何も出ないと、進め方が分からず止まって見える
                Common.NetworkNoticeUi.SetStatus(Common.NetworkNoticeUi.StatusWaitingForHost);
            }

            while (true)
            {
                if (isFollower)
                {
                    if (flowState.ScoreAdvanceCount != advanceCountAtStart)
                    {
                        break;
                    }

                    await UniTask.Yield();
                    continue;
                }

                if (IsAnyActionPressed())
                {
                    // 他の台にも進むことを伝える
                    flowState?.AdvanceScorePanel();
                    break;
                }

                await UniTask.Yield();
            }

            Common.NetworkNoticeUi.ClearStatus();
        }

        static int GetScoreAdvanceCount()
        {
            var flowState = Network.NetworkFlowState.Instance;
            return flowState != null ? flowState.ScoreAdvanceCount : 0;
        }

        static bool IsAnyActionPressed()
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;

            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                {
                    return true;
                }
            }

            return false;
        }

        private void Start()
        {
            // 全て初期化
            //_canvas.alpha = 0.0f;
            _background.gameObject.SetActive(false);
            _chara.gameObject.SetActive(false);
            _crown.gameObject.SetActive(false);
        }
        #endregion
    }
}