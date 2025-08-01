using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.Title
{
    /// <summary>
    /// CharaImageChanger
    /// </summary>
    public class CharaImageChanger
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void Change()
        {
            _bodyMain.DOFade(0.0f, 0.4f);
            _bodyCharaSelect.gameObject.SetActive(true);
            _bodyCharaSelect.color = _bodyCharaSelect.color.SetAlpha(0.0f);
            _bodyCharaSelect.DOFade(1.0f, 0.4f);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        UnityEngine.UI.Image _bodyMain;

        [SerializeField]
        UnityEngine.UI.Image _bodyCharaSelect;
        #endregion

        #region privateメソッド
        #endregion
    }
}