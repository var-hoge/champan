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

            if (_isUseForceStaging is false)
            {
                _cnt = 10;
                _isFirst = false;
            }

            var isLast = _cnt == 4;

            if (isLast)
            {
                rectTransform.localScale = _initScale * 1.5f;
            }

            GetComponent<UnityEngine.UI.Image>().color = Color.white;

            var initPos = rectTransform.position;
            var initLocalEulerAngles = rectTransform.localEulerAngles;
            var initScale = rectTransform.localScale;

            var targetPos = initPos + (initPos - _center.position).normalized * (_moveDist * (isLast ? 2.0f : 1.0f));
            var seq = DOTween.Sequence();
            seq.Append(rectTransform.DOMove(targetPos, _moveDurationSec).SetEase(Ease.OutBack));

            if (_type == Type.GameFinish)
            {
                _isAutoRotateEnabled = true;
                return;
            }

            seq.AppendInterval(_intervalDurationSec * (_isFirst ? 3.0f : isLast ? 0.2f : 1.0f));

            var targetEulerAngles = initLocalEulerAngles;
            targetEulerAngles.z += _rotateDeg;
            seq.Append(rectTransform.DOLocalRotate(targetEulerAngles, _rotateDurationSec));
            var targetScale = initScale * _scaleRate;
            seq.Join(rectTransform.DOScale(targetScale, _rotateDurationSec));
            seq.Join(GetComponent<UnityEngine.UI.Image>().DOFade(0.2f, _rotateDurationSec));

            seq.OnComplete(() =>
            {
                GetComponent<UnityEngine.UI.Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            });

            _isFirst = false;
            ++_cnt;
        }

        void Update()
        {
            if (_isAutoRotateEnabled)
            {
                var rectTransform = GetComponent<RectTransform>();
                var angle = rectTransform.localEulerAngles;
                angle.z += Time.deltaTime * -16.0f / rectTransform.localScale.x;
                rectTransform.localEulerAngles = angle;
            }
        }
        #endregion

        #region privateフィールド
        enum Type
        {
            CountDown,
            GameFinish,
        }

        [SerializeField]
        Type _type;

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

        [SerializeField]
        bool _isUseForceStaging = false;

        bool _isAutoRotateEnabled = false;

        bool _isFirst = true;
        Vector3 _initPos;
        Vector3 _initLocalEulerAngles;
        Vector3 _initScale;
        int _cnt = 0;
        #endregion

        #region privateメソッド
        #endregion
    }
}