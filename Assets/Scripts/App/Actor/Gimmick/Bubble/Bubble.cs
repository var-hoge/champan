using UnityEngine;
using System.Collections;
using System;
using TadaLib.ActionStd;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using KanKikuchi.AudioManager;
using DG.Tweening;
using App.Actor.Gimmick.Crown;
using static UnityEngine.Rendering.STP;
using App.Ui.Main;

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

        public Action OnDestroyEvent;

        public bool IsSpawning { get; private set; }

        [Header("Configurations")]
        [SerializeField] private int crownStartShield;
        [SerializeField] private BubbleShieldConfig[] shieldConfigs;

        [SerializeField] private float minSize = 2.2f;
        [SerializeField] private float maxSize = 4.5f;

        [Header("References")]
        [SerializeField] private SpriteRenderer shieldSpriteRenderer;
        [SerializeField] private GameObject crownSpriteParent;
        [SerializeField] private SpriteRenderer crownFrontBubbleRenderer;

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

        [SerializeField] private CrownEffectCtrl _crownEffect;
        [SerializeField] private CrownAnimCtrl _crownAnim;

        [SerializeField]
        SimpleAnimation _finishCrown;

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

        private static Vector3 TopRight = Vector3.positiveInfinity;

        public Transform CrownSpriteRenderer => crownSpriteParent.transform;
        public bool HasCrown => Crown.Manager.Instance.CrownBubble == this;

        private List<string> _SEPath = null;
        private List<string> SEPath => _SEPath ??= Sound.SePathGenerator.GetSEPath("SE/Bubble Jump/Bubble_Jump_", 11).ToList();
        private bool IsRidden => _moveInfoCtrl.IsRidden;
        private bool _isRidenPrev = false;
        public bool IsTeleportableCrown => !IsSpawning && IsOnScreen() && !IsRidden;

        // Bust bubble
        public void DoBurst(int playerIdx = -1)
        {
            if (playerIdx >= 0)
            {
                Crown.Manager.Instance.LastCrownRidePlayerIdx = playerIdx;
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
                    Crown.Manager.Instance.LastCrownRidePlayerIdx = _moveInfoCtrl.RideObjects[0].GetComponent<Player.DataHolder>().PlayerIdx;
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
                        BubbleUtil.Blow(player, transform.position, blowPower, doVibrate: !HasCrown);
                        if (HasCrown)
                        {
                            player.GetComponent<EmotionCtrl>().NotifyHitCrown();
                        }
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
            if (HasCrown)
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
            Crown.Manager.Instance.CrownBubble = bubble;
            bubble.SetupCrown(Crown.Manager.Instance.ShieldValue, withGameStart: Crown.Manager.Instance.IsFirstCrownSetup);

            Crown.Manager.Instance.IsFirstCrownSetup = false;
        }

        private void RemoveCrown()
        {
            crownSpriteParent.gameObject.SetActive(false);
            _crownEffect.gameObject.SetActive(false);
            shieldSpriteRenderer.gameObject.SetActive(false);
        }

        private void SetupCrown(int shieldValue, bool withGameStart)
        {
            gameObject.name = "BubbleCrown";

            BubbleShieldConfig? config = null;
            foreach (var shieldConfig in shieldConfigs)
            {
                if (shieldConfig.shieldLevel > shieldValue) continue;

                config = shieldConfig;
                break;
            }

            if (config.HasValue)
            {
                shieldSpriteRenderer.gameObject.SetActive(true);
                shieldSpriteRenderer.sprite = config.Value.sprite;
            }

            if (withGameStart)
            {
                crownSpriteParent.transform.localScale = Vector3.one;
                crownSpriteParent.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 15.0f);
            }
            else
            {
                crownSpriteParent.transform.DOScale(Vector3.one, 0.15f);
                crownSpriteParent.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 15.0f + 180.0f);
                crownSpriteParent.transform.DOLocalRotate(new Vector3(0.0f, 0.0f, 375.0f), 0.2f, RotateMode.FastBeyond360);
            }

            {
                var alpha = shieldValue switch
                {
                    var cnt when cnt >= 10 => 0.23f,
                    var cnt when cnt >= 9 => 0.20f,
                    var cnt when cnt >= 8 => 0.17f,
                    var cnt when cnt >= 7 => 0.14f,
                    var cnt when cnt >= 6 => 0.11f,
                    var cnt when cnt >= 5 => 0.08f,
                    var cnt when cnt >= 4 => 0.05f,
                    var cnt when cnt >= 3 => 0.02f,
                    var cnt when cnt >= 2 => 0.0f,
                    var cnt when cnt >= 1 => 0.0f,
                    _ => 0,
                };
                crownFrontBubbleRenderer.color = new Color(1.0f, 1.0f, 1.0f, alpha);
            }

            _crownEffect.SetRemainHitCount(shieldValue);
            _crownAnim.SetRemainHitCount(shieldValue, transform.lossyScale.x);
            // エフェクトの再生
            crownGlitter.Play(false);
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
            if (_isBursting)
            {
                return;
            }

            _isBursting = true;
            PlaySE();
            if (HasCrown)
            {
                Crown.Manager.Instance.ShieldValue--;

                var shieldValue = Crown.Manager.Instance.ShieldValue;

                // 最後の演出
                if (shieldValue == 0)
                {
                    // 振動
                    {
                        var playerIdx = Crown.Manager.Instance.LastCrownRidePlayerIdx;
                        if (Cpu.CpuManager.Instance.IsCpu(playerIdx) is false)
                        {
                            TadaLib.Input.PlayerInputManager.Instance.InputProxy(playerIdx).Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.Happy);
                        }
                    }

                    GameSequenceManager.WinnerPlayerIdx = Crown.Manager.Instance.LastCrownRidePlayerIdx;
                    GameSequenceManager.Instance.GameOver();

                    var scale = transform.lossyScale.x;
                    transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
                    {
                        FinishCrown.Create(transform.position, scale);
                        Destroy(gameObject);
                    });
                    return;
                }
                else
                {
                    // 振動
                    {
                        var playerIdx = Crown.Manager.Instance.LastCrownRidePlayerIdx;
                        if (Cpu.CpuManager.Instance.IsCpu(playerIdx) is false)
                        {
                            var durationSec = shieldValue switch
                            {
                                var value when value == 9 => 0.12f,
                                var value when value == 8 => 0.14f,
                                var value when value == 7 => 0.16f,
                                var value when value == 6 => 0.18f,
                                var value when value == 5 => 0.20f,
                                var value when value == 4 => 0.22f,
                                var value when value == 3 => 0.24f,
                                var value when value == 2 => 0.26f,
                                var value when value == 1 => 0.28f,
                                _ => 0.12f,
                            };
                            Debug.Log(durationSec);
                            TadaLib.Input.PlayerInputManager.Instance.InputProxy(playerIdx).VibrateAdvanced(0.2f, 0.5f, durationSec);
                        }
                    }

                    var scale = transform.lossyScale.x;
                    //// テンポ重視のため早めに出す
                    //RunAwayCrown.Create(transform.position, scale);
                    //crownSpriteParent.gameObject.SetActive(false);
                    transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
                    {
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