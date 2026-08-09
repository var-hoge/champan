using System.Collections.Generic;
using UnityEngine;

namespace App.Actor.Gimmick.Bubble
{
    public class BubbleVisualSetter : MonoBehaviour
    {
        [SerializeField] private List<Sprite> sprites;

        [SerializeField]
        private List<SpriteRenderer> spriteRenderers;

        void Start()
        {
            // オフラインはその場で決める
            if (!Network.NetworkSession.IsOnline)
            {
                Apply(Random.Range(0, sprites.Count));
            }
        }

        void Update()
        {
            if (_isApplied || !Network.NetworkSession.IsOnline)
            {
                return;
            }

            // 見た目の種類は権威側が決めて配る。
            // 各台で乱数を引くと色が食い違うため。
            // この処理は子オブジェクトに付いているため、親をたどって探す
            var binder = GetComponentInParent<Network.NetworkGimmickBinder>();
            if (binder == null)
            {
                Apply(Random.Range(0, sprites.Count));
                return;
            }

            if (!binder.HasVisualIdx)
            {
                // まだ配られていない
                return;
            }

            Apply(binder.VisualIdx % sprites.Count);
        }

        void Apply(int idx)
        {
            _isApplied = true;

            var sprite = sprites[Mathf.Clamp(idx, 0, sprites.Count - 1)];

            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        bool _isApplied = false;
    }
}
