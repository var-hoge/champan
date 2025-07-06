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
            _canvas.gameObject.SetActive(true);
            _canvas.GetComponent<CanvasGroup>().alpha = 0.0f;
            _ = _canvas.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);

            _rematchButton.OnSelected();
            _meinMenuButton.OnUnselected();

            animation.Play("GameFinish");

            await UniTask.WaitForSeconds(3.0f);

            _rematchButton.gameObject.SetActive(true);
            _meinMenuButton.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(0.5f);

            // クリックまで待つ
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            bool isRematch = true;
            int selectedIdx = 0;
            var axisPrev = new List<float>();
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                axisPrev.Add(0.0f);
            }
            while (true)
            {
                var isEnd = false;
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                    {
                        isRematch = selectedIdx == 0;
                        isEnd = true;
                        break;
                    }
                }

                if (isEnd)
                {
                    break;
                }

                // 上下移動
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    var axis = inputManager.InputProxy(idx).Axis(TadaLib.Input.AxisCode.Vertical);

                    // 入力開始時だけ受け付ける
                    if ((axis > 0.5f && axisPrev[idx] > 0.5) ||
                            (axis < -0.5f && axisPrev[idx] < -0.5f))
                    {
                        // 同じ
                        continue;
                    }

                    axisPrev[idx] = axis;

                    // 入力値が少ない場合はなし
                    if (Mathf.Abs(axis) < 0.5f)
                    {
                        continue;
                    }

                    selectedIdx = 1 - selectedIdx;
                    if (selectedIdx == 0)
                    {
                        _rematchButton.OnSelected(doReaction: true);
                        _meinMenuButton.OnUnselected();
                    }
                    else
                    {
                        _rematchButton.OnUnselected();
                        _meinMenuButton.OnSelected(doReaction: true);
                    }

                    break;
                }

                await UniTask.Yield();
            }

            GameMatchManager.Instance.ResetPlayersWinCount();

            if (isRematch)
            {
                _rematchButton.OnDecided();
            }
            else
            {
                _meinMenuButton.OnDecided();
            }

            if (isRematch)
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.5f, 0.5f);
            }
            else
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.5f, 0.5f);
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
        #endregion

        #region privateメソッド
        #endregion
    }
}