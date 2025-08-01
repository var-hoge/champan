﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App.Actor;
using DG.Tweening;
using KanKikuchi.AudioManager;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// PlayerSelectSlot
    /// </summary>
    public class PlayerSelectSlot
        : BaseProc
        , IProcUpdate
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(_playerIdx);
            _cursor.AddMoveCallback(() => OnCharaChanged());
            _cursor.AddSelectCallback(() => OnCharaSelected());
            _cursor.AddCancelCallback(() => OnCharaCanceled());
            _breadCrunchPaths = new[]
            {
                SEPath.BREAD_CRUNCH_1,
                SEPath.BREAD_CRUNCH_2,
                SEPath.BREAD_CRUNCH_3,
            };
        }
        #endregion

        #region IProcUpdate の実装
        public void OnUpdate()
        {
            switch (_phase)
            {
                case Phase.WaitingForEntry:
                    WaitingForEntry();
                    break;
                case Phase.InCharacterSelection:
                    InCharacterSelection();
                    break;
                case Phase.CharacterSelected:
                    CharacterSelected();
                    break;
            }
        }
        #endregion

        #region 定義
        enum Phase
        {
            WaitingForEntry, // エントリー待ち
            InCharacterSelection, // キャラ選択中
            CharacterSelected, // キャラ選択後
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        int _playerIdx = 0;

        [SerializeField]
        CharaSelectCursor _cursor;

        [SerializeField]
        CanvasGroup _charaGroup;

        [SerializeField]
        List<UnityEngine.UI.Image> _charaImages;

        [SerializeField]
        GameObject _joinButton;

        [SerializeField]
        GameObject _player;

        Phase _phase = Phase.WaitingForEntry;
        TadaLib.Input.PlayerInputProxy _inputProxy = null;

        bool _isReselect = false;

        string[] _breadCrunchPaths = null;
        #endregion

        #region privateメソッド
        void WaitingForEntry()
        {
            // 最初から CPU じゃなければ自動エントリーする
            if (Cpu.CpuManager.Instance.IsCpu(_playerIdx) is false)
            {
                // 決定するまでは CPU 状態に戻す
                Cpu.CpuManager.Instance.SetIsCpu(_playerIdx, true);
                _phase = Phase.InCharacterSelection;

                _cursor.Show();
                ShowChara();
                return;
            }

            // ボタン入力待ち
            if (_inputProxy.IsPressed(TadaLib.Input.ButtonCode.Action))
            {
                SEManager.Instance.Play(SEPath.PLAYER_JOIN);
                _phase = Phase.InCharacterSelection;

                _cursor.Show();
                ShowChara();
            }
        }

        void InCharacterSelection()
        {

        }

        void CharacterSelected()
        {
        }

        void ShowChara()
        {
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
            var charaImage = CharacterManager.Instance.GetCharaImage(charaIdx);

            _charaGroup.gameObject.SetActive(true);

            _charaGroup.GetComponent<RectTransform>().localScale = Vector3.zero;
            _charaGroup.GetComponent<RectTransform>().DOScale(1.2f, 0.4f).SetEase(Ease.OutBack);
            _charaGroup.alpha = 0.0f; ;
            _charaGroup.DOFade(1.0f, 0.2f);

            foreach (var chara in _charaImages)
            {
                chara.SetSprite(charaImage);
            }
            _joinButton.gameObject.SetActive(false);
        }

        void OnCharaChanged()
        {
            SEManager.Instance.Play(SEPath.MOVING_CURSOR);
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
            var charaImage = CharacterManager.Instance.GetCharaImage(charaIdx);

            foreach (var chara in _charaImages)
            {
                chara.SetSprite(charaImage);
            }
        }

        void OnCharaSelected()
        {
            var path = _breadCrunchPaths[Random.Range(0, _breadCrunchPaths.Length)];
            SEManager.Instance.Play(path, 0.3f);

            // キャラ生成
            _player.gameObject.SetActive(true);

            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, GetComponent<RectTransform>().position);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10.0f));
            _player.transform.position = worldPos;

            _charaGroup.gameObject.SetActive(false);

            if (_isReselect)
            {
                _player.GetComponent<Actor.Player.CharaCtrl>().UpdateCharaSprite();
                // ジャンプスタート
                _player.GetComponent<TadaLib.ActionStd.StateMachine>().ChangeState(typeof(Actor.Player.State.StateJump));
            }
        }

        void OnCharaCanceled()
        {
            SEManager.Instance.Play(SEPath.CANCEL_SELECTION);

            // キャラ削除
            _player.gameObject.SetActive(false);
            _charaGroup.gameObject.SetActive(false);
            _joinButton.gameObject.SetActive(true);

            _phase = Phase.WaitingForEntry;
            _isReselect = true;
        }
        #endregion
    }
}