using UnityEngine;

public class BubbleAnimator : MonoBehaviour
{
	[SerializeField] private float minSpeed;
	[SerializeField] private float maxSpeed;
	[SerializeField] private float minDistance;
	[SerializeField] private float maxDistance;

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

	private void Update()
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
}
