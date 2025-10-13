using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.Common
{
    /// <summary>
    /// ButtonAnimCtrl
    /// </summary>
    public class ButtonAnimCtrl
        : MonoBehaviour
        , TadaLib.Ui.Menu.IPageItemAnimCtrl
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region IPageItemAnimCtrl の実装
        public void Setup(bool isEnabled, string value, int activeIdx)
        {
            // 非選択状態にする
            OnUnselected();
        }

        public void OnSelected(bool canMoveBack, bool canMoveNext)
        {
            _body.SetSprite(_selectedSprite);
            _body.rectTransform.localPosition = Vector3.zero;
            _body.rectTransform.localScale = Vector3.one;
            _text.color = _selectedColor;

            _body.rectTransform.DOKill();
            _body.rectTransform.DOScale(_body.rectTransform.localScale * 1.12f, 0.08f).SetLoops(2, LoopType.Yoyo);
        }

        public void OnUnselected()
        {
            _body.SetSprite(_unselectedSprite);
            _body.rectTransform.localPosition = _unselectedOffset;
            _body.rectTransform.localScale = _unselectedScale;
            _text.color = _unselectedColor;
        }

        public void OnDecided()
        {
            GetComponent<RectTransform>().DOLocalMoveY(GetComponent<RectTransform>().localPosition.y - 14.0f, 0.07f);
        }

        public void OnValueChanged(string value, int activeIdx, bool isPositiveMove, bool canMoveBack, bool canMoveNext)
        {
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        UnityEngine.UI.Image _body;
        [SerializeField]
        TMPro.TextMeshProUGUI _text;

        [SerializeField]
        Sprite _selectedSprite;
        [SerializeField]
        Color _selectedColor = Color.white;

        [SerializeField]
        Sprite _unselectedSprite;
        [SerializeField]
        Color _unselectedColor = new Color(92 / 255.0f, 85 / 255.0f, 77 / 255.0f, 1.0f);
        [SerializeField]
        Vector3 _unselectedOffset;
        [SerializeField]
        Vector3 _unselectedScale = Vector3.one;
        #endregion

        #region privateメソッド
        #endregion
    }
}