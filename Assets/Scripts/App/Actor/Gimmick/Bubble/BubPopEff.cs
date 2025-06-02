using DG.Tweening;
using UnityEngine;

namespace App.Actor.Gimmick.Bubble
{
    public class BubPopEff : MonoBehaviour
    {
        void Start()
        {
            transform.DOScale(Vector3.one * 1.1f, 0.15f).OnComplete(() => { Destroy(gameObject); });
        }
    }
}