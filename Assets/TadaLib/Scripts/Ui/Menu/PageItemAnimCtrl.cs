using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using static UnityEngine.Rendering.DebugUI;
using DG.Tweening;

namespace TadaLib.Ui.Menu.Standard
{
    /// <summary>
    /// PageItemAnimCtrl
    /// </summary>
    public class PageItemAnimCtrl
        : MonoBehaviour
        , IPageItemAnimCtrl
    {
        #region 定義
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region IPageItemAnimCtrl の実装
        public void Setup(bool isEnabled, string value, int activeIdx)
        {
            UpdateText(value);
            UpdateColor(isEnabled ? _unselectedColor : _disabledColor);
            if (isEnabled is false)
            {
                foreach (var text in _additionalColorChangableTextsForDisabled)
                {
                    text.color = _disabledColor;
                }
            }
            UpdateCursor(showBack: false, showNext: false);

            if (_cursorBack != null)
            {
                _cursorSizeRetainedBack = _cursorBack.rectTransform.localScale;

            }
            if (_cursorNext != null)
            {
                _cursorSizeRetainedNext = _cursorNext.rectTransform.localScale;
            }
        }

        public void OnSelected(bool canMoveBack, bool canMoveNext)
        {
            PlayAnim("OnSelected");

            UpdateColor(_selectedColor);
            UpdateCursor(showBack: canMoveBack, showNext: canMoveNext);
        }

        public void OnUnselected()
        {
            PlayAnim("OnUnselected");

            UpdateColor(_unselectedColor);
            UpdateCursor(showBack: false, showNext: false);
        }

        public void OnDecided()
        {
            PlayAnim("OnDecided");
        }

        public void OnValueChanged(string value, int activeIdx, bool isPositiveMove, bool canMoveBack, bool canMoveNext)
        {
            PlayAnim(isPositiveMove ? "OnValueChanged_Positive" : "OnValueChanged_Negative");

            UpdateText(value);
            UpdateCursor(showBack: canMoveBack, showNext: canMoveNext);

            var targetCursor = isPositiveMove ? _cursorNext : _cursorBack;
            var retainedSize = isPositiveMove ? _cursorSizeRetainedNext : _cursorSizeRetainedBack;
            if (targetCursor != null)
            {
                targetCursor.rectTransform.DOKill();
                targetCursor.rectTransform.localScale = retainedSize;
                targetCursor.rectTransform.DOScale(targetCursor.rectTransform.localScale * 1.12f, 0.08f).SetLoops(2, LoopType.Yoyo);
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        Color _selectedColor = Color.red;

        [SerializeField]
        Color _unselectedColor = Color.grey;

        [SerializeField]
        Color _disabledColor = Color.gray;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _valueDisplayTexts;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _colorChangableTexts;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _additionalColorChangableTextsForDisabled;

        [SerializeField]
        UnityEngine.UI.Image _cursorBack;

        [SerializeField]
        UnityEngine.UI.Image _cursorNext;

        Vector3 _cursorSizeRetainedBack = Vector3.one;
        Vector3 _cursorSizeRetainedNext = Vector3.one;
        #endregion

        #region privateメソッド
        void PlayAnim(string animName)
        {
            var simpleAnimation = GetComponent<SimpleAnimation>();
            if (simpleAnimation != null)
            {
                simpleAnimation.Play(animName);
            }
        }

        void UpdateText(string value)
        {
            foreach (var text in _valueDisplayTexts)
            {
                text.text = value;
            }
        }

        void UpdateColor(Color color)
        {
            foreach (var text in _colorChangableTexts)
            {
                text.color = color;
            }
        }

        void UpdateCursor(bool showBack, bool showNext)
        {
            if (_cursorBack != null)
            {
                _cursorBack.enabled = showBack;
            }

            if (_cursorNext != null)
            {
                _cursorNext.enabled = showNext;
            }
        }
        #endregion
    }
}