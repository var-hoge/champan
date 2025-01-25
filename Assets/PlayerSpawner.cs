using UnityEngine;
using TadaLib.ActionStd;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private RespawnBubble _respawnBubble = null;
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
            var camera = Camera.main;
            var topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.nearClipPlane));
            var x = Mathf.Clamp(player.transform.position.x, -topRight.x, topRight.x);
            var respawnBubble = Instantiate(_respawnBubble, new(x, topRight.y + 1, 0), Quaternion.identity);
            respawnBubble.Init(player);
            player.transform.position = new Vector3(0, -30, 0);
        }
    }
}
