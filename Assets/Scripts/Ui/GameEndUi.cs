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
        public async UniTask GameEnd()
        {
            GameSequenceManager.Instance.PhaseKind = GameSequenceManager.Phase.AfterBattle;

            var winPlayerIdx = GameSequenceManager.WinnerPlayerIdx;
            var winCharaIdx = CharaSelectUiManager.PlayerUseCharaIdList(winPlayerIdx);

            var goalUi = _charaVarietySprites[winPlayerIdx];
            var charaSprite = _charaSprites[winCharaIdx];

            _ = _canvas.DOFade(1.0f, 0.1f);

            _background.gameObject.SetActive(true);
            _background.rectTransform.localPosition = Vector3.down * 10.0f;

            _ = _background.rectTransform.DOLocalMoveY(0.0f, 0.1f);

            await UniTask.WaitForSeconds(0.7f);

            _chara.gameObject.SetActive(true);
            _chara.sprite = charaSprite;
            _chara.rectTransform.sizeDelta = charaSprite.textureRect.size;

            _background.sprite = goalUi.BackSprite;

            _crown.gameObject.SetActive(true);
            _description.sprite = goalUi.WinnerSprite;

            _chara.color.SetAlpha(0.0f);
            _crown.color.SetAlpha(0.0f);
            _ = _chara.DOFade(1.0f, 2.2f);
            _ = _crown.DOFade(1.0f, 2.2f);

            var charaGoalPos = _chara.rectTransform.position;
            _chara.rectTransform.position += Vector3.down * 50.0f;

            _ = _chara.rectTransform.DOMove(charaGoalPos, 0.5f);

            var crownGoalPos = _crown.rectTransform.position;
            _chara.rectTransform.position += Vector3.up * 50.0f;

            _ = _crown.rectTransform.DOMove(crownGoalPos, 0.5f);

            await UniTask.WaitForSeconds(0.5f);

            _description.gameObject.SetActive(true);
            _description.rectTransform.localPosition = Vector3.down * 15.0f;

            _ = _background.rectTransform.DOLocalMoveY(0.0f, 0.3f);

            await UniTask.WaitForSeconds(10.0f);

            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Result", 0.5f, 0.5f);
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
        #endregion
    }
}