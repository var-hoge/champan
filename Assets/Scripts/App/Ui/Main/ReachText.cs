using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TMPro;
using App.Graphics.Outline;
using static App.Graphics.Outline.OutlineManager;
using DG.Tweening;

namespace App.Ui.Main
{
    /// <summary>
    /// ReachText
    /// </summary>
    public class ReachText
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            var idx = _playerIndex;
            if (Cpu.CpuManager.Instance.IsCpu(idx))
            {
                idx = _colors.Count - 1;
            }

            _text.color = _colors[idx].SetAlpha(0.0f);

            _text.DOFade(1.0f, 0.2f);
            _text.rectTransform.localScale = Vector3.zero * 0.2f;
            _text.rectTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        TextMeshProUGUI _text;

        [SerializeField]
        int _playerIndex = 0;

        [SerializeField]

        List<Color> _colors;
        #endregion

        #region privateメソッド
        #endregion
    }
}