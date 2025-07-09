using KanKikuchi.AudioManager;
using System.Collections.Generic;
using System.Linq;
using TadaLib.Input;
using UnityEngine;
using TadaLib.ActionStd;
using App.Graphics.Outline;

namespace App.Actor.Gimmick.RespawnBubble
{
    public class RespawnBubble : MonoBehaviour
    {
        private Transform _player = null;
        private Rigidbody2D _rb = null;
        private MoveInfoCtrl _moveInfoCtrl = null;
        private float _burstTimer = BurstTime;

        private const float BurstTime = 3f;

        private List<string> _SEPath = null;
        private List<string> SEPath => _SEPath ??= Sound.SePathGenerator.GetSEPath("SE/Player Respawn/Player_Respawn_", 9).ToList();

        [SerializeField] private GameObject _bubPopEff;
        [SerializeField] float blowPower = 12.0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _moveInfoCtrl = GetComponent<MoveInfoCtrl>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_player == null)
            {
                return;
            }

            var playerDataHolder = _player.GetComponent<Player.DataHolder>();

            if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.AfterBattle)
            {
                Respawn(playerDataHolder);
                return;
            }

            playerDataHolder.IsValidDummyPlayerPos = true;
            playerDataHolder.DummyPlayerPos = transform.position;

            _burstTimer -= Time.deltaTime;

            var force = Time.deltaTime * 10f;
            // マルチプレイヤー対応
            var axisX = InputUtil.GetAxis(_player.gameObject, AxisCode.Horizontal);
            _rb.AddForce(new(axisX * force, 0.0f));

            if (_burstTimer < 0
                || _moveInfoCtrl.IsRidden)
            {
                foreach (var obj in _moveInfoCtrl.RideObjects)
                {
                    Bubble.BubbleUtil.Blow(obj, transform.position, blowPower);
                }
                Respawn(playerDataHolder);
            }
        }

        private void Respawn(Player.DataHolder playerDataHolder)
        {
            playerDataHolder.IsDead = true;
            playerDataHolder.IsValidDummyPlayerPos = false;

            var path = SEPath[Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);
            _player.position = transform.position;
            _player.GetComponent<Player.MoveCtrl>().SetVelocityForce(Vector3.zero);
            Destroy(gameObject);
            Instantiate(_bubPopEff, transform.position, Quaternion.identity);
        }

        public void Init(GameObject player)
        {
            _player = player.transform;
            var x = Random.Range(0f, 10f) * Mathf.Sign(_player.position.x) * -1;
            _rb.AddForce(new(x, -15));
            _body.sprite = CharacterManager.Instance.GetCharaImage(_player.GetComponent<Player.DataHolder>().CharaIdx);

            {
                var playerIndex = player.GetComponent<App.Actor.Player.DataHolder>().PlayerIdx;
                var kind = playerIndex switch
                {
                    var idx when idx == 0 => OutlineManager.OutlineKind.Player0,
                    var idx when idx == 1 => OutlineManager.OutlineKind.Player1,
                    var idx when idx == 2 => OutlineManager.OutlineKind.Player2,
                    _ => OutlineManager.OutlineKind.Player3,
                };
                if (OutlineManager.Instance.TryGetOutlineMaterial(kind, true, out var material))
                {
                    _body.sharedMaterial = material;
                }
            }
        }

        [SerializeField]
        SpriteRenderer _body;
    }
}