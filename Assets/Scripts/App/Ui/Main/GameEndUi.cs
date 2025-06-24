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
            foreach(var bubble in bubbles)
            {
                bubble.DoBurst();
            }

            await UniTask.WaitForSeconds(2.0f);

            // 勝ち点を表示
            _winCountPanel.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(6.0f);

            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.1f, 0.1f);
        }

        public async UniTask GameEnd(SimpleAnimation animation, int winnerPlayerIdx)
        {
            GameSequenceManager.Instance.PhaseKind = Phase.AfterBattle;

            var winCharaIdx = Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(winnerPlayerIdx);

            if (winCharaIdx == 3)
            {
                // キャラと王冠が被らないようにする
                _chara.rectTransform.localPosition += new Vector3(30.0f, -50.0f, 0.0f);
            }

            var goalUi = _charaVarietySprites[winnerPlayerIdx];
            var charaSprite = _charaSprites[winCharaIdx];

            _chara.sprite = charaSprite;
            _chara.rectTransform.sizeDelta = charaSprite.textureRect.size;
            _background.sprite = goalUi.BackSprite;
            _description.sprite = goalUi.WinnerSprite;

            animation.Play("GameEnd");

            await UniTask.WaitForSeconds(15.0f);

            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.5f, 0.5f);
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
        UnityEngine.UI.Image _description;

        [SerializeField]
        WinCountPanel _winCountPanel;
        #endregion

        #region privateメソッド
        private void Start()
        {
            // 全て初期化
            //_canvas.alpha = 0.0f;
            _background.gameObject.SetActive(false);
            _chara.gameObject.SetActive(false);
            _crown.gameObject.SetActive(false);
            _description.gameObject.SetActive(false);
        }
        #endregion
    }
}