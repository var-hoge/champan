using UnityEngine;

public class BobbleGenerator : MonoBehaviour
{
    [SerializeField] private int _numOfBubble = 6;
    [SerializeField] private GameObject _bubblePrafab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (var n = 0; n < _numOfBubble; ++n)
        {
            var position = new Vector3(Random.Range(-10f, 10f), Random.Range(-4f, 4f), 1f);
            var bubble = Instantiate(_bubblePrafab, position, Quaternion.identity);
            bubble.GetComponent<BubbleAnimator>().AnimationEnabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
