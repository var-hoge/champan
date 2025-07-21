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
    /// RunAwayCrown
    /// </summary>
    public class RunAwayCrown
        : MonoBehaviour
    {
        #region static メソッド
        public static RunAwayCrown Create(Vector3 pos, float scale)
        {
            scale *= 0.4f;

            var prefab = Manager.Instance.Setting.RunAwayCrownPrefab;
            var crown = GameObject.Instantiate<RunAwayCrown>(prefab);
            crown.transform.position = pos;
            crown.transform.localScale = new Vector3(scale, scale, scale);

            crown.GetComponent<SimpleAnimation>().Play("RunAway");

            return crown;
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        private void Update()
        {
            _lifeTime -= Time.deltaTime;
            if (_lifeTime < 0.0f)
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        float _lifeTime = 1.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}