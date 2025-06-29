using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace Ui.Main
{
    /// <summary>
    /// CountDownEffect
    /// </summary>
    public class CountDownEffect
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            var rectTransform = GetComponent<RectTransform>();

            if (_isFirst)
            {
                _isFirst = false;
                _initPos = rectTransform.position;
                _initLocalEulerAngles = rectTransform.localEulerAngles;
                _initScale = rectTransform.localScale;
            }
            else
            {
                rectTransform.position = _initPos;
                rectTransform.localEulerAngles = _initLocalEulerAngles;
                rectTransform.localScale = _initScale;
            }

            GetComponent<UnityEngine.UI.Image>().color = Color.white;

            var initPos = rectTransform.position;
            var initLocalEulerAngles = rectTransform.localEulerAngles;
            var initScale = rectTransform.localScale;

            var targetPos = initPos + (initPos - _center.position).normalized * _moveDist;
            var seq = DOTween.Sequence();
            seq.Append(rectTransform.DOMove(targetPos, _moveDurationSec));

            seq.AppendInterval(_intervalDurationSec);

            var targetEulerAngles = initLocalEulerAngles;
            targetEulerAngles.z += _rotateDeg;
            seq.Append(rectTransform.DOLocalRotate(targetEulerAngles, _rotateDurationSec));
            var targetScale = rectTransform.localScale * _scaleRate;
            seq.Join(rectTransform.DOScale(targetScale, _rotateDurationSec));
            seq.Join(GetComponent<UnityEngine.UI.Image>().DOFade(0.2f, _rotateDurationSec));

            seq.OnComplete(() =>
            {

                GetComponent<UnityEngine.UI.Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            });
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        RectTransform _center;

        [SerializeField]
        float _moveDist = 50.0f;
        [SerializeField]
        float _moveDurationSec = 0.3f;

        [SerializeField]
        float _intervalDurationSec = 0.5f;

        [SerializeField]
        float _rotateDeg = 60.0f;
        [SerializeField]
        float _scaleRate = 0.5f;
        [SerializeField]
        float _rotateDurationSec = 0.3f;

        bool _isFirst = true;
        Vector3 _initPos;
        Vector3 _initLocalEulerAngles;
        Vector3 _initScale;
        #endregion

        #region privateメソッド
        #endregion
    }
}