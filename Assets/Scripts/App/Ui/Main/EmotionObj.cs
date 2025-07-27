using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.Main
{
    /// <summary>
    /// EmotionObj
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EmotionObj
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void Init(Sprite sprite)
        {
            GetComponent<SpriteRenderer>().sprite = sprite;

            var startPos = transform.localPosition + Vector3.up * 0.5f;
            var endPos = transform.localPosition;

            transform.localPosition = startPos;

            var secRate = 1.0f;
            if (GameSequenceManager.Instance != null && GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.AfterBattle)
            {
                secRate *= 0.7f;
            }

            var seq = DOTween.Sequence();
            seq.Append(transform.DOLocalMove(endPos, _stagingDurationSecIn * secRate).SetEase(Ease.OutBack));
            seq.AppendInterval(_stagingDurationSec * secRate);
            seq.Append(transform.DOLocalMove(startPos, _stagingDurationSecOut * secRate));
            seq.Join(GetComponent<SpriteRenderer>().DOFade(0.0f, _stagingDurationSecOut * secRate).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        float _stagingDurationSecIn = 0.2f;

        [SerializeField]
        float _stagingDurationSec = 0.5f;

        [SerializeField]
        float _stagingDurationSecOut = 0.3f;
        #endregion

        #region privateメソッド
        #endregion
    }
}