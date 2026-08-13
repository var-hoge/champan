using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
using static UnityEngine.Rendering.DebugUI;
using App.Cpu;

namespace Ui.Main
{
    /// <summary>
    /// GameFinishUi
    /// </summary>
    public class GameFinishUi
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public async UniTask Staging(SimpleAnimation animation)
        {
            // 選択と決定はホストが配る。
            //
            // 演出に入る前に初期値を配り、決定の回数も控えておく。
            // 待ち始めてから控えると、その前にホストが決めていた場合に取りこぼす。
            var flowState = App.Network.NetworkFlowState.Instance;
            flowState?.SetFinishMenuItemIdx(0);
            var decidedCountAtStart = flowState != null ? flowState.FinishDecidedCount : 0;

            var name = BGMManager.Instance.GetCurrentAudioNames()[0];
            BGMManager.Instance.FadeOut(0.25f);
            BGMManager.Instance.Play(audioPath: name, volumeRate: 1f, delay: 5f);

            var path = _playerWinPaths[Random.Range(0, _playerWinPaths.Length)];
            SEManager.Instance.Play(SEPath.VICTORY_SCREEN_START);
            SEManager.Instance.Play(audioPath: path, volumeRate: 1f, delay: 3.5f);

            _canvas.gameObject.SetActive(true);
            _canvas.GetComponent<CanvasGroup>().alpha = 0.0f;
            _ = _canvas.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);

            _rematchButton.OnSelected();
            _meinMenuButton.OnUnselected();

            // 振動
            {
                var winnerPlayerIdx = GameSequenceManager.WinnerPlayerIdx;
                if (CpuManager.Instance.IsCpu(winnerPlayerIdx) is false)
                {
                    TadaLib.Input.PlayerInputManager.Instance.InputProxy(winnerPlayerIdx).Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.VeryHappy);
                }
            }

            animation.Play("GameFinish");

            await UniTask.WaitForSeconds(3.0f);
            _rematchButton.gameObject.SetActive(true);
            _meinMenuButton.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(0.5f);

            // クリックまで待つ
            //
            // ネットワーク対戦では、ルール選択と同じくホストだけが操作する。
            // 各台で選ばせると、選んだ先が食い違ううえ、
            // 待ち続けた台は決定後の後始末 (BGM の切り替え) に進めないまま
            // ホストにシーンを移され、対戦の BGM が鳴り続けていた。
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;

            // 状態が届いていないときは、待ち続けて詰まらせるより自分で操作する
            var isFollower = App.Network.NetworkSession.IsOnline
                && !App.Network.NetworkSession.HasAuthority
                && flowState != null;

            int selectedIdx = 0;

            if (isFollower)
            {
                // 自分では選べないことを伝える
                App.Ui.Common.NetworkNoticeUi.SetStatus(
                    App.Ui.Common.NetworkNoticeUi.StatusWaitingForHost);
            }

            while (true)
            {
                if (isFollower)
                {
                    // ホストの選択に追従する
                    if (flowState.FinishMenuItemIdx != selectedIdx)
                    {
                        selectedIdx = flowState.FinishMenuItemIdx;
                        ApplyMenuSelection(selectedIdx);
                    }

                    if (flowState.FinishDecidedCount != decidedCountAtStart)
                    {
                        break;
                    }

                    await UniTask.Yield();
                    continue;
                }

                var isEnd = false;
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                    {
                        isEnd = true;
                        break;
                    }
                }

                if (isEnd)
                {
                    // 他の台にも決定を伝える
                    flowState?.DecideFinishMenu();
                    break;
                }

                // 上下移動
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).AxisTrigger(TadaLib.Input.AxisCode.Vertical, out var isPositive) is false)
                    {
                        continue;
                    }

                    selectedIdx = 1 - selectedIdx;
                    ApplyMenuSelection(selectedIdx);

                    // 他の台にも選択を伝える
                    flowState?.SetFinishMenuItemIdx(selectedIdx);

                    break;
                }

                await UniTask.Yield();
            }

            App.Ui.Common.NetworkNoticeUi.ClearStatus();

            var isRematch = selectedIdx == 0;

            GameMatchManager.Instance.ResetPlayersWinCount();

            if (isRematch)
            {
                _rematchButton.OnDecided();
            }
            else
            {
                _meinMenuButton.OnDecided();
            }

            SEManager.Instance.Play(SEPath.MENU_VALIDATION);

            if (isRematch)
            {
                BGMSwitcher.FadeOutAndFadeIn(BGMPath.TITLE_SCREEN, 0.4f, 0.4f);

                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.3f, 0.3f);
            }
            else
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.6f, 0.4f);
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;

        [SerializeField]
        TadaLib.Ui.Button _rematchButton;

        [SerializeField]
        TadaLib.Ui.Button _meinMenuButton;

        string[] _playerWinPaths = null;
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _playerWinPaths = new[]
            {
                SEPath.PLAYER_WIN_01,
                SEPath.PLAYER_WIN_02,
                SEPath.PLAYER_WIN_03,
            };
        }
        #endregion

        #region privateメソッド
        /// <summary>
        /// 選んでいる項目を見た目に反映する
        ///
        /// 追従する側も同じここを通す。
        /// 状態だけ映して見た目を別に書くと、必ず食い違う。
        /// </summary>
        void ApplyMenuSelection(int selectedIdx)
        {
            SEManager.Instance.Play(SEPath.MENU_NAVIGATION);

            if (selectedIdx == 0)
            {
                _rematchButton.OnSelected(doReaction: true);
                _meinMenuButton.OnUnselected();
                return;
            }

            _rematchButton.OnUnselected();
            _meinMenuButton.OnSelected(doReaction: true);
        }
        #endregion
    }
}