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
    public class CharaSelectIcon
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        private void Start()
        {
            _charaIdx = CharaSelectUiManager.PlayerUseCharaIdList[_playerIdx];

            GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered += OnAction;
        }

        private void OnDestroy()
        {
            GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered -= OnAction;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        CharaSelectUiManager _manager;

        [SerializeField]
        int _playerIdx = 0;

        int _charaIdx = 0;
        #endregion

        #region privateメソッド
        void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            // 入力値が少ない場合はなし
            if (Mathf.Abs(value.x) < 0.5f)
            {
                return;
            }

            var nextCharaIdx = _charaIdx;

            if (value.x < -0.0f)
            {
                // 左に進む
                nextCharaIdx = (_charaIdx - 1 + _manager.CharaMaxCount) % _manager.CharaMaxCount;
            }
            else // (value.x > 0.0f)
            {
                // 右に進む
                nextCharaIdx = (_charaIdx + 1) % _manager.CharaMaxCount;
            }

            // TODO: 埋まってた場合はさらに移動する
            // TODO: 同じキャラの場合は動けない演出を加える
        }

        void OnAction(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if(!_manager.NotifySelect(_playerIdx, _charaIdx))
            {
                // 選べなかった
                return;
            }

            // 決定
        }
        #endregion
    }
}