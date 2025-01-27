using DG.Tweening;
using KanKikuchi.AudioManager;
using Scripts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BobbleGenerator : MonoBehaviour
{
	[SerializeField] private int _numOfBubble = 6;
	[SerializeField] private GameObject _bubblePrafab;

	void Start()
	{
		List<Bubble> bubbles = new();
		for (var n = 0; n < _numOfBubble; ++n)
		{
			var position = new Vector3(Random.Range(-10f, 10f), Random.Range(-4f, 4f), 1f);
			var bubble = Instantiate(_bubblePrafab, position, Quaternion.identity);
			bubble.GetComponent<BubbleAnimator>().AnimationEnabled = true;
			bubbles.Add(bubble.GetComponent<Bubble>());
		}

		Bubble.SetupCrown(bubbles[Random.Range(0, bubbles.Count - 1)]);
		Bubble.CrownBubble.OnDestroyEvent += TeleportCrown;
	}

	private void TeleportCrown()
	{
		Debug.Log($"TeleportCrown");
		SEManager.Instance.Play(SEPath.CROWN_BUBBLE_REPOSITION);

		if (Bubble.CrownShieldValue == 0)
		{
			GameSequenceManager.WinnerPlayerIdx = Bubble.LastCrownRidePlayerIdx;
			GameSequenceManager.Instance.GameOver();
			return;
		}

		Transform crown = Bubble.CrownBubble.CrownSpriteRenderer;
		crown.SetParent(null);

		Bubble.CrownBubble.OnDestroyEvent -= TeleportCrown;

		List<Bubble> bubbles = (FindObjectsByType(typeof(Bubble), FindObjectsSortMode.None) as Bubble[]).ToList();
		if (bubbles.Count == 0) return;

		bubbles = bubbles.Where(x => !x.IsSpawning).ToList();

		if (bubbles.Contains(Bubble.CrownBubble))
		{
			bubbles.Remove(Bubble.CrownBubble);
		}

		Bubble.SetupCrown(bubbles[Random.Range(0, bubbles.Count - 1)]);
		crown.DOMove(Bubble.CrownBubble.transform.position, 1f).OnComplete(() => { Destroy(crown.gameObject); }).Play();

		Bubble.CrownBubble.OnDestroyEvent += TeleportCrown;
	}
}
