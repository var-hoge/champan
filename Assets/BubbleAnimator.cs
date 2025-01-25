using UnityEngine;

public class BubbleAnimator : MonoBehaviour
{
	[SerializeField] private float minSpeed;
	[SerializeField] private float maxSpeed;
	[SerializeField] private float minDistance;
	[SerializeField] private float maxDistance;

	private Vector2 startPosition;
	private Vector2 startRotation;

	private float startDelayX;
	private float speedX;
	private float distanceX;

	private float startDelayY;
	private float speedY;
	private float distanceY;

	private void Start()
	{
		startPosition = transform.position;
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
		float x = Mathf.Sin(Time.time * speedX + startDelayX) * distanceX;
		float y = Mathf.Sin(Time.time * speedY + startDelayY) * distanceY;

		transform.position = startPosition + new Vector2(x, y);
	}
}
