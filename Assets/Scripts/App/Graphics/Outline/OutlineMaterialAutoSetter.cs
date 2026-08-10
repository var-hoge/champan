using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Graphics.Outline
{
    /// <summary>
    /// OutlineMaterialAutoSetter
    /// </summary>
    public class OutlineMaterialAutoSetter
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            ApplyMaterials();
        }

        void Update()
        {
            // 勝者は試合が決まるまで分からない。
            // ネットワーク対戦では、決まったことが届くのが遅れることもあり、
            // Start の時点では別の席を指していることがある。
            var playerIdx = OutlineManager.Instance.ResolvePlayerIdx(_outlineKind);
            if (playerIdx != _appliedPlayerIdx)
            {
                ApplyMaterials();
            }

            if (_spritePrev == null)
            {
                return;
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }


            if (spriteRenderer.sprite != _spritePrev)
            {
                // @memo: ShaderGraph で作ったマテリアルをセットした場合、この方法でスプライトを差し替えないといけないらしい
                //        さもないと、スプライトの変更が反映されない
                var mpb = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(mpb);
                _spritePrev = spriteRenderer.sprite;
                mpb.SetTexture("_MainTex", _spritePrev.texture);
                spriteRenderer.SetPropertyBlock(mpb);
            }

        }
        #endregion

        #region privateフィールド
        [SerializeField]
        OutlineManager.OutlineKind _outlineKind;

        [SerializeField]
        bool _considerCpu = true;

        Sprite _spritePrev = null;

        /// <summary>
        /// 今あてているマテリアルが、どの席のものか
        /// </summary>
        int _appliedPlayerIdx = -1;
        #endregion

        #region privateメソッド
        void ApplyMaterials()
        {
            _appliedPlayerIdx = OutlineManager.Instance.ResolvePlayerIdx(_outlineKind);

            {
                if (OutlineManager.Instance.TryGetOutlineMaterial(_outlineKind, _considerCpu, out var material))
                {
                    var spriteRenderer = GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sharedMaterial = material;
                        _spritePrev = spriteRenderer.sprite;
                    }
                }
            }

            {
                if (OutlineManager.Instance.TryGetOutlineMaterialForImage(_outlineKind, _considerCpu, out var material))
                {
                    var image = GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        image.material = material;

                        // ShaderGraph で作ったマテリアルは、当て直しただけでは
                        // 表示中のスプライトが反映されないことがある
                        image.SetMaterialDirty();
                    }
                }
            }
        }
        #endregion
    }
}