using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace TadaLib.Ui
{
    /// <summary>
    /// Button
    /// </summary>
    public class Button
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void OnSelected(bool doReaction = false)
        {
            _body.SetSprite(_selectedSprite);
            _body.rectTransform.localPosition = Vector3.zero;
            _body.rectTransform.localScale = Vector3.one;
            _text.color = _selectedColor;

            if (doReaction)
            {
                _body.rectTransform.DOKill();
                _body.rectTransform.DOScale(_body.rectTransform.localScale * 1.12f, 0.08f).SetLoops(2, LoopType.Yoyo);
            }
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
            GetComponent<RectTransform>().DOLocalMoveY(GetComponent<RectTransform>().localPosition.y - 10.0f, 0.15f);
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
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