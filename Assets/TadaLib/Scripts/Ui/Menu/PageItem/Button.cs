using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace TadaLib.Ui.Menu.PageItem
{
    /// <summary>
    /// Button
    /// </summary>
    public class Button : IPageItem
    {
        #region 定義
        public struct Recipe
        {
            public GameObject Obj;
            public System.Action Decided;
            public System.Func<bool> IsEnabledFunc;
        }
        #endregion

        #region コンストラクタ
        public Button(in Recipe recipe)
        {
            _obj = recipe.Obj;
            _decided = recipe.Decided;
            _isEnabledFunc = recipe.IsEnabledFunc;
        }
        #endregion

        #region プロパティ
        #endregion

        #region IPageItem の実装
        public bool IsEnabled => _isEnabledFunc();

        public void Setup()
        {
            AnimUtil.SetupAnimIfNeed(_obj, IsEnabled);
        }

        public void OnSelected()
        {
            AnimUtil.PlaySelectedAnimIfNeed(_obj);
        }

        public void OnUnselected()
        {
            AnimUtil.PlayUnselectedAnimIfNeed(_obj);
        }

        public void OnDecided()
        {
            _decided?.Invoke();
            AnimUtil.PlayDecidedAnimIfNeed(_obj);
        }
        #endregion

        #region private フィールド
        readonly GameObject _obj;
        event System.Action _decided;
        System.Func<bool> _isEnabledFunc;
        #endregion
    }
}