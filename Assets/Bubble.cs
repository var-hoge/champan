using UnityEngine;
using System.Collections;
using System;
using TadaLib.ActionStd;
using Scripts;
using UnityEngine.Rendering;
using Scripts.Actor.Player;

public class Bubble : MonoBehaviour
{
    [Serializable]
    private struct BubbleShieldConfig
    {
        public int shieldLevel;
        public Sprite sprite;
    }

    public static Bubble CrownBubble { get; set; }
    public static int CrownShieldValue { get; private set; }

    private const float BurstTime = 5f;

    public Action OnDestroyEvent;

    public bool IsSpawning { get; private set; }

    [Header("Configurations")]
    [SerializeField] private int crownStartShield;
    [SerializeField] private BubbleShieldConfig[] shieldConfigs;

    [SerializeField] private float minSize = 2.2f;
    [SerializeField] private float maxSize = 4.5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer shieldSpriteRenderer;
    [SerializeField] private SpriteRenderer crownSpriteRenderer;

    [Header("Amplitude")]
    [SerializeField] private float amplitude = 0.01f;

    private float _burstTimer;
    private bool _hasRidden = false;
    private MoveInfoCtrl _moveInfoCtrl = null;
    private BubbleShieldConfig currentShieldConfig;
    private int currentShieldValue;
    private Transform visualRoot;
    private bool _vibrating = false;
    private IEnumerator _vibrateCoroutine = null;
    private int currentRiders;

    public static int LastCrownRidePlayerIdx = 0;

    public Transform CrownSpriteRenderer => crownSpriteRenderer.transform;
    private bool HasCrown => CrownBubble == this;

    void Start()
    {
        _moveInfoCtrl = GetComponent<MoveInfoCtrl>();
        visualRoot = transform.Find("VisualRoot").transform;

        _burstTimer = BurstTime;

        if (TryGetComponent(out SortingGroup sortingGroup))
            sortingGroup.sortingOrder = UnityEngine.Random.Range(0, 10000);

        transform.localScale = Vector3.one * UnityEngine.Random.Range(minSize, maxSize);
    }

    void OnDestroy()
    {
        OnDestroyEvent?.Invoke();
    }

    void Update()
    {
        if (GameSequenceManager.Instance.PhaseKind != GameSequenceManager.Phase.Battle)
        {
            return;
        }

        bool losingRider = currentRiders > _moveInfoCtrl.RideObjects.Count;
        currentRiders = _moveInfoCtrl.RideObjects.Count;

        var isRiding = _moveInfoCtrl.IsRiding();
        if (isRiding)
        {
            if (HasCrown)
            {
                LastCrownRidePlayerIdx = _moveInfoCtrl.RideObjects[0].GetComponent<DataHolder>().PlayerIdx;
            }
            _burstTimer -= Time.deltaTime;
            _hasRidden = true;

            if (_burstTimer < 1.5 && !_vibrating)
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

        if ((!isRiding && _hasRidden) || losingRider)
        {
            if (HasCrown)
            {
                CrownShieldValue--;

                // TODO: Push back the player
            }

            Destroy(gameObject);
        }

        // prevent bubble from staying at the bottom of the screen
        {
            var camera = Camera.main;
            var bottomRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, 0.0f, camera.nearClipPlane));
            if (transform.position.y < bottomRight.y)
            {
                GetComponent<Rigidbody2D>().AddForce(Vector2.up * 1f * Time.timeScale);
            }
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
        else
        {
            CrownShieldValue = bubble.crownStartShield;
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
            if (shieldConfig.shieldLevel > shieldValue) continue;

            config = shieldConfig;
            break;
        }

        crownSpriteRenderer.gameObject.SetActive(true);

        shieldSpriteRenderer.gameObject.SetActive(true);
        shieldSpriteRenderer.sprite = config.sprite;
    }

    public void Init(float x)
    {
        IsSpawning = true;
        GetComponent<Rigidbody2D>().AddForce(new Vector2(x, UnityEngine.Random.Range(15f, 90f)));
        StartCoroutine(EnableAnimation());
    }

    private IEnumerator EnableAnimation()
    {
        yield return new WaitForSeconds(5f);
        GetComponent<BubbleAnimator>().AnimationEnabled = true;
        IsSpawning = false;
    }
}
