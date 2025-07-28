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

            // スローにする
            Time.timeScale = 0.02f;

            await UniTask.WaitForSeconds(1.0f * Time.timeScale);

            Time.timeScale = 1.0f;

            GameSequenceManager.Instance.PhaseKind = Phase.AfterBattle;

            // バブルを全部壊す
            var bubbles = GameObject.FindObjectsByType<Actor.Gimmick.Bubble.Bubble>(FindObjectsSortMode.None);
            foreach (var bubble in bubbles)
            {
                bubble.DoBurst();
            }

            await UniTask.WaitForSeconds(1.5f);

            // 勝ち点を表示
            _winCountPanel.gameObject.SetActive(true);
            _continueButton.gameObject.SetActive(false);

            await UniTask.WaitForSeconds(2.3f);

            _continueButton.gameObject.SetActive(true);
            _continueButton.OnSelected();

            await UniTask.WaitForSeconds(0.03f);

            // クリックまで待つ
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            while (true)
            {
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
                    break;
                }

                await UniTask.Yield();
            }

            _continueButton.OnDecided();

            await UniTask.WaitForSeconds(0.5f);

            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.3f, 0.3f);
        }

        public async UniTask GameEnd(SimpleAnimation animation, int winnerPlayerIdx)
        {
            // スローにする
            Time.timeScale = 0.02f;

            await UniTask.WaitForSeconds(1.0f * Time.timeScale);

            Time.timeScale = 1.0f;

            GameSequenceManager.Instance.PhaseKind = Phase.AfterBattle;

            // バブルを全部壊す
            var bubbles = GameObject.FindObjectsByType<Actor.Gimmick.Bubble.Bubble>(FindObjectsSortMode.None);
            foreach (var bubble in bubbles)
            {
                bubble.DoBurst();
            }

            await UniTask.WaitForSeconds(1.2f);

            // 1 点先取で勝ちの場合はテンポ重視のため、パネルを出さない
            if (GameMatchManager.Instance.WinCountToMatchFinish != 1)
            {
                await UniTask.WaitForSeconds(0.5f);

                // 勝ち点を表示
                _winCountPanel.gameObject.SetActive(true);
                _continueButton.gameObject.SetActive(false);

                await UniTask.WaitForSeconds(2.5f);

                _continueButton.gameObject.SetActive(true);
                _continueButton.OnSelected();

                await UniTask.WaitForSeconds(0.5f);

                // クリックまで待つ
                var inputManager = TadaLib.Input.PlayerInputManager.Instance;
                while (true)
                {
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
                        break;
                    }

                    await UniTask.Yield();
                }

                _continueButton.OnDecided();

                await UniTask.WaitForSeconds(0.25f);
            }

            _ = _canvas.DOFade(0.0f, 0.2f);

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