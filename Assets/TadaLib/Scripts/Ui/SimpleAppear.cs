using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

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
                rectTrasnform.DOMove(initPos, _durationSec);
            }

            if (GetComponent<UnityEngine.UI.Image>() is { } image)
            {
                image.color = image.color.SetAlpha(0.0f);
                image.DOFade(1.0f, _durationSec);
            }

            if (GetComponent<TMPro.TextMeshProUGUI>() is { } text)
            {
                text.color = text.color.SetAlpha(0.0f);
                text.DOFade(1.0f, _durationSec);
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.0f;
                canvasGroup.DOFade(1.0f, _durationSec);
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = spriteRenderer.color.SetAlpha(0.0f);
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
        float _durationSec = 0.4f;

        [SerializeField]
        Vector2 _movePixel = new Vector2(0.0f, 50.0f);
        #endregion

        #region privateメソッド
        #endregion
    }
}