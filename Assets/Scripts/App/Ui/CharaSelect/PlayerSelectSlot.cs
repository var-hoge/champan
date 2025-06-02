using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App.Actor;
using DG.Tweening;
using static UnityEngine.GraphicsBuffer;

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
            _inputSystemInput = GameController.Instance.GetPlayerInput(_playerIdx);
            _cursor.AddMoveCallback(() => OnCharaChanged());
            _cursor.AddSelectCallbac(() => OnCharaSelected());
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
        UnityEngine.UI.Image _chara;

        [SerializeField]
        GameObject _joinButton;

        [SerializeField]
        GameObject _player;

        Phase _phase = Phase.WaitingForEntry;
        UnityEngine.InputSystem.PlayerInput _inputSystemInput = null;

        #endregion

        #region privateメソッド
        void WaitingForEntry()
        {
            // ボタン入力待ち
            if (_inputSystemInput.actions["Action"].IsPressed())
            {
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

            _chara.rectTransform.sizeDelta = charaImage.textureRect.size;
            _chara.sprite = charaImage;

            _chara.rectTransform.localScale = Vector3.zero;
            _chara.rectTransform.DOScale(1.2f, 0.4f).SetEase(Ease.OutBack);
            _chara.color = _chara.color.SetAlpha(0.0f);
            _chara.DOFade(1.0f, 0.2f);

            _joinButton.gameObject.SetActive(false);
        }

        void OnCharaChanged()
        {
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
            var charaImage = CharacterManager.Instance.GetCharaImage(charaIdx);

            _chara.rectTransform.sizeDelta = charaImage.textureRect.size;
            _chara.sprite = charaImage;
        }

        void OnCharaSelected()
        {
            // キャラ生成
            _player.gameObject.SetActive(true);

            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, GetComponent<RectTransform>().position);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10.0f));
            _player.transform.position = worldPos;

            _chara.gameObject.SetActive(false);
        }
        #endregion
    }
}