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
    /// AnimUtil
    /// </summary>
    public static class AnimUtil
    {
        #region static 関数
        public static void SetupAnimIfNeed(GameObject obj, bool isEnabled, string value = "", int activeIdx = 0)
        {
            foreach (var animCtrl in GetAnimCtrlAll(obj))
            {
                animCtrl.Setup(isEnabled, value, activeIdx);
            }
        }

        public static void PlaySelectedAnimIfNeed(GameObject obj, bool canMoveBack = false, bool canMoveNext = false)
        {
            foreach (var animCtrl in GetAnimCtrlAll(obj))
            {
                animCtrl.OnSelected(canMoveBack, canMoveNext);
            }
        }

        public static void PlayUnselectedAnimIfNeed(GameObject obj)
        {
            foreach (var animCtrl in GetAnimCtrlAll(obj))
            {
                animCtrl.OnUnselected();
            }
        }

        public static void PlayDecidedAnimIfNeed(GameObject obj)
        {
            foreach (var animCtrl in GetAnimCtrlAll(obj))
            {
                animCtrl.OnDecided();
            }
        }

        public static void PlayValueChangedAnimIfNeed(GameObject obj, string value, int activeIdx, bool isPositiveMove, bool canMoveBack, bool canMoveNext)
        {
            foreach (var animCtrl in GetAnimCtrlAll(obj))
            {
                animCtrl.OnValueChanged(value, activeIdx, isPositiveMove, canMoveBack, canMoveNext);
            }
        }
        #endregion

        #region private static 関数
        static IPageItemAnimCtrl[] GetAnimCtrlAll(GameObject obj)
        {
            return obj.GetComponents<IPageItemAnimCtrl>();
        }
        #endregion
    }
}