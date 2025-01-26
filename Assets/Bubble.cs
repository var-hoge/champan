using UnityEngine;
using TadaLib.ActionStd;
using System.Linq;
using System.Collections;

public class Bubble : MonoBehaviour
{
	[System.Serializable]
	private struct BubbleShieldConfig
	{
		public int shieldLevel;
		public int probability;
		public Sprite sprite;
	}

	private const float BurstTime = 5f;

	[Header("Configurations")]
	[SerializeField] private BubbleShieldConfig[] shieldConfigs;

	[Header("References")]
	[SerializeField] private SpriteRenderer shieldSpriteRenderer;

	private float _burstTimer = BurstTime;
	private bool _hasRidden = false;
	private MoveInfoCtrl _moveInfoCtrl = null;
	private BubbleShieldConfig currentShieldConfig;
	private int currentShieldValue;

	void Start()
	{
		_moveInfoCtrl = GetComponent<MoveInfoCtrl>();

		int totalProbability = shieldConfigs.Sum(x => x.probability);
		int pickProbability = Random.Range(0, totalProbability + 1);
		int progressProbablity = 0;
		foreach (var shieldConfig in shieldConfigs)
		{
			progressProbablity += shieldConfig.probability;
			if (pickProbability <= progressProbablity)
			{
				currentShieldConfig = shieldConfig;
				break;
			}
		}

		currentShieldValue = currentShieldConfig.shieldLevel;
		UpdateShield();
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
			currentShieldValue--;

			if (currentShieldValue == 0)
			{
				Destroy(gameObject);
				return;
			}

			UpdateShield();

			_burstTimer = BurstTime;
			_hasRidden = false;
		}
	}

	private void UpdateShield()
	{
		if (currentShieldValue <= 0)
		{
			shieldSpriteRenderer.gameObject.SetActive(false);
			return;
		}

		BubbleShieldConfig config = default;
		foreach (var shieldConfig in shieldConfigs)
		{
			if (shieldConfig.shieldLevel == currentShieldValue)
			{
				config = shieldConfig;
				break;
			}
		}

		shieldSpriteRenderer.sprite = config.sprite;
	}

	public void Init(float x)
	{
		GetComponent<Rigidbody2D>().AddForce(new Vector2(x, Random.Range(15f, 90f)));
		StartCoroutine(EnableAnimation());
	}

	private IEnumerator EnableAnimation()
	{
		yield return new WaitForSeconds(5f);
		GetComponent<BubbleAnimator>().AnimationEnabled = true;
	}
}
