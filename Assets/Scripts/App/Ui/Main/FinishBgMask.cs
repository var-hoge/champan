using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace Ui.Main
{
    //[ExecuteInEditMode]
    public class FinishBgMask
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _theta = 0.0f;
        }

        void Update()
        {
            var material = GetComponent<UnityEngine.UI.Image>().material;

            if (material == null)
            {
                return;
            }

            _theta -= Time.deltaTime * 0.01f;
            material.SetVector("_MaskScale", _scale);
            material.SetFloat("_MaskRad2", _theta);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Vector3 _scale;

        [SerializeField]
        float _theta;

        #endregion

        #region privateメソッド
        #endregion
    }
}