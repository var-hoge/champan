using UnityEngine;

public class RespawnBubble : MonoBehaviour
{
    private Transform _player = null;
    private Rigidbody2D _rb = null;
    private float _burstTimer = BurstTime;

    private const float BurstTime = 5f;

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

        _burstTimer -= Time.deltaTime;

        var force = Time.deltaTime * 10f;
        // マルチプレイヤー対応
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _rb.AddForce(new(-force, 0));
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            _rb.AddForce(new(force, 0));
        }

        if (_burstTimer < 0)
        {
            _player.position = transform.position;
            Destroy(gameObject);
        }
    }

    public void Init(GameObject player)
    {
        _player = player.transform;
        var x = Random.Range(0f, 10f) * Mathf.Sign(_player.position.x) * -1;
        _rb.AddForce(new(x, -10));
    }
}
