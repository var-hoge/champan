using UnityEngine;

public class Bubble : MonoBehaviour
{
    private float _burstTimer = BurstTime;
    private bool _isPlayerStaying = false;

    private const float BurstTime = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_isPlayerStaying)
        {
            _burstTimer -= Time.deltaTime;
        }
        if (_burstTimer < 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            _isPlayerStaying = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            Destroy(gameObject);
        }
    }

    private static bool IsPlayer(Collider2D collision)
    {
        return collision.transform.tag == "Player";
    }
}
