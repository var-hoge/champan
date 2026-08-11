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

        /// <summary>
        /// 最後にこのバブルへ乗った人
        /// 王冠バブルでは、この人が勝者になる
        /// </summary>
        private int _lastRiderPlayerIdx = -1;

        /// <summary>
        /// 試合終了の演出を再生済みか
        /// 自分で判定した直後に知らせが届くと二重になる
        /// </summary>
        private bool _hasPlayedFinishStaging = false;

        /// <summary>
        /// 試合終了の演出を終えてから王冠バブルを破棄するまでの待ち
        /// 知らせが届くぶん、他の台は演出の開始が遅れる
        /// </summary>
        private const float FinishDespawnDelaySec = 0.5f;
        private BubbleShieldConfig currentShieldConfig;
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

            // 大きさは各台が乱数で決めるため、権威側の値を配って揃える
            if (Network.NetworkSession.IsOnline)
            {
                var binder = GetComponent<Network.NetworkGimmickBinder>();
                if (binder != null)
                {
                    binder.PublishBaseScale(transform.localScale);
                }
            }

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

            // 破裂の判定や搭乗の処理は権威側だけが行う。
            // リモート側でも走らせると、Despawn が効かないまま DOScale(0) で
            // 縮むだけの「見えないが存在するバブル」が残り、影だけが残る。
            // 権威を持たない側でも、踏まれたときの演出はその場で再生する。
            // ホストの結果を待ってから再生すると、踏んだ手応えが遅れて感じられる。
            // 破裂やシールドの増減といった結果はホストだけが決める。
            if (!Network.NetworkSession.HasAuthority)
            {
                UpdateRideVisualOnly();
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

                RememberRider();

                if (HasCrown)
                {
                    // 記録するのは、この台が動かしているキャラだけ。
                    //
                    // 他の台のキャラは、乗ったことが遅れて伝わってくる。
                    // それを勝者として書くと、実際に触れた人を上書きしてしまう。
                    // 他の台のキャラの分は、その台からの知らせで記録される。
                    var riderIdx = _lastRiderPlayerIdx;
                    if (riderIdx >= 0 && Network.SeatInput.IsMovableHere(riderIdx))
                    {
                        Crown.Manager.Instance.LastCrownRidePlayerIdx = riderIdx;
                    }
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

                BlowRiders();
            }
            else
            {
                _isRidenPrev = false;
            }


            // �v���C���[�̒��n���������肵���̂ŁA���L�̓R�����g�A�E�g

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

            // �N���E���o�u���̏ꍇ�A�V�[���h����]
            if (HasCrown)
            {
                _shieldCountRoot.Rotate(Vector3.forward, Time.deltaTime * 15f);
            }
        }

        /// <summary>
        /// 踏まれたときの演出だけを再生する (権威を持たない側)
        ///
        /// 乗車の情報は各台に届いているため、演出はその場で出せる。
        /// ホストの結果を待つと手応えが遅れるため、判定と演出を分けている。
        /// </summary>
        private void UpdateRideVisualOnly()
        {
            var losingRider = currentRiders > _moveInfoCtrl.RideObjects.Count;
            currentRiders = _moveInfoCtrl.RideObjects.Count;

            if (IsRidden)
            {
                if (!_isRidenPrev)
                {
                    _isRidenPrev = true;

                    GetComponent<BubbleAnimator>().OnRide();
                    GetComponent<BubbleAnimator>().AnimationEnabled = true;
                    PlaySE();
                }

                RememberRider();

                _burstTimer -= Time.deltaTime;
                _hasRidden = true;

                if (_burstTimer < 0)
                {
                    RequestBurst();
                }

                // 王冠入りのバブルは触れた時点で弾け飛ぶ。
                // 権威の有無に関わらず、見ている台それぞれで起こす。
                BlowRiders();
            }
            else
            {
                _isRidenPrev = false;
            }

            if ((!IsRidden && _hasRidden) || losingRider)
            {
                RequestBurst();
            }
        }

        /// <summary>
        /// 今このバブルに乗っている人を覚えておく
        ///
        /// 吹き飛ばした後や、乗っている人が居なくなった後でも、
        /// 誰が触れたのかを伝えられるようにする。
        /// </summary>
        private void RememberRider()
        {
            if (_moveInfoCtrl.RideObjects.Count == 0)
            {
                return;
            }

            _lastRiderPlayerIdx = _moveInfoCtrl.RideObjects[0]
                .GetComponent<Player.DataHolder>().PlayerIdx;
        }

        /// <summary>
        /// 乗っているプレイヤーを吹き飛ばす
        ///
        /// 王冠入りのバブルは一人でも触れた時点で吹き飛ばす。
        /// それ以外は、二人以上が乗り合ったときだけ押し出す。
        ///
        /// 吹き飛ばすのはキャラを動かす行為なので、動かしている台だけが行う。
        /// 他の台で動かしても、持ち主が配る座標で上書きされてしまう。
        /// 各台が自分の担当を吹き飛ばすことで、全員が同じ結果になる。
        /// </summary>
        private void BlowRiders()
        {
            var players = _moveInfoCtrl.RideObjects;
            var requiredCount = HasCrown ? 0 : 1;
            if (players.Count <= requiredCount)
            {
                return;
            }

            foreach (var player in players)
            {
                if (HasCrown)
                {
                    // 表情は配っていないため、見ている台それぞれで出す
                    player.GetComponent<EmotionCtrl>().NotifyHitCrown();
                }

                var playerIdx = player.GetComponent<Player.DataHolder>().PlayerIdx;
                if (Network.NetworkSession.IsOnline
                    && !Network.SeatInput.IsMovableHere(playerIdx))
                {
                    continue;
                }

                BubbleUtil.Blow(player, transform.position, blowPower, doVibrate: !HasCrown);

                if (HasCrown)
                {
                    VibrateCrownHit(playerIdx);
                }
            }
        }

        /// <summary>
        /// 試合終了の演出
        ///
        /// 勝敗はホストが決めるが、演出は各台で再生する。
        /// ホストでしか動かない破裂処理の中に置いていたため、
        /// ゲスト側では演出が出ないままリザルト画面に飛んでいた。
        /// </summary>
        public void PlayFinishStaging(int winnerSeatIdx)
        {
            if (_hasPlayedFinishStaging)
            {
                return;
            }

            _hasPlayedFinishStaging = true;

            // 手元のコントローラを鳴らせるのは、その席を操作している台だけ。
            // 席番号とこの台のコントローラ番号も一致しない。
            if (Cpu.CpuManager.Instance.IsCpu(winnerSeatIdx) is false)
            {
                Network.SeatInput.GetProxyOrNull(winnerSeatIdx)
                    ?.Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.Happy);
            }

            GameSequenceManager.WinnerPlayerIdx = winnerSeatIdx;
            GameSequenceManager.Instance.GameOver();

            TadaLib.Scene.TimeScaleManager.Instance.SetTemporaryTimeScale(0.01f, 0.015f, 0.0f);

            transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
            {
                var scale = transform.lossyScale.x;
                FinishCrown.Create(transform.position, scale);

                // 破棄できるのは権威を持つ側だけ。
                //
                // すぐに破棄すると、知らせが届くぶん遅れて演出を始めた台では
                // 縮み切る前にバブルごと消えて、王冠が飛び出さない。
                // 全員が演出を終えるまで待つ。
                DOVirtual.DelayedCall(FinishDespawnDelaySec, () =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    Network.NetworkSession.Despawn(gameObject);
                });
            });
        }

        /// <summary>
        /// 王冠バブルに触れたときの振動
        ///
        /// 手元のコントローラを鳴らせるのは、その席を操作している台だけ。
        /// シールドの増減はホストが行うため、そこで鳴らすとゲストは鳴らない。
        /// </summary>
        private void VibrateCrownHit(int playerIdx)
        {
            if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
            {
                return;
            }

            // 残りが少ないほど長く鳴らす
            var durationSec = Crown.Manager.Instance.ShieldValue switch
            {
                var value when value >= 9 => 0.12f,
                var value when value >= 1 => 0.12f + (10 - value) * 0.02f,
                _ => 0.12f,
            };

            // 席番号とこの台のコントローラ番号は一致しない。
            // 「2 台目の席 2」は、その台のローカル 1 人目が持っている。
            Network.SeatInput.GetProxyOrNull(playerIdx)
                ?.VibrateAdvanced(0.2f, 0.5f, durationSec);
        }

        /// <summary>
        /// 破裂を要求する (権威を持たない側)
        ///
        /// 見た目はその場で出し、実際の破棄とシールドの増減はホストに任せる。
        /// ホストの返答を待つと破裂が遅れて感じられるため。
        /// </summary>
        private void RequestBurst()
        {
            if (_isBursting)
            {
                return;
            }

            _isBursting = true;

            // 覚えておいた人を伝える。
            //
            // 王冠入りのバブルは触れた瞬間に相手を吹き飛ばすため、
            // ここに来たときには乗っている人が居なくなっている。
            // その場で調べると誰も見つからず、勝者が決まらない。
            var playerIdx = _lastRiderPlayerIdx;

            // 最後の一撃なら、ここでバブルを消してはいけない。
            // 試合終了の演出でゆっくり縮んでから王冠が飛び出すため、
            // 先に消すと、知らせが届く頃にはもう何も残っていない。
            var isFinalHit = HasCrown
                && Crown.Manager.Instance.ShieldValue + Crown.Manager.Instance.ExShieldValue <= 1;

            // 見た目だけ先に出す
            PlaySE();

            if (!isFinalHit)
            {
                // 権威を持つ側と同じ見せ方にする。
                // 縮めてから弾けさせると、弾ける瞬間が遅れて別物に見える。
                transform.DOScale(transform.localScale * 1.15f, 0.1f).OnComplete(() =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    Instantiate(_bubPopEff, transform.position, Quaternion.identity);

                    // 破棄はホストが行う。届くまで見た目だけ消しておく
                    gameObject.SetActive(false);
                });
            }

            // 王冠バブルなら、シールドが減った見た目も先に出す。
            // 正しい値はホストから配られてくるので、そこで上書きされる。
            if (HasCrown && Crown.Manager.Instance.ShieldValue > 0)
            {
                SetupCrown(Crown.Manager.Instance.ShieldValue - 1, withGameStart: false);
            }

            var binder = GetComponent<Network.NetworkGimmickBinder>();
            if (binder != null)
            {
                binder.RequestBurst(playerIdx);
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

        //private void UpdateShield()
        //{
        //    if (currentShieldValue <= 0)
        //    {
        //        shieldSpriteRenderer.gameObject.SetActive(false);
        //        return;
        //    }
        //}

        /// <summary>
        /// 王冠バブルのシールド表示を更新する
        ///
        /// シールドの値はホストが決めて配るため、
        /// 受け取った側も見た目を更新する必要がある。
        /// </summary>
        public void RefreshCrownVisual()
        {
            if (!HasCrown)
            {
                return;
            }

            SetupCrown(Crown.Manager.Instance.ShieldValue, withGameStart: false);
        }

        public static void SetupCrown(Bubble bubble)
        {
            Crown.Manager.Instance.CrownBubble = bubble;
            bubble.SetupCrown(Crown.Manager.Instance.ShieldValue, withGameStart: Crown.Manager.Instance.IsFirstCrownSetup);

            Crown.Manager.Instance.IsFirstCrownSetup = false;

            // どのバブルが王冠を持つかは全員で一致している必要がある
            if (Network.NetworkCrownState.Instance != null)
            {
                Network.NetworkCrownState.Instance.SetCrownBubble(bubble);
            }
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
                    var cnt when cnt >= 0 => 0.0f,
                    _ => 0,
                };
                crownFrontBubbleRenderer.color = new Color(1.0f, 1.0f, 1.0f, alpha);
            }

            _crownEffect.SetRemainHitCount(shieldValue);
            _crownAnim.SetRemainHitCount(shieldValue, transform.lossyScale.x);
            // �G�t�F�N�g�̍Đ�
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
            yield return new WaitForSeconds(3f);
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
                if (Crown.Manager.Instance.ShieldValue == 0)
                {
                    Crown.Manager.Instance.ExShieldValue--;
                }
                else
                {
                    Crown.Manager.Instance.ShieldValue--;
                }

                // �Ō�̉��o
                if (Manager.Instance.IsShieldDestroyed)
                {
                    var winnerSeatIdx = Crown.Manager.Instance.LastCrownRidePlayerIdx;

                    // 勝敗はホストが決める。
                    // 決まった結果を全台に伝えて、演出は各台で再生する。
                    Network.NetworkMatchState.Instance?.NotifyGameFinish(winnerSeatIdx);

                    PlayFinishStaging(winnerSeatIdx);
                    return;
                }
                else
                {
                    // @memo: 振動は BlowRiders が各台で鳴らす。
                    //        ここはホストでしか動かないため、ゲストで鳴らない。

                    // �t�F�C�N���o
                    if (Manager.Instance.DoFakeFinishStaging)
                    {
                        var scale = transform.lossyScale.x;
                        RunAwayCrown.Create(transform.position, scale);
                        crownSpriteParent.gameObject.SetActive(false);

                        TadaLib.Scene.TimeScaleManager.Instance.SetTemporaryTimeScale(0.01f, 0.008f, 0.0f);
                    }

                    transform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
                    {
                        if (Manager.Instance.DoFakeFinishStaging)
                        {
                            var scale = transform.lossyScale.x;
                            RunAwayCrown.Create(transform.position, scale);
                            crownSpriteParent.gameObject.SetActive(false);
                        }
                        Network.NetworkSession.Despawn(gameObject);
                    });
                }
            }
            else
            {
                transform.DOScale(transform.localScale * 1.15f, 0.1f).OnComplete(() =>
                {
                    Instantiate(_bubPopEff, transform.position, Quaternion.identity);
                    Network.NetworkSession.Despawn(gameObject);
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