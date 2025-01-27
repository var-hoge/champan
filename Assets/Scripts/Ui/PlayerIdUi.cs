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
            var dataHolder = player.GetComponent<Scripts.Actor.Player.DataHolder>();
            var isDummyValid = dataHolder.IsValidDummyPlayerPos;
            var playerPos = isDummyValid ? dataHolder.DummyPlayerPos : player.transform.position;

            var screenPos = Camera.main.WorldToScreenPoint(playerPos);

            var offsetY = 170.0f;
            var angle = 0.0f;

            // 画面内に収めるよう努力する
            if (isDummyValid)
            {
                offsetY = 100.0f;
                if (screenPos.y + offsetY > Screen.height)
                {
                    var diff = screenPos.y + offsetY - Screen.height;
                    angle = TadaLib.Util.InterpUtil.Linier(0.0f, 60.0f, Mathf.Min(1.0f, diff / offsetY));

                    if (screenPos.x > Screen.width / 2)
                    {
                        angle = -angle;
                    }
                }
            }

            GetComponent<RectTransform>().localEulerAngles = new Vector3(0.0f, 0.0f, -angle);
            GetComponent<RectTransform>().position = screenPos + offsetY * new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0.0f);
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