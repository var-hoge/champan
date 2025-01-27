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
        public void Setup(CharaSelectUiManager manager, int selectIdx)
        {
            _manager = manager;
            _selectIdx = selectIdx;
            GetComponent<RectTransform>().position = _manager.GetPickIconTransform(_playerIdx, _selectIdx).position;
            GetComponent<RectTransform>().rotation = _manager.GetPickIconTransform(_playerIdx, _selectIdx).rotation;
        }
        #endregion

        #region privateフィールド
        CharaSelectUiManager _manager;

        [SerializeField]
        int _playerIdx = 0;

        int _selectIdx = 0;

        float _moveInputValuePrev = 0.0f;
        #endregion

        #region privateメソッド
        void OnMove(Vector2 value)
        {
            // 入力開始時だけ受け付ける
            if ((value.x > 0.5f && _moveInputValuePrev > 0.5) ||
                    (value.x < -0.5f && _moveInputValuePrev < -0.5f))
            {
                // 同じ
                return;
            }

            _moveInputValuePrev = value.x;

            // 入力値が少ない場合はなし
            if (Mathf.Abs(value.x) < 0.5f)
            {
                return;
            }

            var nextSelectIdx = _selectIdx;

            if (value.x < -0.0f)
            {
                // 左に進む
                nextSelectIdx = (_selectIdx - 1 + _manager.CharaMaxCount) % _manager.CharaMaxCount;
            }
            else // (value.x > 0.0f)
            {
                // 右に進む
                nextSelectIdx = (_selectIdx + 1) % _manager.CharaMaxCount;
            }

            // TODO: 埋まってた場合はさらに移動する
            // TODO: 同じキャラの場合は動けない演出を加える
            if (_selectIdx != nextSelectIdx)
            {
                _selectIdx = nextSelectIdx;
                GetComponent<RectTransform>().DOKill();
                GetComponent<RectTransform>().DOMove(_manager.GetPickIconTransform(_playerIdx, _selectIdx).position, 0.2f);
                GetComponent<RectTransform>().DORotate(_manager.GetPickIconTransform(_playerIdx, _selectIdx).eulerAngles, 0.2f);
            }
        }

        void OnAction()
        {
            if (!_manager.NotifySelect(_playerIdx, _selectIdx))
            {
                // 選べなかった
                return;
            }

            // 決定
        }

        private void Start()
        {
            _selectIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);

            GameController.Instance.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnAction += OnAction;
            GameController.Instance.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnMove += OnMove;
            //GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered += OnAction;
        }

        private void OnDestroy()
        {
            var gameController = GameController.Instance;
            if (gameController == null)
            {
                return;
            }
            gameController.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnAction -= OnAction;
            gameController.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnMove -= OnMove;
            //GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered -= OnAction;
        }
        #endregion
    }
}