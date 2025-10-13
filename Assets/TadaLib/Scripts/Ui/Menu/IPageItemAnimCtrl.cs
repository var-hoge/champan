using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace TadaLib.Ui.Menu
{
    /// <summary>
    /// IPageItemAnimCtrl
    /// </summary>
    public interface IPageItemAnimCtrl
    {
        void Setup(bool isEnabled, string value, int activeIdx);

        void OnSelected(bool canMoveBack, bool canMoveNext);

        void OnUnselected();

        void OnDecided();

        void OnValueChanged(string value, int activeIdx, bool isPositiveMove, bool canMoveBack, bool canMoveNext);
    }
}