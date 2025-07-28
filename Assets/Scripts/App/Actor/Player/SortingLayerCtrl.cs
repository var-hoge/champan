using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Player
{
    /// <summary>
    /// SortingLayerCtrl
    /// </summary>
    public class SortingLayerCtrl
        : BaseProc
        , IProcPostMove
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region IProcPostMove の実装
        public void OnPostMove()
        {
            var speed = Mathf.Abs(GetComponent<DataHolder>().Velocity.x);

            var order = speed switch
            {
                var sp when sp < 3.0f => 0,
                var sp when sp < 6.0f => 1,
                var sp when sp < 9.0f => 2,
                var sp when sp < 12.0f => 3,
                var sp when sp < 15.0f => 4,
                _ => 5,
            };

            _body.sortingOrder = order;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        SpriteRenderer _body;
        #endregion

        #region privateメソッド
        #endregion
    }
}