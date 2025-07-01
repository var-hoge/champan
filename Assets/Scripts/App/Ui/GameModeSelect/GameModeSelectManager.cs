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

            // 決定とキャンセルが同時に押されたら、決定を優先する
            var isDecied = IsDecided();
            var isCanceled = IsCanceled();

            var moveIdx = 0;

            if (isDecied)
            {
                moveIdx = 1;
            }
            else if (isCanceled)
            {
                moveIdx = -1;
            }

            if (moveIdx == 0)
            {
                return;
            }

            int nextIndex;
            if (moveIdx == 1)
            {
                nextIndex = CalcAdvancedIndex(_selectedIndex);
            }
            else // moveIdx == -1
            {
                nextIndex = CalcRetreatedIndex(_selectedIndex);
            }

            if (nextIndex == -1)
            {
                // 動けなかった
                return;
            }

            if (nextIndex == _items.Count)
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.4f, 0.3f);
                _isEnd = true;
                return;
            }

            _items[_selectedIndex].OnDecide();
            _items[_selectedIndex].OnUnselected();

            _selectedIndex = nextIndex;
            _items[_selectedIndex].OnSelected();

            if (_selectedIndex == _items.Count - 1)
            {
                _startButtonOnDisabled.SetActive(false);
                _startButtonOnEnabled.SetActive(true);
                _selectBack.gameObject.SetActive(false);
            }
            else
            {
                _startButtonOnDisabled.SetActive(true);
                _startButtonOnEnabled.SetActive(false);
                _selectBack.gameObject.SetActive(true);
                _selectBack.rectTransform.DOKill();
                _selectBack.rectTransform.DOMove(_items[_selectedIndex].CenterPos, 0.2f);
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
        bool _isEnd = false;
        #endregion

        #region private メソッド
        bool IsDecided()
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                if (inputManager.InputProxy(idx).IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsCanceled()
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                if (inputManager.InputProxy(idx).IsPressedTrigger(TadaLib.Input.ButtonCode.Cancel))
                {
                    return true;
                }
            }

            return false;
        }

        int CalcAdvancedIndex(int curSelectedIndex)
        {
            ++curSelectedIndex;
            if (curSelectedIndex == _items.Count)
            {
                return curSelectedIndex;
            }
            if (_items[curSelectedIndex].IsInvalid is false)
            {
                return curSelectedIndex;
            }
            return CalcAdvancedIndex(curSelectedIndex);
        }

        int CalcRetreatedIndex(int curSelectedIndex)
        {
            --curSelectedIndex;
            if (curSelectedIndex == -1)
            {
                return curSelectedIndex;
            }
            if (_items[curSelectedIndex].IsInvalid is false)
            {
                return curSelectedIndex;
            }
            return CalcRetreatedIndex(curSelectedIndex);
        }
        #endregion
    }
}