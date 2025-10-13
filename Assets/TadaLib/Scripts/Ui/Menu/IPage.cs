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
    /// IPage
    /// </summary>
    public interface IPage
    {
        IEnumerable<IPageItem> Build();
    }
}