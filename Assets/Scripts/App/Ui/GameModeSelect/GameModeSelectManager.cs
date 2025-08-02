using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;
using KanKikuchi.AudioManager;

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
            _selectBgDefaultAlpha = _selectBack.alpha;

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
                _selectBack.GetComponent<RectTransform>().position = _items[_selectedIndex].CenterPos;
            }

            _backUi.fillAmount = 0.0f;
            _backUi.material = new(_backUiMaterial);
            _backUi.material.SetFloat("_Mask", 0.28f);
        }

        void Update()
        {
            if (_isEnd)
            {
                return;
            }

            UpdateBack();

            if (_items[_selectedIndex].OnUpdate())
            {
                return;
            }

            if (DecideOrCancel())
            {
                return;
            }
        }
        #endregion

        #region 定義
        enum InputResult
        {
            Decide,
            Cancel,
            None,
        }
        #endregion

        #region private フィールド
        [SerializeField]
        List<Item> _items;

        [SerializeField]
        CanvasGroup _selectBack;

        [SerializeField]
        TadaLib.Ui.Button _startButton;

        [SerializeField]
        Material _backUiMaterial;

        [SerializeField]
        UnityEngine.UI.Image _backUi;

        [SerializeField]
        float _backTimeSecToDecide = 2.0f;

        int _selectedIndex = 0;
        bool _isEnd = false;

        float _selectBgDefaultAlpha = 1.0f;

        float _backProgress = 0.0f;
        #endregion

        #region private メソッド
        bool DecideOrCancel()
        {
            var input = GetInput();

            var moveIdx = input switch
            {
                InputResult.Decide => 1,
                InputResult.Cancel => -1,
                _ => 0
            };

            if (moveIdx == 0)
            {
                return false;
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
                return false;
            }

            if (nextIndex == _items.Count)
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.4f, 0.4f);
                _items[_selectedIndex].OnDecide();
                _isEnd = true;
                return true;
            }

            SEManager.Instance.Play(SEPath.MENU_NAVIGATION);

            if (moveIdx > 0)
            {
                _items[_selectedIndex].OnDecide();
            }
            _items[_selectedIndex].OnUnselected();

            _selectedIndex = nextIndex;
            _items[_selectedIndex].OnSelected();

            if (_selectedIndex == _items.Count - 1)
            {
                _startButton.OnSelected(doReaction: true);
                _selectBack.GetComponent<RectTransform>().DOMove(_items[_selectedIndex].CenterPos, 0.2f);
                _selectBack.DOFade(0.0f, 0.1f);
            }
            else
            {
                _startButton.OnUnselected();
                _selectBack.GetComponent<RectTransform>().DOKill();
                _selectBack.DOFade(_selectBgDefaultAlpha, 0.1f);
                _selectBack.GetComponent<RectTransform>().DOMove(_items[_selectedIndex].CenterPos, 0.2f);
            }

            return true;
        }

        InputResult GetInput()
        {
            // 決定とキャンセルが同時に押されたら、決定を優先する
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;

            // 方向入力
            //for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            for (int idx = 0; idx < 1; ++idx)
            {
                if (inputManager.InputProxy(idx).AxisTrigger(TadaLib.Input.AxisCode.Vertical, out var isPositive))
                {
                    return isPositive ? InputResult.Cancel : InputResult.Decide;
                }
            }

            // ボタン入力
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                if (inputManager.InputProxy(idx).IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                {
                    return InputResult.Decide;
                }

                if (inputManager.InputProxy(idx).IsPressedTrigger(TadaLib.Input.ButtonCode.Cancel))
                {
                    return InputResult.Cancel;
                }
            }

            return InputResult.None;
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

        void UpdateBack()
        {

            bool IsBacklPressed()
            {
                var inputManager = TadaLib.Input.PlayerInputManager.Instance;
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Cancel))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (IsBacklPressed())
            {
                _backProgress += Time.deltaTime;
            }
            else
            {
                _backProgress = 0.0f;
            }

            var rate = _backProgress / _backTimeSecToDecide;
            _backUi.fillAmount = Mathf.Min(rate, 1.0f);

            if (rate >= 1.0f)
            {
                // scene transition
                TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.3f, 0.3f, isReverse: true);
                _isEnd = true;

                _backUi.rectTransform.parent.DOPunchScale(Vector3.one * 1.06f, 0.2f);
            }
        }
        #endregion
    }
}