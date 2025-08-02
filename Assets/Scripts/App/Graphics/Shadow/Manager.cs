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
    /// Manager
    /// </summary>
    public class Manager
        : BaseManagerProc<Manager>
        , IProcManagerUpdate
    {
        #region プロパティ
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        /// <summary>
        /// 影の登録。owner が破棄されたら自動で影の登録が解除されます。
        /// </summary>
        /// <param name="requestor"></param>
        /// <param name="owner"></param>
        public void Register(Requestor requestor, GameObject owner)
        {
            var shadowRenderer = CreateShadowRenderer();
            shadowRenderer.sortingLayerID = requestor.SpriteSortingLayer;
            shadowRenderer.sortingOrder = requestor.SpriteOrder - 1;

            _objs.Add(new Obj()
            {
                Requestor = requestor,
                Owner = owner,
                ShadowRenderer = shadowRenderer,
            });
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region IManagerProcUpdate の実装
        public void OnUpdate()
        {
            Cleanup();
            UpdateShadow();
        }
        #endregion

        #region 定義
        class Obj
        {
            public Requestor Requestor;
            public GameObject Owner;
            public SpriteRenderer ShadowRenderer;
            public Sprite Sprite;
        }
        #endregion

        #region private フィールド
        [SerializeField]
        SpriteRenderer _shadowPrefab;

        [SerializeField]
        List<SpriteRenderer> _shadowPools;

        [SerializeField]
        Vector3 _shadowOffset = new Vector3(-0.3f, -0.3f, 0.0f);

        List<Obj> _objs = new List<Obj>();
        #endregion

        #region private メソッド
        void Cleanup()
        {
            for (int idx = _objs.Count - 1; idx >= 0; --idx)
            {
                var obj = _objs[idx];
                if (obj.Owner == null || !obj.Owner.activeInHierarchy)
                {
                    if (obj.ShadowRenderer != null)
                    {
                        Destroy(obj.ShadowRenderer.gameObject);
                    }
                    _objs.RemoveAt(idx);
                }
            }
        }

        void UpdateShadow()
        {
            foreach (var obj in _objs)
            {
                // sprite の更新
                var sprite = obj.Requestor.Sprite;
                if (obj.Sprite != sprite)
                {
                    // 変わった
                    obj.ShadowRenderer.sprite = sprite;
                }
                obj.ShadowRenderer.color = obj.ShadowRenderer.color.SetAlpha(obj.Requestor.SpriteAlpha);


                // 座標の更新
                var shadowTransform = obj.ShadowRenderer.transform;
                shadowTransform.position = obj.Owner.transform.position + _shadowOffset;
                shadowTransform.rotation = obj.Owner.transform.rotation;
                shadowTransform.localScale = obj.Owner.transform.lossyScale;
            }
        }

        SpriteRenderer CreateShadowRenderer()
        {
            if (_shadowPools.Count > 0)
            {
                var shadow = _shadowPools[_shadowPools.Count - 1];
                _shadowPools.RemoveAt(_shadowPools.Count - 1);
                shadow.gameObject.SetActive(true);
                return shadow;
            }

            var obj = GameObject.Instantiate<SpriteRenderer>(_shadowPrefab);
            obj.transform.SetParent(transform);
            return obj;
        }
        #endregion
    }
}