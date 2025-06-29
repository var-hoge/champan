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
            var initPos = rectTrasnform.position;
            var pos = initPos;
            pos.x -= _movePixel.x;
            pos.y -= _movePixel.y;
            rectTrasnform.position = pos;
            rectTrasnform.DOMove(initPos, _durationSec);

            if (GetComponent<UnityEngine.UI.Image>() is { } image)
            {
                image.DOFade(1.0f, _durationSec);
            }

            if (GetComponent<TMPro.TextMeshProUGUI>() is { } text)
            {
                text.DOFade(1.0f, _durationSec);
            }
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