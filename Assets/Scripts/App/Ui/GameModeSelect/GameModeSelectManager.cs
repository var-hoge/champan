using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.GameModeSelect
{
    /// <summary>
    /// GameModeSelectManager
    /// </summary>
    public class GameModeSelectManager
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void SetIsExistCpu(bool isExist)
        {
            GameMatchManager.Instance.SetIsExistCpu(isExist);
        }

        public void SetWinCountToMatchFinish(int count)
        {
            GameMatchManager.Instance.SetWinCountToMatchFinish(count);
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            foreach (var item in _items)
            {
                item.OnUnselected();
            }

            while (_selectedIndex < _items.Count)
            {
                if (_items[_selectedIndex].IsInvalid)
                {
                    ++_selectedIndex;
                }
                else
                {
                    break;
                }
            }

            if (_selectedIndex < _items.Count)
            {
                _items[_selectedIndex].OnSelected();
                _selectBack.rectTransform.position = _items[_selectedIndex].CenterPos;
            }
        }

        void Update()
        {
            if (_isEnd)
            {
                return;
            }

            var isDecied = IsDecided();

            if (isDecied == _isDecidedPrev)
            {
                return;
            }

            _isDecidedPrev = isDecied;

            if (isDecied)
            {
                if (_selectedIndex == _items.Count)
                {
                    // シーン遷移
                    TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.4f, 0.3f);
                    _isEnd = true;
                    return;
                }

                _items[_selectedIndex].OnDecide();
                _items[_selectedIndex].OnUnselected();

                ++_selectedIndex;

                if (_selectedIndex < _items.Count)
                {
                    _items[_selectedIndex].OnSelected();

                    _selectBack.rectTransform.DOKill();
                    _selectBack.rectTransform.DOMove(_items[_selectedIndex].CenterPos, 0.2f);
                }
                else
                {
                    _startButtonOnDisabled.SetActive(false);
                    _startButtonOnEnabled.SetActive(true);
                }
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        List<Item> _items;

        [SerializeField]
        UnityEngine.UI.Image _selectBack;

        [SerializeField]
        GameObject _startButtonOnDisabled;

        [SerializeField]
        GameObject _startButtonOnEnabled;

        int _selectedIndex = 0;
        bool _isDecidedPrev = false;
        bool _isEnd = false;
        #endregion

        #region private メソッド
        bool IsDecided()
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}