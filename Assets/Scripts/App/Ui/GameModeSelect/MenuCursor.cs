using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.GameModeSelect
{
    /// <summary>
    /// MenuCursor
    /// </summary>
    public class MenuCursor
        : MonoBehaviour
        , TadaLib.Ui.Menu.ICursor
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _defaultAlpha = GetComponent<CanvasGroup>().alpha;
            _menuCtrl.SetCursor(this);
        }
        #endregion

        #region ICursor の実装
        public void SetupCursor(int activePageItemIdx)
        {
            GetComponent<RectTransform>().position = _itemCenterPosList[activePageItemIdx].position;
        }

        public void OnActiveItemChanged(int activePageItemIdx)
        {
            GetComponent<RectTransform>().DOKill();
            GetComponent<RectTransform>().DOMove(_itemCenterPosList[activePageItemIdx].position, 0.2f);

            // 最後の要素なら透明にする
            var nextAlpha = (activePageItemIdx == _itemCenterPosList.Count - 1) ? 0.0f : _defaultAlpha;
            GetComponent<CanvasGroup>().DOFade(nextAlpha, 0.1f);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        TadaLib.Ui.Menu.MenuCtrl _menuCtrl;

        [SerializeField]
        List<RectTransform> _itemCenterPosList;

        float _defaultAlpha;
        #endregion

        #region privateメソッド
        #endregion
    }
}