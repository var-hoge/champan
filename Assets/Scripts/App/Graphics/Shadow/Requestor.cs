using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Graphics.Shadow
{
    /// <summary>
    /// Requestor
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Requestor
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public Sprite Sprite => GetComponent<SpriteRenderer>().sprite;
        public int SpriteSortingLayer => GetComponent<SpriteRenderer>().sortingLayerID;
        public int SpriteOrder => GetComponent<SpriteRenderer>().sortingOrder;
        public float SpriteAlpha => GetComponent<SpriteRenderer>().color.a;
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            Manager.Instance.Register(this, gameObject);
        }
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        #endregion
    }
}