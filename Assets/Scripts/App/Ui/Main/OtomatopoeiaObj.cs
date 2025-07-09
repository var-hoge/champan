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
            var toPos = rectTransform.position + Vector3.up * _moveY;
            var toRot = rectTransform.eulerAngles + Vector3.forward * _rotateDeg;

            var seq = DOTween.Sequence();
            seq.Append(rectTransform.DOMove(toPos, _stagingDurationSec).SetEase(Ease.OutQuint));
            seq.Join(rectTransform.DORotate(toRot, _stagingDurationSec).SetEase(Ease.OutBack));
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