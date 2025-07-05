using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace TadaLib.Util
{
    /// <summary>
    /// InverseParentTransform
    /// </summary>
    public class InverseParentTransform
        : BaseProc
        , IProcMove
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

        #region IProcMove の実装
        public void OnMove()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            if (parent.GetComponent<RectTransform>() is { } parentRect)
            {
                var rect = GetComponent<RectTransform>();

                var parentMtx = parentRect.localToWorldMatrix;
                var mtx = rect.localToWorldMatrix;

                var targetMtx = parentMtx.inverse * mtx;

                var pos = targetMtx.MultiplyPoint3x4(Vector3.zero);
                var scale = targetMtx.lossyScale;
                var rot = Quaternion.LookRotation(targetMtx.GetColumn(2), targetMtx.GetColumn(1));

                rect.localPosition = pos;
                rect.localRotation = rot;
                rect.localScale = scale;

                return;
            }
        }
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        #endregion
    }
}