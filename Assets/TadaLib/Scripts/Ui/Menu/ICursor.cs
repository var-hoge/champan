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
    /// ICursor
    /// </summary>
    public interface ICursor
    {
        void SetupCursor(int activePageItemIdx);

        void OnActiveItemChanged(int activePageItemIdx);
    }
}