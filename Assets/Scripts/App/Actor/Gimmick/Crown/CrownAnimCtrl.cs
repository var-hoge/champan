using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Unity.VisualScripting;
using DG.Tweening;
using static UnityEngine.Rendering.DebugUI.Table;

namespace App.Actor.Gimmick.Crown
{
    /// <summary>
    /// CrownAnimCtrl
    /// </summary>
    public class CrownAnimCtrl
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void SetRemainHitCount(int count, float scale)
        {
            var animRate = count switch
            {
                var cnt when cnt >= 10 => 0.0f,
                var cnt when cnt >= 9 => 0.0f,
                var cnt when cnt >= 8 => 0.0f,
                var cnt when cnt >= 7 => 0.3f,
                var cnt when cnt >= 6 => 0.7f,
                var cnt when cnt >= 5 => 0.8f,
                var cnt when cnt >= 4 => 1.5f,
                var cnt when cnt >= 3 => 2.0f,
                var cnt when cnt >= 2 => 2.3f,
                var cnt when cnt >= 1 => 2.5f,
                _ => 0.0f,
            };

            //GetComponent<SimpleAnimation>().Play("Idle", animRate);

            StartCoroutine(Bounce(animRate, scale));
        }
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        IEnumerator Bounce(float animRate, float scale)
        {
            if (animRate > 0.0f)
            {
                var durationSecBase = 0.3f / animRate;

                var degPrev = Random.Range(0.0f, 360.0f);
                Vector2 targetPosPrev = Vector2.zero;
                while (true)
                {
                    if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.AfterBattle)
                    {
                        break;
                    }

                    if (gameObject == null)
                    {
                        break;
                    }

                    //var isCw = Random.Range(0, 2) % 2 == 0;
                    //var deg = degPrev + (isCw ? Random.Range(30.0f, 90.0f) : Random.Range(-30.0f, -90.0f));
                    var deg = degPrev + Random.Range(30.0f, 330.0f);

                    var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                    float radius = 0.02f * scale * Random.Range(0.5f, 1.0f) * Mathf.Sqrt(animRate);
                    var targetPos = dir * radius;

                    var easeCandidate = new List<Ease>() {
                        //Ease.Linear,
                        //Ease.OutBounce,
                        Ease.InOutQuad,
                        Ease.InOutQuart,
                        Ease.InOutExpo,
                        Ease.InOutQuad,
                        Ease.InOutQuart,
                        Ease.InOutExpo,
                        Ease.InOutBounce,
                    };

                    var ease = easeCandidate[Random.Range(0, easeCandidate.Count)];

                    transform.DOKill();

                    var durationSec = durationSecBase * (Vector2.Distance(targetPos, targetPosPrev) * 10.0f);
                    var tween = transform.DOLocalMove(targetPos, durationSec).SetEase(ease);

                    yield return tween.WaitForCompletion();
                    yield return new WaitForSeconds(0.05f / animRate);

                    targetPosPrev = targetPos;
                    degPrev = deg;

                    //yield return transform.DOShakePosition(
                    //    duration: durationSecBase, 
                    //    strength: 0.2f * scale, 
                    //    vibrato: 20, 
                    //    randomness: 90, 
                    //    snapping: false, 
                    //    fadeOut: true)
                    //    .WaitForCompletion();

                    //yield return crown.DOMove(originalPos, 0.1f).SetEase(Ease.OutSine).WaitForCompletion();

                    //yield return new WaitForSeconds(0.3f); // 小休止
                }
            }
        }
        #endregion
    }
}