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
            var sprite = sprites[Random.Range(0, sprites.Count - 1)];

            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.sprite = sprite; ;
            }
        }
    }
}