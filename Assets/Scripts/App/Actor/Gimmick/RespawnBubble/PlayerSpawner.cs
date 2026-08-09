using UnityEngine;
using TadaLib.ActionStd;
using KanKikuchi.AudioManager;
using System.Collections.Generic;
using System.Linq;

namespace App.Actor.Gimmick.RespawnBubble
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        private RespawnBubble _respawnBubble = null;
        [SerializeField] Bubble.Bubble _bubble = null;
        private MoveInfoCtrl _moveInfoCtrl = null;
        [SerializeField] float _BGMVolumeRate = 1f;

        [SerializeField] private int bubbleCountMin = 1;
        [SerializeField] private int bubbleCountMax = 5;

        private (float Min, float Max) spawnRangeX;
        private (float Min, float Max) spawnRangeY;

        private List<string> _SEPath = null;
        private List<string> SEPath => _SEPath ??= Sound.SePathGenerator.GetSEPath("SE/Player Death/Player_Death_", 9).ToList();

        private static readonly Vector3 OutOfScreenPoint = new(0, -30, 0);

        /// <summary>
        /// 落下を続けて処理しない時間
        ///
        /// 画面外へ移すまでの数フレーム、落下地点に残り続けるため
        /// </summary>
        private const float FallCooldownSec = 1.0f;

        private readonly Dictionary<int, float> _latestFallTimes = new();


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _moveInfoCtrl = GetComponent<MoveInfoCtrl>();

            // �X�|�[���͈͂̃L���b�V��
            {
                var camera = Camera.main;
                var topRight = camera.ScreenToWorldPoint(new(Screen.width, Screen.height, camera.nearClipPlane));
                var maxX = topRight.x - 1f;
                var maxY = topRight.y + 0.5f;
                spawnRangeX = (-maxX, maxX);
                spawnRangeY = (-maxY, maxY);
            }
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var player in _moveInfoCtrl.RideObjects)
            {
                var holder = player.GetComponent<Player.DataHolder>();

                // 落下を検知できるのは、そのキャラを動かしている台だけ。
                // 他の台では物理を止めているため、ここには来ない。
                // 検知した台が知らせるので、二重に処理しないようにする。
                if (Network.NetworkSession.IsOnline
                    && !Network.SeatInput.IsLocalSeat(holder.PlayerIdx))
                {
                    continue;
                }

                // 落ちたキャラは、画面外へ移すまでの間ここに残り続ける。
                // 毎フレーム処理すると、オノマトペが出続けてしまう。
                //
                // IsDead は戻される場所がないため、ここでは使えない。
                // 使うと、一度落ちたキャラが二度と落ちられなくなる。
                if (IsInFallCooldown(holder.PlayerIdx))
                {
                    continue;
                }

                RegisterFall(holder.PlayerIdx);

                if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.Battle)
                {
                    var dataHolder = player.GetComponent<Player.DataHolder>();
                    dataHolder.IsDead = true;

                    // 落下は勝敗に関わらないため、検知した台がそのまま知らせる
                    if (Network.NetworkSession.IsOnline)
                    {
                        Network.NetworkMatchState.Instance?.NotifyFall(
                            dataHolder.PlayerIdx, player.transform.position, dataHolder.Velocity);
                    }
                    // SE�̍Đ�
                    var path = SEPath[Random.Range(0, SEPath.Count)];
                    SEManager.Instance.Play(path, 20f);
                    var playerIdx = player.GetComponent<Player.DataHolder>().PlayerIdx;
                    if (Cpu.CpuManager.Instance.IsCpu(playerIdx) is false)
                    {
                        // �R���g���[����U��
                        TadaLib.Input.PlayerInputManager.Instance.InputProxy(playerIdx).Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.Dead);
                    }
                    // �I�m�}�g�y�𐶐�
                    Ui.Main.OtomatopoeiaManager.Instance.Spawn(dataHolder.PlayerIdx, player.transform.position, dataHolder.Velocity);
                    // ���X�|�[���o�u���̐���
                    var spawnPointX = Mathf.Clamp(player.transform.position.x, spawnRangeX.Min, spawnRangeX.Max);
                    // 落下を検知できるのは、そのキャラを動かしている台だけ。
                    // 権威を持たない側は、ホストに生成を要求する。
                    if (!Network.NetworkSession.HasAuthority)
                    {
                        Network.NetworkMatchState.Instance?.RequestRespawnBubble(
                            dataHolder.PlayerIdx, spawnPointX, spawnRangeY.Max);

                        // プレイヤーを画面外へ移動
                        player.transform.position = OutOfScreenPoint;
                        continue;
                    }

                    var respawnBubble = Network.NetworkSession.HasAuthority
                        ? Network.NetworkSession.Spawn(
                            _respawnBubble,
                            new(spawnPointX, spawnRangeY.Max, 0),
                            Quaternion.identity,
                            // どの席のバブルかを Spawned より前に渡す
                            // (各台が自分の Player を結び付けるために使う)
                            spawned =>
                            {
                                var binder = spawned.GetComponent<Network.NetworkRespawnBubbleBinder>();
                                if (binder != null)
                                {
                                    binder.SetSeatIdx(dataHolder.PlayerIdx);
                                }
                            })
                        : null;
                    if (respawnBubble != null)
                    {
                        respawnBubble.Init(player);
                    }
                    // �v���C���[����ʊO�Ɉړ�
                    player.transform.position = OutOfScreenPoint;
                    // �ǉ��̃o�u���𐶐�
                    CreateBubble(spawnPointX);
                }
                else
                {
                    var dataHolder = player.GetComponent<Player.DataHolder>();
                    // �I�m�}�g�y�𐶐�
                    Ui.Main.OtomatopoeiaManager.Instance.Spawn(dataHolder.PlayerIdx, player.transform.position, dataHolder.Velocity);
                    player.GetComponent<Player.DataHolder>().IsDead = true;
                    // �v���C���[����ʊO�Ɉړ�
                    player.transform.position = OutOfScreenPoint;
                }
            }
        }

        /// <summary>
        /// 直前に落下を処理したばかりか
        /// </summary>
        bool IsInFallCooldown(int seatIdx)
        {
            if (!_latestFallTimes.TryGetValue(seatIdx, out var latestTime))
            {
                return false;
            }

            return Time.time - latestTime < FallCooldownSec;
        }

        void RegisterFall(int seatIdx)
        {
            _latestFallTimes[seatIdx] = Time.time;
        }

        /// <summary>
        /// 落下したという知らせを受けて、この台の見た目と音を出す
        ///
        /// キャラを画面外へ動かす処理は行わない。
        /// 座標は落ちた本人の台から配られるため、放っておいても揃う。
        /// </summary>
        public static void PlayFallVisual(int seatIdx, Vector3 position, Vector3 velocity)
        {
            var spawner = FindAnyObjectByType<PlayerSpawner>();
            if (spawner == null)
            {
                return;
            }

            var path = spawner.SEPath[Random.Range(0, spawner.SEPath.Count)];
            SEManager.Instance.Play(path, 20f);

            Ui.Main.OtomatopoeiaManager.Instance.Spawn(seatIdx, position, velocity);
        }

        /// <summary>
        /// 指定した席の復帰用バブルを生成する (ホストのみ)
        ///
        /// 落下を検知した台からの要求で呼ばれる。
        /// </summary>
        public void SpawnRespawnBubbleForSeat(int seatIdx, float spawnPointX, float spawnPointY)
        {
            if (!Network.NetworkSession.HasAuthority)
            {
                return;
            }

            var respawnBubble = Network.NetworkSession.Spawn(
                _respawnBubble,
                new(spawnPointX, spawnPointY, 0),
                Quaternion.identity,
                spawned =>
                {
                    var binder = spawned.GetComponent<Network.NetworkRespawnBubbleBinder>();
                    if (binder != null)
                    {
                        binder.SetSeatIdx(seatIdx);
                    }
                });

            if (respawnBubble == null)
            {
                return;
            }

            // 生成した時点では対象の Player がまだ結び付いていないため、
            // Player に依らない部分だけを設定する。
            // これを忘れると下向きの初速が付かず、バブルが上に留まったままになる。
            respawnBubble.InitForSeat(seatIdx, spawnPointX);

            // 対象の Player は各台が自分で結び付ける (NetworkRespawnBubbleBinder)
            CreateBubble(spawnPointX);
        }

        private void CreateBubble(float spawnPointX)
        {
            // 追加バブルの数と配置は全員で一致している必要があるため、権威を持つ側だけが生成する
            if (!Network.NetworkSession.HasAuthority)
            {
                return;
            }

            var xPosition = spawnPointX - 2f;
            var xForce = -15f;
            int count = Random.Range(bubbleCountMin, bubbleCountMax);
            for (var n = 0; n < count; ++n)
            {
                var bubble = Network.NetworkSession.Spawn(_bubble, new(xPosition, spawnRangeY.Min, 0), Quaternion.identity);
                bubble.Init(xForce);
                xPosition += 2f;
                xForce += 15f;
            }
        }
    }
}