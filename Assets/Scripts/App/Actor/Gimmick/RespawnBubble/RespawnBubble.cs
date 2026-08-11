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

        /// <summary>
        /// 降り始めの速度 (打ち消されたときに入れ直す)
        /// </summary>
        private Vector2 _descentVelocity = Vector2.zero;

        private float _descentGuardTimer = 0.0f;

        /// <summary>
        /// 降り始めを見張る時間
        /// </summary>
        private const float DescentGuardSec = 0.5f;

        /// <summary>
        /// これ以下なら止まっているとみなす
        /// </summary>
        private const float DescentStoppedSqr = 0.01f;

        /// <summary>
        /// 降りる勢いを弱める強さ
        ///
        /// 大きいほど早く止まる。
        /// 降りる距離はおよそ「初速 / (これ + Rigidbody の減衰)」になる。
        /// </summary>
        private const float DescentDamping = 0.4f;


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

        /// <summary>
        /// 消えるときは、頭上の UI をバブルに置いていかない
        ///
        /// バブルの消え方は台によって違う。
        /// 割れる処理を通らずに消えることもあるため、
        /// そこで戻すのでは取りこぼす。
        /// </summary>
        void OnDisable()
        {
            if (_player == null)
            {
                return;
            }

            var playerDataHolder = _player.GetComponent<Player.DataHolder>();
            if (playerDataHolder != null)
            {
                playerDataHolder.IsValidDummyPlayerPos = false;
            }
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

            // 降りる動きは、このバブルを生成した台が受け持つ。
            // 他の台は物理を止めてあり、配られてくる位置に従う。
            //
            // 割れる判断とは受け持ちが違う点に注意。
            // 割れる判断はキャラを動かしている台が行う (手応えを遅らせないため)。
            if (IsDescentDriver())
            {
                KeepDescending();
            }

            // 割れる判断と復帰位置は、そのキャラを動かしている台が決める。
            // ホストに問い合わせると往復の待ち時間がそのまま手応えの遅れになるため。
            if (Network.NetworkSession.IsOnline
                && !Network.SeatInput.IsMovableHere(playerDataHolder.PlayerIdx))
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
        /// 降り始める
        ///
        /// このバブルを動かすのは、生成した台だけ。
        /// 他の台は物理を止めて、配られてくる位置に従う。
        ///
        /// 力ではなく速度を直接与える。
        /// 力は次の物理更新まで持ち越されるため、
        /// その間に body の種類が入れ直されると失われ、
        /// 重力が無いこのバブルは二度と降りてこない。
        /// </summary>
        void BeginDescent(float basePosX)
        {
            var x = Random.Range(0f, 10f) * Mathf.Sign(basePosX) * -1;

            // 一度の力で得られる速さに合わせる (質量 0.1 に対し力 15 を 1 回)
            _descentVelocity = new Vector2(x, -15.0f) / _rb.mass * Time.fixedDeltaTime;
            _rb.linearVelocity = _descentVelocity;

            _descentGuardTimer = DescentGuardSec;

        }

        /// <summary>
        /// このバブルの動きを受け持つ台か
        ///
        /// 生成した台が動かし、その位置を他の台へ配る。
        /// </summary>
        bool IsDescentDriver()
        {
            if (!Network.NetworkSession.IsOnline)
            {
                return true;
            }

            var binder = GetComponent<Network.NetworkGimmickBinder>();

            return binder != null
                && binder.Object != null
                && binder.Object.IsValid
                && binder.Object.HasStateAuthority;
        }

        /// <summary>
        /// 降り始めが打ち消されていたら、入れ直す
        ///
        /// 速度が消える経路をすべて塞ぎ切るのは難しい。
        /// 降りてこないと復帰できず、試合が進まなくなるため、安全網を置く。
        /// </summary>
        void KeepDescending()
        {
            if (_descentGuardTimer > 0.0f)
            {
                _descentGuardTimer -= Time.deltaTime;

                if (_rb.linearVelocity.sqrMagnitude <= DescentStoppedSqr)
                {
                    _rb.linearVelocity = _descentVelocity;
                }
            }

            // 縦だけ強めに減速させる。
            //
            // Rigidbody の減衰を上げると横の操作まで重くなる。
            // 落ちてくる勢いだけを弱めたいので、縦だけここで落とす。
            var velocity = _rb.linearVelocity;
            velocity.y *= Mathf.Exp(-DescentDamping * Time.deltaTime);
            _rb.linearVelocity = velocity;

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
            BeginDescent(basePosX);

            ApplyOutline(seatIdx);
        }

        public void Init(GameObject player)
        {
            _player = player.transform;
            BeginDescent(_player.position.x);
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