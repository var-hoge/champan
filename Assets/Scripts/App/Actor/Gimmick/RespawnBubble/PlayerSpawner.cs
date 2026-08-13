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

        /// <summary>
        /// 落ちたキャラを、復帰するまで待たせておく場所
        /// </summary>
        public static readonly Vector3 OutOfScreenPoint = new(0, -30, 0);

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
                    && !Network.SeatInput.IsMovableHere(holder.PlayerIdx))
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
                        // 席番号とこの台のコントローラ番号は一致しない。
                        // 他の台が担当する席なら null が返る。
                        Network.SeatInput.GetProxyOrNull(playerIdx)
                            ?.Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.Dead);
                    }
                    // �I�m�}�g�y�𐶐�
                    Ui.Main.OtomatopoeiaManager.Instance.Spawn(dataHolder.PlayerIdx, player.transform.position, dataHolder.Velocity);
                    // ���X�|�[���o�u���̐���
                    var spawnPointX = Mathf.Clamp(player.transform.position.x, spawnRangeX.Min, spawnRangeX.Max);
                    // 復帰バブルは、落ちた本人の台が生成して持つ。
                    //
                    // 生成した台がバブルを動かし、その位置を他の台へ配る。
                    // ホストに生成させると、落ちた本人はバブルを動かせず、
                    // 左右に寄せる操作が効かなくなる。
                    var respawnBubble = Network.NetworkSession.Spawn(
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
                        });

                    if (respawnBubble != null)
                    {
                        respawnBubble.Init(player);
                    }

                    // プレイヤーを画面外へ移動
                    player.transform.position = OutOfScreenPoint;

                    // 追加のバブルは数と配置が全員で一致している必要があるため、
                    // ホストに任せる
                    if (Network.NetworkSession.IsOnline
                        && !Network.NetworkSession.HasAuthority)
                    {
                        Network.NetworkMatchState.Instance?.RequestExtraBubbles(spawnPointX);
                        continue;
                    }

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
        /// 落下に添える追加のバブルを生成する (ホストのみ)
        ///
        /// 落下を検知した台からの要求で呼ばれる。
        ///
        /// 復帰バブル本体は落ちた本人の台が持つ。
        /// こちらは数と配置が乱数で決まるため、全員で一致させる必要があり、
        /// ホストだけが生成する。
        /// </summary>
        public void SpawnExtraBubbles(float spawnPointX)
        {
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