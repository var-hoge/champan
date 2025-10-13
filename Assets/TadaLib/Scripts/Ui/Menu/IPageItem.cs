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
    /// IPageItem
    /// </summary>
    public interface IPageItem
    {
        /// <summary>
        /// 有効かどうか
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// セットアップ
        /// </summary>
        void Setup();

        /// <summary>
        /// Item にフォーカスがあった時のコールバック関数　
        /// </summary>
        void OnSelected();

        /// <summary>
        /// Item にフォーカスが外れた時のコールバック関数　
        /// </summary>
        void OnUnselected();

        /// <summary>
        /// Item が選択された時のコールバック関数
        /// </summary>
        void OnDecided();
    }
}