using System.Collections.Generic;
using UnityEngine;

public class BubbleVisualSetter : MonoBehaviour
{
	[SerializeField] private List<Sprite> sprites;

	private SpriteRenderer spriteRenderer;

	void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.sprite = sprites[Random.Range(0, sprites.Count - 1)];
	}
}
