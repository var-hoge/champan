using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using KanKikuchi.AudioManager;

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
                if (_invalidReason is InvalidReason.CannotAddCpu)
                {
                    return Cpu.CpuManager.Instance.CpuCount() == 0;
                }
                return false;
            }
        }
        public Vector3 CenterPos => _center.position;
        #endregion

        #region メソッド
        public void OnSelected()
        {
            if (_items.Count == 0)
            {
                return;
            }

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
            if (_items.Count == 0)
            {
                return;
            }

            _isSelected = false;

            foreach (var text in _colorChangableTexts)
            {
                text.color = _unselectedColor;
            }
            if (IsInvalid)
            {
                foreach (var text in _colorChangableTextsForInvalid)
                {
                    text.color = text.color.SetAlpha(_invalidColor.a);
                }
            }

            _selectCursorLeft.SetActive(false);
            _selectCursorRight.SetActive(false);
        }

        public void OnDecide()
        {
            if (_onItemDecided.Count == 0)
            {
                return;
            }

            _onItemDecided[_curIndex].Invoke();
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            if (_items.Count == 0)
            {
                return;
            }

            if (IsInvalid)
            {
                _curIndex = _invalidIndex;
                OnIndexChanged();
                foreach (var text in _colorChangableTextsForInvalid)
                {
                    text.color = _unselectedColor.SetAlpha(_invalidColor.a);
                }
                return;
            }

            _curIndex = _defaultIndex;
            OnIndexChanged();
        }

        public bool OnUpdate()
        {
            if (_items.Count == 0)
            {
                return false;
            }

            var inputValue = CalcInputValue();

            if (inputValue == 0)
            {
                return false;
            }
            SEManager.Instance.Play(SEPath.MOVING_CURSOR);

            var itemCount = _items.Count;
            _curIndex = (_curIndex + inputValue + itemCount) % itemCount;

            OnIndexChanged();

            return true;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Color _selectedColor;

        [SerializeField]
        Color _unselectedColor;

        [SerializeField]
        Color _invalidColor;

        [SerializeField]
        RectTransform _center;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _mainTexts;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _colorChangableTexts;

        [SerializeField]
        List<TMPro.TextMeshProUGUI> _colorChangableTextsForInvalid;

        [SerializeField]
        GameObject _selectCursorLeft;

        [SerializeField]
        GameObject _selectCursorRight;

        [SerializeField]
        List<string> _items = new List<string>();

        [SerializeField]
        List<UnityEngine.Events.UnityEvent> _onItemDecided;

        enum InvalidReason
        {
            None,
            CannotAddCpu,
        }
        [SerializeField]
        InvalidReason _invalidReason;

        [SerializeField]
        int _defaultIndex = 0;

        [SerializeField]
        int _invalidIndex = 0;

        bool _isSelected = false;
        int _curIndex = 0;
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
                if (inputManager.InputProxy(idx).AxisTrigger(TadaLib.Input.AxisCode.Horizontal, out var isPositive))
                {
                    return isPositive ? 1 : -1;
                }
            }

            return 0;
        }

        void OnIndexChanged()
        {
            foreach (var text in _mainTexts)
            {
                text.text = _items[_curIndex];
            }
        }
        #endregion
    }
}