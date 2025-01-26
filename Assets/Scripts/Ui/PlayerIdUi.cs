using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Ui
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class PlayerIdUi
        : TadaLib.ProcSystem.BaseProc
        , TadaLib.ProcSystem.IProcPostMove
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void OnPostMove()
        {
            var player = TadaLib.ActionStd.PlayerManager.TryGetPlayer(_playerNumber);

            if (player == null)
            {
                return;
            }

            // プレイヤーの頭上に移動させる
            var screenPos = Camera.main.WorldToScreenPoint(player.transform.position);
            //var uiPos = GetComponent<RectTransform>().InverseTransformPoint(screenPos);

            GetComponent<RectTransform>().position = screenPos + Vector3.up * 175.0f;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;
        [SerializeField]
        int _playerNumber = 0;
        #endregion

        #region privateメソッド
        #endregion
    }
}