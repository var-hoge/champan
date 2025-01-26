using UnityEngine;
using TadaLib.ActionStd;
using KanKikuchi.AudioManager;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private RespawnBubble _respawnBubble = null;
    [SerializeField] Bubble _bubble = null;
    private MoveInfoCtrl _moveInfoCtrl = null;
    [SerializeField] float _BGMVolumeRate = 0.7f;

    private List<string> _SEPath = null;
    private List<string> SEPath
    {
        get
        {
            if (_SEPath == null)
            {
                _SEPath = new();
                for (var n = 1; n <= 9; ++n)
                {
                    var headNum = n < 10 ? "0" : null;
                    _SEPath.Add($"SE/Player Death/Player_Death_{headNum}{n}");
                }
            }
            return _SEPath;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _moveInfoCtrl = GetComponent<MoveInfoCtrl>();
        BGMManager.Instance.Play(BGMPath.FIGHT, _BGMVolumeRate);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var player in _moveInfoCtrl.RideObjects)
        {
            var path = SEPath[Random.Range(0, SEPath.Count)];
            SEManager.Instance.Play(path, 20f);

            // respawn player
            var camera = Camera.main;
            var topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.nearClipPlane));
            var x = Mathf.Clamp(player.transform.position.x, -topRight.x, topRight.x);
            var y = topRight.y + 1;
            var respawnBubble = Instantiate(_respawnBubble, new(x, y, 0), Quaternion.identity);
            respawnBubble.Init(player);
            player.transform.position = new Vector3(0, -30, 0);

            var xPosition = x - 2f;
            var xForce = -15f;
            for (var n = 0; n < 3; ++n)
            {
                var bubble = Instantiate(_bubble, new(xPosition, -y, 0), Quaternion.identity);
                bubble.Init(xForce);
                xPosition += 2f;
                xForce += 15f;
            }
        }
    }
}
