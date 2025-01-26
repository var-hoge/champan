using UnityEngine;
using System.Collections;
using System;
using TadaLib.ActionStd;

public class Bubble : MonoBehaviour
{
	[Serializable]
	private struct BubbleShieldConfig
	{
		public int shieldLevel;
		public Sprite sprite;
	}

	public static Bubble CrownBubble { get; set; }
	public static int CrownShieldValue { get; private set; } = 4; // TODO: Make it exportable

	private const float BurstTime = 5f;

	public Action OnDestroyEvent;

	[Header("Configurations")]
	[SerializeField] private BubbleShieldConfig[] shieldConfigs;

	[Header("References")]
	[SerializeField] private SpriteRenderer shieldSpriteRenderer;
	[SerializeField] private SpriteRenderer crownSpriteRenderer;

	private float _burstTimer = BurstTime;
	private bool _hasRidden = false;
	private MoveInfoCtrl _moveInfoCtrl = null;

	private bool HasCrown => CrownBubble == this;

	void Start()
	{
		_moveInfoCtrl = GetComponent<MoveInfoCtrl>();
	}

	void OnDestroy()
	{
		OnDestroyEvent?.Invoke();
	}

	void Update()
	{
		var isRiding = _moveInfoCtrl.IsRiding();
		if (isRiding)
		{
			_hasRidden = true;
			_burstTimer -= Time.deltaTime;

			if (_burstTimer < 0)
			{
				Destroy(gameObject);
			}
		}

		if (!isRiding && _hasRidden)
		{
			if (HasCrown)
			{
				CrownShieldValue--;

				// TODO: Push back the player
				// TODO: Update crown ownership

				Destroy(gameObject);
				return;
			}

			_burstTimer = BurstTime;

			// TODO: Fix has it might be conflicting with other players currently ridding
			_hasRidden = _moveInfoCtrl.RideObjects.Count > 0;
		}
	}

	public static void SetupCrown(Bubble bubble)
	{
		if (CrownBubble != null)
		{
			CrownBubble.RemoveCrown();
		}

		CrownBubble = bubble;

		bubble.SetupCrown(CrownShieldValue);
	}

	private void RemoveCrown()
	{
		// TODO: If supporting bubble shield, reset original shield here

		crownSpriteRenderer.gameObject.SetActive(false);
		shieldSpriteRenderer.gameObject.SetActive(false);
	}

	private void SetupCrown(int shieldValue)
	{
		gameObject.name = "BubbleCrown";

		BubbleShieldConfig config = default;
		foreach (var shieldConfig in shieldConfigs)
		{
			if (shieldConfig.shieldLevel == shieldValue)
			{
				config = shieldConfig;
				break;
			}
		}

		crownSpriteRenderer.gameObject.SetActive(true);

		shieldSpriteRenderer.gameObject.SetActive(true);
		shieldSpriteRenderer.sprite = config.sprite;
	}

	public void Init(float x)
	{
		GetComponent<Rigidbody2D>().AddForce(new Vector2(x, UnityEngine.Random.Range(15f, 90f)));
		StartCoroutine(EnableAnimation());
	}

	private IEnumerator EnableAnimation()
	{
		yield return new WaitForSeconds(5f);
		GetComponent<BubbleAnimator>().AnimationEnabled = true;
	}
}
