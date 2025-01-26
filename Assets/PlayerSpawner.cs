using UnityEngine;
using TadaLib.ActionStd;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private RespawnBubble _respawnBubble = null;
    [SerializeField] Bubble _bubble = null;
    private MoveInfoCtrl _moveInfoCtrl = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _moveInfoCtrl = GetComponent<MoveInfoCtrl>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var player in _moveInfoCtrl.RideObjects)
        {
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
