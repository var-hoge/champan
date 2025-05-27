using DG.Tweening;
using UnityEngine;

public class BubbleAnimator
    : TadaLib.ProcSystem.BaseProc
    , TadaLib.ProcSystem.IProcMove
{
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float minDistance;
    [SerializeField] private float maxDistance;

    [SerializeField] private float rideMinScaleRate = 0.5f;
    [SerializeField] private float rideEndScaleRate = 0.8f;
    [SerializeField] private float rideAnimDurationSec = 0.5f;
    [SerializeField] private float rideAnimAplitude = 1.0f;
    [SerializeField] private float rideAnimDampingFactor = 0.8f;

    private Vector2 startPosition = Vector2.positiveInfinity;
    private Vector2 startRotation;
    private float startTime = 0;

    private float startDelayX;
    private float speedX;
    private float distanceX;

    private float startDelayY;
    private float speedY;
    private float distanceY;

    public bool AnimationEnabled = false;

    private void Start()
    {
        startRotation = transform.rotation.eulerAngles;

        startDelayX = Random.Range(0f, 360f);
        startDelayY = Random.Range(0f, 360f);

        speedX = Random.Range(minSpeed, maxSpeed);
        speedY = Random.Range(minSpeed, maxSpeed);

        distanceX = Random.Range(minDistance, maxDistance);
        distanceY = Random.Range(minDistance, maxDistance);
    }

    public void OnMove()
    {
        if (!AnimationEnabled)
        {
            return;
        }

        if (startPosition.Equals(Vector2.positiveInfinity))
        {
            startPosition = transform.position;
            startTime = Time.time;
            startDelayX = 0f;
            startDelayY = 0f;
        }

        var time = Time.time - startTime;
        float x = Mathf.Sin(time * speedX + startDelayX) * distanceX;
        float y = Mathf.Sin(time * speedY + startDelayY) * distanceY;

        transform.position = startPosition + new Vector2(x, y);
    }

    public void OnRide()
    {
        PlayRideAnimation(rideAnimAplitude, transform.localScale.y, isFirst: true);
    }

    void PlayRideAnimation(float aplitude, float originScale, bool isFirst = false)
    {
        if (aplitude < 0.01f)
        {
            // èIóπ
            return;
        }

        // å∏êäêUìÆÇÇ≥ÇπÇÈ
        var width = rideEndScaleRate - rideMinScaleRate;

        Ease ease = Ease.Linear;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScaleY(originScale * (rideEndScaleRate - width * aplitude), rideAnimDurationSec * 0.5f * (isFirst ? 0.5f : 1.0f)).SetEase(ease));
        seq.Append(transform.DOScaleY(originScale * (rideEndScaleRate + width * aplitude), rideAnimDurationSec * 0.5f).SetEase(ease));

        seq.OnComplete(() =>
            {
                // êUïùÇå∏è≠Ç≥ÇπÇƒêUìÆ
                PlayRideAnimation(aplitude * rideAnimDampingFactor, originScale);
            });
    }
}
