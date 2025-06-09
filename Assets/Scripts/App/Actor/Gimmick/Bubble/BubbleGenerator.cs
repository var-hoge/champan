using DG.Tweening;
using KanKikuchi.AudioManager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace App.Actor.Gimmick.Bubble
{
    public class BobbleGenerator : MonoBehaviour
    {
        [SerializeField] private int _numOfBubble = 6;
        [SerializeField] private GameObject _bubblePrafab;

        void Start()
        {
            List<Bubble> bubbles = new();
            for (var n = 0; n < _numOfBubble; ++n)
            {
                bubbles.Add(Generate());
            }

            Bubble.SetupCrown(bubbles[Random.Range(0, bubbles.Count - 1)]);
            Bubble.CrownBubble.OnDestroyEvent += TeleportCrown;
        }

        private void TeleportCrown()
        {
            if (Bubble.CrownShieldValue <= 0)
            {
                Bubble.CrownBubble.OnDestroyEvent -= TeleportCrown;
                return;
            }

            Debug.Log($"TeleportCrown\nCrownShieldValue : {Bubble.CrownShieldValue}");
            SEManager.Instance.Play(SEPath.CROWN_BUBBLE_REPOSITION);

            Transform crown = Bubble.CrownBubble.CrownSpriteRenderer;
            crown.SetParent(null);

            Bubble.CrownBubble.OnDestroyEvent -= TeleportCrown;

            var bubbles = FindObjectsByType<Bubble>(FindObjectsSortMode.None)
                            .Where(bubble => bubble.TeleportCrown)
                            .ToList();

            if (bubbles.Contains(Bubble.CrownBubble))
            {
                bubbles.Remove(Bubble.CrownBubble);
            }

            var target = (bubbles.Count == 0)
                            ? Generate()
                            : bubbles[Random.Range(0, bubbles.Count - 1)];
            Bubble.SetupCrown(target);
            crown.DOMove(Bubble.CrownBubble.transform.position, 1f).OnComplete(() => { Destroy(crown.gameObject); }).Play();

            Bubble.CrownBubble.OnDestroyEvent += TeleportCrown;
        }

        private Bubble Generate()
        {
            var position = new Vector3(Random.Range(-10f, 10f), Random.Range(-4f, 4f), 1f);
            var bubble = Instantiate(_bubblePrafab, position, Quaternion.identity);
            bubble.GetComponent<BubbleAnimator>().AnimationEnabled = true;
            return bubble.GetComponent<Bubble>();
        }
    }
}