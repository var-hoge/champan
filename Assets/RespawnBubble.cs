using KanKikuchi.AudioManager;
using Scripts.Actor;
using Scripts.Actor.Player;
using System.Collections.Generic;
using System.Linq;
using TadaLib.Input;
using UnityEngine;

public class RespawnBubble : MonoBehaviour
{
    private Transform _player = null;
    private Rigidbody2D _rb = null;
    private float _burstTimer = BurstTime;

    private const float BurstTime = 5f;

    private List<string> _SEPath = null;
    private List<string> SEPath => _SEPath ??= GameController.GetSEPath("SE/Player Respawn/Player_Respawn_", 9).ToList();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_player == null)
        {
            return;
        }

        var playerDataHolder = _player.GetComponent<DataHolder>();

        playerDataHolder.IsValidDummyPlayerPos = true;
        playerDataHolder.DummyPlayerPos = transform.position;

        _burstTimer -= Time.deltaTime;

        var force = Time.deltaTime * 10f;
        // マルチプレイヤー対応
        var axisX = InputUtil.GetAxis(_player.gameObject, AxisCode.Horizontal);
        _rb.AddForce(new(axisX * force, 0.0f));

        if (_burstTimer < 0)
        {
            playerDataHolder.IsDead = true;
            playerDataHolder.IsValidDummyPlayerPos = false;

            var path = SEPath[Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);
            _player.position = transform.position;
            _player.GetComponent<MoveCtrl>().SetVelocityForce(Vector3.zero);
            Destroy(gameObject);
        }
    }

    public void Init(GameObject player)
    {
        _player = player.transform;
        var x = Random.Range(0f, 10f) * Mathf.Sign(_player.position.x) * -1;
        _rb.AddForce(new(x, -15));
        _body.sprite = CharacterManager.Instance.GetCharaImage(_player.GetComponent<DataHolder>().CharaIdx);
    }

    [SerializeField]
    SpriteRenderer _body;
}
