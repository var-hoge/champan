using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// PlayerSelectSlotManager
    /// </summary>
    public class PlayerSelectSlotManager
        : BaseManagerProc<PlayerSelectSlotManager>
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            for (int idx = 0; idx < transform.childCount; ++idx)
            {
                var child = transform.GetChild(idx);
                if (child.GetComponent<PlayerSelectSlot>() is { } obj)
                {
                    _players.Add(obj);
                }
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        List<PlayerSelectSlot> _players = new List<PlayerSelectSlot>();
        #endregion

        #region privateメソッド
        #endregion
    }
}