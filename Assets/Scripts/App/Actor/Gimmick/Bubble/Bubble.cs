using UnityEngine;
using System.Collections;
using System;
using TadaLib.ActionStd;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using KanKikuchi.AudioManager;
using DG.Tweening;

namespace App.Actor.Gimmick.Bubble
{
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

        [Header("Burst Time")]
        [SerializeField] private float burstTime = 5f;

        [SerializeField] float blowPower = 12.0f;

        [SerializeField] private MoveInfoCtrl _moveInfoCtrl;

        [SerializeField] private GameObject _bubPopEff;
        [SerializeField] private Transform _shieldCountRoot;
        [SerializeField] private GameObject _shieldCountPrefab;

        [Header("Crown Effect")]
        [SerializeField] private ParticleSystem[] crownParticles;
        [SerializeField] private ParticleSystem crownGlitter;

        [SerializeField] private Sprite InactiveShieldSprite;

        private float _burstTimer;
        private float _burstGracePeriod = 0.05f;
        private bool _hasRidden = false;
        private bool _isBursting = false;
        private BubbleShieldConfig currentShieldConfig;
        private int currentShieldValue;
        private Transform visualRoot;
        private bool _vibrating = false;
        private IEnumerator _vibrateCoroutine = null;
        private int currentRiders;

        public static int LastCrownRidePlayerIdx = 0;
        private static Vector3 TopRight = Vector3.positiveInfinity;

        public Transform CrownSpriteRenderer => crownSpriteRenderer.transform;
        public bool HasCrown => CrownBubble == this;

        private List<string> _SEPath = null;
        private List<string> SEPath => _SEPath ??= Sound.SePathGenerator.GetSEPath("SE/Bubble Jump/Bubble_Jump_", 11).ToList();
        private bool IsRidden => _moveInfoCtrl.IsRidden;
        private bool _isRidenPrev = false;
        public bool TeleportCrown => !IsSpawning && IsOnScreen() && !IsRidden;
        private bool IsCrownBubble => shieldSpriteRenderer.gameObject.activeSelf;

        // Bust bubble
        public void DoBurst(int playerIdx = -1)
        {
            if (playerIdx >= 0)
            {
                LastCrownRidePlayerIdx = playerIdx;
            }
            BurstImpl();
        }

        void Start()
        {
            visualRoot = transform.Find("VisualRoot").transform;

            _burstTimer = burstTime;

            if (TryGetComponent(out SortingGroup sortingGroup))
                sortingGroup.sortingOrder = UnityEngine.Random.Range(0, 10000);

            transform.localScale = Vector3.one * UnityEngine.Random.Range(minSize, maxSize);

            if (TopRight.Equals(Vector3.positiveInfinity))
            {
                var camera = Camera.main;
                TopRight = camera.ScreenToWorldPoint(new(Screen.width, Screen.height, camera.nearClipPlane));
            }

            _isRidenPrev = true;
        }

        void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }

        void Update()
        {
            if (GameSequenceManager.Instance == null)
            {
                return;
            }

            if (GameSequenceManager.Instance.PhaseKind != GameSequenceManager.Phase.Battle
                || _isBursting)
            {
                return;
            }

            bool losingRider = currentRiders > _moveInfoCtrl.RideObjects.Count;
            currentRiders = _moveInfoCtrl.RideObjects.Count;

            if (IsRidden)
            {
                if (!_isRidenPrev)
                {
                    _isRidenPrev = true;
                    GetComponent<BubbleAnimator>().OnRide();
                }

                if (HasCrown)
                {
                    LastCrownRidePlayerIdx = _moveInfoCtrl.RideObjects[0].GetComponent<Player.DataHolder>().PlayerIdx;
                }

                if (_burstTimer == burstTime)
                {
                    PlaySE();
                    GetComponent<BubbleAnimator>().AnimationEnabled = true;
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
                    BurstImpl();
                }

                // 複数のプレイヤーが乗っている場合、乗っている全てのプレイヤーを飛ばす
                var players = _moveInfoCtrl.RideObjects;
                var count = HasCrown ? 0 : 1;
                if (players.Count > count)
                {
                    foreach (var player in players)
                    {
                        BubbleUtil.Blow(player, transform.position, blowPower);
                    }
                }
            }
            else
            {
                _isRidenPrev = false;
            }


            // プレイヤーの着地挙動が安定したので、下記はコメントアウト

            //_burstGracePeriod = ((!isRiding && _hasRidden) || losingRider)
            //                    ? _burstGracePeriod - Time.deltaTime
            //                    : 0.05f;
            //if (_burstGracePeriod < 0)
            //{
            //    BurstImpl();
            //}

            if ((!IsRidden && _hasRidden) || losingRider)
            {
                BurstImpl();
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

            // クラウンバブルの場合、シールドを回転
            if (IsCrownBubble)
            {
                _shieldCountRoot.Rotate(Vector3.forward, Time.deltaTime * 15f);
            }
        }

        private void PlaySE()
        {
            var path = SEPath[UnityEngine.Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);
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

            crownSpriteRenderer.transform.DOScale(Vector3.one, 0.15f);

            shieldSpriteRenderer.gameObject.SetActive(true);
            shieldSpriteRenderer.sprite = config.sprite;

            // エフェクトの再生
            crownGlitter.Play(false);
            if (shieldValue <= 1)
            {
                foreach (var particle in crownParticles)
                {
                    particle.Play(false);
                }
            }

            // シールドの残数を表示
            if (shieldValue > 1)
            {
                DisplayShieldCount(shieldValue);
            }
        }

        /// <summary>
        /// シールドの残数を表示
        /// </summary>
        /// <param name="shieldValue">シールドの残数</param>
        private void DisplayShieldCount(int shieldValue)
        {
            var angle = -360 / 5f;
            for (var n = 1; n <= 5; ++n)
            {
                var shieldCount = Instantiate(_shieldCountPrefab, _shieldCountRoot).transform;
                shieldCount.localPosition = Vector3.up * 0.7f;
                shieldCount.RotateAround(_shieldCountRoot.position, Vector3.forward, angle * n);

                if (n > shieldValue)
                {
                    shieldCount.GetComponent<SpriteRenderer>().sprite = InactiveShieldSprite;
                }
            }
        }

        public void Init(float x)
        {
            IsSpawning = true;
            var y = UnityEngine.Random.Range(15f, 90f);
            GetComponent<Rigidbody2D>().AddForce(new(x, y));
            StartCoroutine(EnableAnimation());
        }

        private IEnumerator EnableAnimation()
        {
            yield return new WaitForSeconds(5f);
            GetComponent<BubbleAnimator>().AnimationEnabled = true;
            IsSpawning = false;
        }

        private void BurstImpl()
        {
            _isBursting = true;
            PlaySE();
            if (HasCrown)
            {
                if (CrownShieldValue == 1)
                {
                    // 最後の演出
                    if (Bubble.CrownShieldValue == 1)
                    {
                        Bubble.CrownShieldValue--;

                        GameSequenceManager.WinnerPlayerIdx = Bubble.LastCrownRidePlayerIdx;
                        GameSequenceManager.Instance.GameOver();

                        transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
                        {
                            CrownShieldValue--;
                            Destroy(gameObject);
                        });
                        return;
                    }
                }
                else
                {
                    transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
                    {
                        CrownShieldValue--;
                        Destroy(gameObject);
                    });
                }
            }
            else
            {
                transform.DOScale(transform.localScale * 1.15f, 0.1f).OnComplete(() =>
                {
                    Instantiate(_bubPopEff, transform.position, Quaternion.identity);
                    Destroy(gameObject);
                });
            }
        }

        public bool IsOnScreen()
        {
            var pos = transform.position;
            return pos.x > -TopRight.x
                && pos.x < TopRight.x
                && pos.y > -TopRight.y
                && pos.y < TopRight.y;
        }
    }
}