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

        /// <summary>
        /// 復帰処理を実行済みか
        /// 権威を持たない台では破棄されないため、二重に走らないようにする
        /// </summary>
        private bool _isRespawned = false;

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
            // 対象が結び付くまでは何もしない
            // (ネットワーク対戦では、席が配られるまで結び付けられない)
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

            // 割れる判断と復帰位置は、そのキャラを動かしている台が決める。
            // ホストに問い合わせると往復の待ち時間がそのまま手応えの遅れになるため。
            if (Network.NetworkSession.IsOnline
                && !Network.SeatInput.IsLocalSeat(playerDataHolder.PlayerIdx))
            {
                return;
            }

            _burstTimer -= Time.deltaTime;


            var force = Time.deltaTime * 10f;
            // �}���`�v���C���[�Ή�
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
            // 破棄は権威側でしか行われないため、
            // そのままだと権威を持たない台で毎フレーム復帰処理が走る
            if (_isRespawned)
            {
                return;
            }

            _isRespawned = true;

            playerDataHolder.IsDead = true;
            playerDataHolder.IsValidDummyPlayerPos = false;

            var path = SEPath[Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);

            // ここに来るのは、そのキャラを動かしている台だけ。
            // 待たずにその場で戻す
            _player.position = transform.position;
            _player.GetComponent<Player.MoveCtrl>().SetVelocityForce(Vector3.zero);

            Instantiate(_bubPopEff, transform.position, Quaternion.identity);

            // 決めた結果を他の台に伝える
            if (Network.NetworkSession.IsOnline)
            {
                Network.NetworkMatchState.Instance?.NotifyRespawn(
                    playerDataHolder.PlayerIdx,
                    transform.position);
            }

            // 破棄できるのは権威を持つ側だけ。
            // それ以外の台では、伝えた先のホストが破棄するまで見た目だけ消しておく。
            if (Network.NetworkSession.HasAuthority)
            {
                Network.NetworkSession.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 割れる見た目だけを出す
        ///
        /// 権威を持たない台は、ホストが破棄したことで初めて割れたと分かる。
        /// </summary>
        public void PlayBurstVisual()
        {
            if (_isRespawned)
            {
                return;
            }

            _isRespawned = true;

            if (_player != null)
            {
                var playerDataHolder = _player.GetComponent<Player.DataHolder>();
                playerDataHolder.IsDead = true;
                playerDataHolder.IsValidDummyPlayerPos = false;
            }

            var path = SEPath[Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);

            Instantiate(_bubPopEff, transform.position, Quaternion.identity);
        }

        /// <summary>
        /// 対象のプレイヤーを結び付ける
        ///
        /// 生成はホストだけが行うため、ホスト以外では Init が呼ばれない。
        /// 各台が自分の Player を結び付ける必要があるため、
        /// 見た目と対象の設定だけを切り出してある。
        /// </summary>
        public void Bind(GameObject player)
        {
            _player = player.transform;
            _body.sprite = CharacterManager.Instance.GetCharaImage(
                player.GetComponent<Player.DataHolder>().CharaIdx);
        }

        /// <summary>
        /// 動き出しと枠線を設定する
        ///
        /// ネットワーク対戦では、落ちた台からの要求でホストが生成するため、
        /// 生成の時点では対象の Player がまだ結び付いていない。
        /// Player に依らない部分だけを切り出してある。
        /// </summary>
        public void InitForSeat(int seatIdx, float basePosX)
        {
            var x = Random.Range(0f, 10f) * Mathf.Sign(basePosX) * -1;
            _rb.AddForce(new(x, -15));

            ApplyOutline(seatIdx);
        }

        public void Init(GameObject player)
        {
            _player = player.transform;
            var x = Random.Range(0f, 10f) * Mathf.Sign(_player.position.x) * -1;
            _rb.AddForce(new(x, -15));
            _body.sprite = CharacterManager.Instance.GetCharaImage(_player.GetComponent<Player.DataHolder>().CharaIdx);

            ApplyOutline(player.GetComponent<App.Actor.Player.DataHolder>().PlayerIdx);
        }

        private void ApplyOutline(int playerIndex)
        {
            {
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