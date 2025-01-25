using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Scripts.Actor.Gimmick.CrownBubble
{
    class BubbleLayer : MonoBehaviour
    {
        public float Radius { private set; get; }

        public void Setup(float radius, int sortingOrder)
        {
            Radius = radius;
            _meshTransform.localScale = Vector3.one * radius;   

            for(int idx = 0; idx < _meshTransform.childCount; ++idx)
            {
                if(_meshTransform.GetChild(idx).TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    renderer.sortingOrder = sortingOrder;
                }
            }
        }

        public void OnBreak()
        {
            gameObject.SetActive(false);
        }

        [SerializeField]
        Transform _meshTransform;
    }
}
