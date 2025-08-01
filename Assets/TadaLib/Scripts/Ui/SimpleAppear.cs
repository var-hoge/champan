using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;
using UnityEngine.Events;

namespace TadaLib.Ui
{
    /// <summary>
    /// SimpleAppear
    /// </summary>
    public class SimpleAppear
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            var rectTrasnform = GetComponent<RectTransform>();
            if (rectTrasnform != null)
            {
                var initPos = rectTrasnform.position;
                var pos = initPos;
                pos.x -= _movePixel.x;
                pos.y -= _movePixel.y;
                rectTrasnform.position = pos;
                rectTrasnform.DOMove(initPos, _durationSec).OnComplete(() =>
                {
                    foreach (var ev in _onAppeared)
                    {
                        ev.Invoke();
                    }
                });
            }

            if (GetComponent<UnityEngine.UI.Image>() is { } image)
            {
                image.color = image.color.SetAlpha(_alphaFrom);
                image.DOFade(1.0f, _durationSec);
            }

            if (GetComponent<TMPro.TextMeshProUGUI>() is { } text)
            {
                text.color = text.color.SetAlpha(_alphaFrom);
                text.DOFade(1.0f, _durationSec);
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = _alphaFrom;
                canvasGroup.DOFade(1.0f, _durationSec);
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = spriteRenderer.color.SetAlpha(_alphaFrom);
                spriteRenderer.DOFade(1.0f, _durationSec);
            }

            //if (GetComponent<CanvasGroup>() is { } group)
            //{
            //    group.alpha = 0.0f;
            //    group.DOFade(1.0f, _durationSec);
            //}
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        float _alphaFrom = 0.0f;

        [SerializeField]
        float _durationSec = 0.4f;

        [SerializeField]
        Vector2 _movePixel = new Vector2(0.0f, 50.0f);

        [SerializeField]
        List<UnityEvent> _onAppeared;
        #endregion

        #region privateメソッド
        #endregion
    }
}