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
using UnityEngine.U2D;

namespace Ui
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class PlayerIconSlot
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void SetChara(int charaIdx)
        {
            var sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
            _body.sprite = sprite;
            _body.rectTransform.sizeDelta = sprite.textureRect.size;
            _body.rectTransform.localScale = Vector3.one * 0.6f;
            _body.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 25.0f);
            _body.rectTransform.DOPunchScale(Vector3.one * 1.5f, 0.1f);
        }

        public void UnsetChara()
        {
            _body.rectTransform.DOKill();

            _body.sprite = _unselectedSprite;
            _body.rectTransform.sizeDelta = _unselectedSprite.textureRect.size;
            _body.rectTransform.localScale = _localScaleDefault;
            _body.rectTransform.localEulerAngles = _localEulerAnglesDefault;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        UnityEngine.UI.Image _body;

        Sprite _unselectedSprite;
        Vector3 _localScaleDefault;
        Vector3 _localEulerAnglesDefault;
        #endregion

        #region privateメソッド
        void Start()
        {
            _unselectedSprite = _body.sprite;
            _localScaleDefault = _body.rectTransform.localScale;
            _localEulerAnglesDefault = _body.rectTransform.localEulerAngles;
        }
        #endregion
    }
}