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
        #region static プロパティ
        public static bool IsFirst { get; set; } = false;
        public static bool IsLast { get; set; } = false;
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            var rectTransform = GetComponent<RectTransform>();

            if (_isFirstInnter)
            {
                _isFirstInnter = false;
                _initPos = rectTransform.localPosition;
                _initLocalEulerAngles = rectTransform.localEulerAngles;
                _initScale = rectTransform.localScale;
            }
            else
            {
                rectTransform.localPosition = _initPos;
                rectTransform.localEulerAngles = _initLocalEulerAngles;
                rectTransform.localScale = _initScale;
            }

            if (_isUseForceStaging is false)
            {
                _cnt = 10;
                _isFirstInnter = false;
            }

            if (IsLast)
            {
                rectTransform.localScale = _initScale * 1.5f;
            }

            GetComponent<UnityEngine.UI.Image>().color = Color.white;

            var initPos = rectTransform.localPosition;
            var initLocalEulerAngles = rectTransform.localEulerAngles;
            var initScale = rectTransform.localScale;

            var useMoveDist = _moveDist * 0.7f;
            var targetPos = initPos + (initPos - _center.localPosition).normalized * (useMoveDist * (IsLast ? 2.0f : 1.0f));
            var seq = DOTween.Sequence();
            seq.Append(rectTransform.DOLocalMove(targetPos, _moveDurationSec).SetEase(Ease.OutBack));

            if (_type == Type.GameFinish)
            {
                _isAutoRotateEnabled = true;
                return;
            }

            seq.AppendInterval(_intervalDurationSec * (IsFirst ? 3.0f : IsLast ? 0.2f : 1.0f));

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

            _isFirstInnter = false;
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

        bool _isFirstInnter = true;
        Vector3 _initPos;
        Vector3 _initLocalEulerAngles;
        Vector3 _initScale;
        int _cnt = 0;
        #endregion

        #region privateメソッド
        #endregion
    }
}