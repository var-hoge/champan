﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace App.Ui.Common
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
        void Start()
        {
            GetComponent<UnityEngine.UI.Image>().enabled = false;

            _isCpu = GameSequenceManager.Instance != null && Cpu.CpuManager.Instance.IsCpu(_playerNumber);

            if (_isCpu)
            {
                GetComponent<RectTransform>().localScale = GetComponent<RectTransform>().localScale * 0.85f;
            }
        }

        public void OnPostMove()
        {
            var player = TadaLib.ActionStd.PlayerManager.TryGetPlayer(_playerNumber);

            if (player == null || !player.activeSelf)
            {
                GetComponent<UnityEngine.UI.Image>().enabled = false;
                return;
            }

            GetComponent<UnityEngine.UI.Image>().enabled = true;

            // プレイヤーの頭上に移動させる
            var dataHolder = player.GetComponent<Actor.Player.DataHolder>();
            var isDummyValid = dataHolder.IsValidDummyPlayerPos;
            var playerPos = isDummyValid ? dataHolder.DummyPlayerPos : player.transform.position;

            var screenPos = Camera.main.WorldToScreenPoint(playerPos);

            var offsetY = 155.0f;
            var angle = 0.0f;

            if (_isCpu)
            {
                offsetY *= 0.85f;
            }

            if (!_isDummyPrev && isDummyValid)
            {
                // 風船中に UI の方向が変わらないようにする
                _angleRate = screenPos.x > Screen.width / 2 ? -1.0f : 1.0f;
            }
            _isDummyPrev = isDummyValid;

            // 画面内に収めるよう努力する
            if (isDummyValid)
            {
                offsetY = 84.0f;
                if (_isCpu)
                {
                    offsetY *= 0.85f;
                }
                if (screenPos.y + offsetY > Screen.height)
                {
                    var diff = screenPos.y + offsetY - Screen.height;
                    var rate = Mathf.Clamp01(diff / offsetY);
                    angle = TadaLib.Util.InterpUtil.Linier(0.0f, 60.0f, rate);
                    angle *= _angleRate;

                    offsetY = TadaLib.Util.InterpUtil.Linier(40.0f, offsetY, 1.0f - rate);
                }
            }

            _offsetY = TadaLib.Util.InterpUtil.Linier(_offsetY, offsetY, 0.1f, Time.deltaTime);

            var useOffsetY = _offsetY * (Screen.height / 1080.0f);

            GetComponent<RectTransform>().localEulerAngles = new Vector3(0.0f, 0.0f, -angle);
            GetComponent<RectTransform>().position = screenPos + useOffsetY * new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0.0f);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;
        [SerializeField]
        int _playerNumber = 0;
        float _angleRate = 1.0f;
        bool _isDummyPrev = false;
        float _offsetY = 170.0f;
        bool _isCpu = false;
        #endregion

        #region privateメソッド
        #endregion
    }
}