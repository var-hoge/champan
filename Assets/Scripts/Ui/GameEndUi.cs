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
using Scripts.Actor;
using Unity.VisualScripting;
using Scripts;

namespace Ui
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
        public async UniTask GameEnd(SimpleAnimation animation)
        {
            GameSequenceManager.Instance.PhaseKind = GameSequenceManager.Phase.AfterBattle;

            var winPlayerIdx = GameSequenceManager.WinnerPlayerIdx;
            var winCharaIdx = CharaSelectUiManager.PlayerUseCharaIdList(winPlayerIdx);

            if (winCharaIdx == 3)
            {
                // キャラと王冠が被らないようにする
                _chara.rectTransform.localPosition += new Vector3(30.0f, -50.0f, 0.0f);
            }

            var goalUi = _charaVarietySprites[winPlayerIdx];
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
        #endregion

        #region privateメソッド
        private void Start()
        {
            // 全て初期化
            _canvas.alpha = 0.0f;
            _background.gameObject.SetActive(false);
            _chara.gameObject.SetActive(false);
            _crown.gameObject.SetActive(false);
            _description.gameObject.SetActive(false);
        }
        #endregion
    }
}