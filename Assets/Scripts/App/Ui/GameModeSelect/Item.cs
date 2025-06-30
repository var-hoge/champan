using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.GameModeSelect
{
    /// <summary>
    /// Item
    /// </summary>
    public class Item
        : MonoBehaviour
    {
        #region プロパティ
        public bool IsInvalid
        {
            get
            {
                return false;
                //return Cpu.CpuManager.Instance.CpuCount() == 0;
            }
        }
        public Vector3 CenterPos => _center.position;
        #endregion

        #region メソッド
        public void OnSelected()
        {
            _isSelected = true;

            foreach (var text in _colorChangableTexts)
            {
                text.color = _selectedColor;
            }

            _selectCursorLeft.SetActive(true);
            _selectCursorRight.SetActive(true);
        }

        public void OnUnselected()
        {
            _isSelected = false;

            foreach (var text in _colorChangableTexts)
            {
                text.color = _unselectedColor;
            }

            _selectCursorLeft.SetActive(false);
            _selectCursorRight.SetActive(false);
        }

        public void OnDecide()
        {
            _onItemDecided[_curIndex].Invoke();
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _curIndex = _defaultIndex;
            OnIndexChanged();
        }

        void Update()
        {
            if (_isSelected is false)
            {
                return;
            }

            var inputValue = CalcInputValue();

            if (inputValue == _inputValuePrev)
            {
                return;
            }

            _inputValuePrev = inputValue;

            if (inputValue == 0)
            {
                return;
            }

            var itemCount = _items.Count;
            _curIndex = (_curIndex + inputValue + itemCount) % itemCount;

            OnIndexChanged();
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Color _selectedColor;

        [SerializeField]
        Color _unselectedColor;

        [SerializeField]
        RectTransform _center;

        [SerializeField]
        TMPro.TextMeshProUGUI _mainText;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _colorChangableTexts;

        [SerializeField]
        GameObject _selectCursorLeft;

        [SerializeField]
        GameObject _selectCursorRight;

        [SerializeField]
        List<string> _items = new List<string>();

        [SerializeField]
        List<UnityEngine.Events.UnityEvent> _onItemDecided;

        [SerializeField]
        int _defaultIndex = 0;

        bool _isSelected = false;
        int _curIndex = 0;
        int _inputValuePrev = 0;
        #endregion

        #region privateメソッド
        /// <summary>
        /// 入力値の取得
        /// -1 なら左、0 なら何も押していない、1 なら右
        /// </summary>
        /// <returns></returns>
        int CalcInputValue()
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
            {
                var value = inputManager.InputProxy(idx).Axis(TadaLib.Input.AxisCode.Horizontal);

                // 入力値が少ない場合はなし
                if (Mathf.Abs(value) < 0.5f)
                {
                    continue;
                }

                return value < 0.0f ? -1 : 1;
            }

            return 0;
        }

        void OnIndexChanged()
        {
            _mainText.text = _items[_curIndex];
        }
        #endregion
    }
}