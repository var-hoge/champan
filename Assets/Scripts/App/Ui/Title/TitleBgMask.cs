using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.Title
{
    [ExecuteInEditMode]
    public class TitleBgMask
        : MonoBehaviour
    {
        #region プロパティ
        public Vector3 Scale => _scale;
        #endregion

        #region メソッド
        public void SetScale(Vector3 value)
        {
            _scale = value;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }

        void Update()
        {
#if UNITY_EDITOR
            {
                if (Application.isPlaying is false)
                {
                    if(_isDebugUpdateEnabled is false)
                    {
                        return;
                    }
                }
            }
#endif

            var material = GetComponent<UnityEngine.UI.Image>().material;

            if (material == null)
            {
                return;
            }

            material.SetVector("_MaskPos", _trans);
            material.SetVector("_MaskScale", _scale);
            material.SetFloat("_MaskRad", _rotRad);

        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Vector3 _trans;

        [SerializeField]
        float _rotRad;

        [SerializeField]
        Vector3 _scale = Vector3.one;

#if UNITY_EDITOR
        [Header("エディタモードでも更新")]
        [SerializeField]
        bool _isDebugUpdateEnabled = true;
#endif
        #endregion

        #region privateメソッド
        #endregion
    }
}