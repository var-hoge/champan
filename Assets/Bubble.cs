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

	[Header("Amplitude")]
	[SerializeField] private float amplitude = 0.01f;

	private float _burstTimer = BurstTime;
	private bool _hasRidden = false;
	private MoveInfoCtrl _moveInfoCtrl = null;
	private BubbleShieldConfig currentShieldConfig;
	private int currentShieldValue;
	private Transform visualRoot;
	private bool _vibrating = false;
	private IEnumerator _vibrateCoroutine = null;

	private bool HasCrown => CrownBubble == this;

	void Start()
	{
		_moveInfoCtrl = GetComponent<MoveInfoCtrl>();
		visualRoot = transform.Find("VisualRoot").transform;
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

			if (_burstTimer < 1.5
				&& !_vibrating)
			{
				_vibrateCoroutine = Vibrate();
				StartCoroutine(_vibrateCoroutine);
				_vibrating = true;
			}

			if (_burstTimer < 0)
			{
				Destroy(gameObject);
			}
		}
		else
		{
			if (_vibrateCoroutine != null)
			{
				StopCoroutine(_vibrateCoroutine);
				_vibrateCoroutine = null;
			}
			_burstTimer = BurstTime;
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

	private IEnumerator Vibrate()
	{
		var sign = 1f;
		while (true)
		{
			yield return new WaitForSeconds(0.03f);
			visualRoot.localPosition = new(amplitude * sign, 0, 0);
			sign *= -1;
		}
	}

	private void UpdateShield()
	{
		if (currentShieldValue <= 0)
		{
			shieldSpriteRenderer.gameObject.SetActive(false);
			return;
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
