using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Graphics
{
    /// <summary>
    /// OverrideColor
    /// </summary>
    public class OverrideColor
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.material = new Material(image.material);
                image.material.SetColor("_Color", _color);
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Color _color;
        #endregion

        #region privateメソッド
        #endregion
    }
}