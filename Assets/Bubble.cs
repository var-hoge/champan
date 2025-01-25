using UnityEngine;
using TadaLib.ActionStd;

public class Bubble : MonoBehaviour
{
    private float _burstTimer = BurstTime;
    private bool _hasRidden = false;
    private MoveInfoCtrl _moveInfoCtrl = null;

    private const float BurstTime = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _moveInfoCtrl = GetComponent<MoveInfoCtrl>();
    }

    // Update is called once per frame
    void Update()
    {
        var isRiding = _moveInfoCtrl.IsRiding();
        if (isRiding)
        {
            _hasRidden = true;
            _burstTimer -= Time.deltaTime;
        }

        if ((!isRiding && _hasRidden)
            || _burstTimer < 0)
        {
            Destroy(gameObject);
        }
    }
}
