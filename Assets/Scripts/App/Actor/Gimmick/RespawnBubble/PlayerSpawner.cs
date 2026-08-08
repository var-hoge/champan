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
                if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.Battle)
                {
                    var dataHolder = player.GetComponent<Player.DataHolder>();
                    dataHolder.IsDead = true;
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
                    // 生成は権威を持つ側だけが行う (オフラインでは常に権威を持つ)
                    var respawnBubble = Network.NetworkSession.HasAuthority
                        ? Network.NetworkSession.Spawn(_respawnBubble, new(spawnPointX, spawnRangeY.Max, 0), Quaternion.identity)
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