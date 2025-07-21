using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Gimmick.Crown
{
    /// <summary>
    /// FinishCrown
    /// </summary>
    public class FinishCrown
        : MonoBehaviour
    {
        #region static メソッド
        public static FinishCrown Create(Vector3 pos, float scale)
        {
            var prefab = Manager.Instance.Setting.FinishCrownPrefab;
            var finishCrown = GameObject.Instantiate<FinishCrown>(prefab);
            finishCrown.transform.position = pos;
            //finishCrown.transform.localScale = new Vector3(scale, scale, scale);

            finishCrown.GetComponent<SimpleAnimation>().Play("Appear");

            return finishCrown;
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド

        #endregion

        #region MonoBehavior の実装
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        #endregion
    }
}