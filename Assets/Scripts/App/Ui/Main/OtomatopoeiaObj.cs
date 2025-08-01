using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.Main
{
    /// <summary>
    /// OtomatopoeiaObj
    /// </summary>
    public class OtomatopoeiaObj
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void Init(Sprite sprite, Vector3 playerVel)
        {
            GetComponent<UnityEngine.UI.Image>().SetSprite(sprite);

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.localScale *= (Screen.height / 1080.0f);

            var toPos = rectTransform.localPosition + Vector3.up * _moveY;
            var toRot = rectTransform.eulerAngles + Vector3.forward * _rotateDeg;

            var sec = _stagingDurationSec;
            if (GameSequenceManager.Instance != null && GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.AfterBattle)
            {
                sec *= 0.7f;
            }

            var seq = DOTween.Sequence();
            seq.Append(rectTransform.DOLocalMove(toPos, sec).SetEase(Ease.OutQuint));
            seq.Join(rectTransform.DORotate(toRot, sec).SetEase(Ease.OutBack));
            seq.Append(GetComponent<UnityEngine.UI.Image>().DOFade(0.0f, 0.2f));
            seq.OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        float _stagingDurationSec = 1.0f;

        [SerializeField]
        float _moveY = 30.0f;

        [SerializeField]
        float _rotateDeg = 30.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}