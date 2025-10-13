using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.Sample.Action2d.Camera;
using System.Linq;
using Unity.VisualScripting;

namespace TadaLib.Ui.Menu
{
    /// <summary>
    /// MenuCtrl
    /// </summary>
    public class MenuCtrl
        : MonoBehaviour
    {
        #region プロパティ
        public bool IsEnabled { get; set; } = true;

        public event System.Action ActivePageItemChanged;
        public event System.Action ActivePageItemPickedValueChanged;
        #endregion

        #region メソッド
        /// <summary>
        /// 最初のページのセットアップ
        /// </summary>
        public void SetupRootPage(IPage page)
        {
            Debug.Assert(_pages.Count == 0);
            _pages.Push(page);

            _pageIndicies.Push(0);
            RefreshActivePageImpl(isNeedToInitActivePageIdx: true);
            GetActivePageItem().OnSelected();

        }

        /// <summary>
        /// ページの更新
        /// </summary>
        public void RefreshActivePage()
        {
            RefreshActivePageImpl();
        }

        /// <summary>
        /// カーソルの登録
        /// </summary>
        /// <param name="cursor"></param>
        public void SetCursor(ICursor cursor)
        {
            _cursor = cursor;
            _isCursorDirty = true;
        }
        #endregion

        #region MonoBehavior の実装
        void Update()
        {
            // OnStart のタイミングで登録されている必要がある
            Debug.Assert(GetActivePage() is not null);

            if (IsEnabled is false)
            {
                return;
            }

            if (_isCursorDirty)
            {
                _isCursorDirty = false;
                _cursor?.SetupCursor(GetActivePageItemIndex());
            }

            var inputResult = HandleInput();

            var pageItemCount = _activePageCache.Count;

            switch (inputResult)
            {
                // すべての enum 値を網羅するようにする
                case InputResult.None:
                    break;
                case InputResult.Decide:
                    GetActivePageItem().OnDecided();
                    break;
                case InputResult.Cancel:
                    if (_pages.Count == 1)
                    {
                        // 最初のページなので何もしない
                    }
                    else
                    {
                        // 一つ前のページへ戻る
                        PopPage();
                        _activePageCache[GetActivePageItemIndex()].OnSelected();
                    }
                    break;
                case InputResult.MoveToNextPageItem:
                    {
                        var currentItem = GetActivePageItem();

                        // 次の選択肢へ
                        var currentItemIdx = GetActivePageItemIndex();

                        const int InvalidNextItemIdx = -1;
                        int nextItemIdx = InvalidNextItemIdx;
                        for (int idx = 1; idx < pageItemCount; ++idx)
                        {
                            var checkIdx = currentItemIdx + idx;
                            if (checkIdx == pageItemCount)
                            {
                                // 上限に達した
                                if (_canLoop is false)
                                {
                                    // ループしない場合は何もしない
                                    break;
                                }

                                // ループ
                                checkIdx = checkIdx % pageItemCount;
                            }

                            if (_activePageCache[checkIdx].IsEnabled)
                            {
                                nextItemIdx = checkIdx;
                                break;
                            }
                        }

                        if (nextItemIdx == InvalidNextItemIdx)
                        {
                            // 移動先がない
                            break;
                        }

                        // 次の選択肢へ
                        ChangeActivePageItemIndex(nextItemIdx);

                        currentItem.OnUnselected();
                        GetActivePageItem().OnSelected();

                        _cursor?.OnActiveItemChanged(GetActivePageItemIndex());

                        ActivePageItemChanged?.Invoke();
                    }
                    break;
                case InputResult.MoveToPrevPageItem:
                    {
                        var currentItem = GetActivePageItem();

                        // 前の選択肢へ
                        var currentItemIdx = GetActivePageItemIndex();

                        const int InvalidNextItemIdx = -1;
                        int nextItemIdx = InvalidNextItemIdx;
                        for (int idx = 1; idx < pageItemCount; ++idx)
                        {
                            var checkIdx = currentItemIdx - idx;
                            if (checkIdx == -1)
                            {
                                // 上限に達した
                                if (_canLoop is false)
                                {
                                    // ループしない場合は何もしない
                                    break;
                                }

                                // ループ
                                checkIdx = (checkIdx + pageItemCount) % pageItemCount;
                            }

                            if (_activePageCache[checkIdx].IsEnabled)
                            {
                                nextItemIdx = checkIdx;
                                break;
                            }
                        }

                        if (nextItemIdx == InvalidNextItemIdx)
                        {
                            // 移動先がない
                            break;
                        }

                        // 前の選択肢へ
                        ChangeActivePageItemIndex(nextItemIdx);

                        currentItem.OnUnselected();
                        GetActivePageItem().OnSelected();

                        _cursor?.OnActiveItemChanged(GetActivePageItemIndex());

                        ActivePageItemChanged?.Invoke();
                    }
                    break;
                case InputResult.MoveToNextValuePickItem:
                    {
                        var valuePicker = GetActivePageItem() as PageItem.ValuePicker;
                        // 入力時点で精査されている
                        Debug.Assert(valuePicker is not null);

                        if (valuePicker.CanMoveNext is false)
                        {
                            break;
                        }

                        var nextIdx = (valuePicker.ActiveOptionIdx + 1) % valuePicker.Options.Count;
                        valuePicker.Move(nextIdx, isPositiveMove: true);

                        ActivePageItemPickedValueChanged?.Invoke();
                    }
                    break;
                case InputResult.MoveToPrevValuePickItem:
                    {
                        var valuePicker = GetActivePageItem() as PageItem.ValuePicker;
                        // 入力時点で精査されている
                        Debug.Assert(valuePicker is not null);

                        if (valuePicker.CanMoveBack is false)
                        {
                            break;
                        }

                        var nextIdx = (valuePicker.ActiveOptionIdx - 1 + valuePicker.Options.Count) % valuePicker.Options.Count;
                        valuePicker.Move(nextIdx, isPositiveMove: false);

                        ActivePageItemPickedValueChanged?.Invoke();
                    }
                    break;
            }
        }
        #endregion

        #region 定義
        enum InputType
        {
            Gamepad1P,
            Gamepad2P,
            Gamepad3P,
            Gamepad4P,
            GamepadAll,
            Mouse,
        }

        enum AlignmentType
        {
            Vertical, // 垂直並び
            Horizontal, // 水平並び
        }

        enum CancelBehaviorType
        {
            BackToPrevItem, // 一つ前の選択肢に戻る
            BackToPrevPage, // 一つ前のページに戻る (ページがない場合は何もしない)
        }

        enum InputResult
        {
            None,
            Decide,
            Cancel,
            MoveToNextPageItem,
            MoveToPrevPageItem,
            MoveToNextValuePickItem,
            MoveToPrevValuePickItem,
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        InputType _inputType = InputType.Gamepad1P;

        [SerializeField]
        AlignmentType _alignmentType = AlignmentType.Vertical;

        [SerializeField]
        CancelBehaviorType _cancelBehaviorType = CancelBehaviorType.BackToPrevPage;

        [SerializeField]
        bool _canLoop = false;

        Stack<IPage> _pages = new();
        Stack<int> _pageIndicies = new();

        List<IPageItem> _activePageCache = null;

        ICursor? _cursor = null;
        bool _isCursorDirty = false;
        #endregion

        #region privateメソッド
        void RefreshActivePageImpl(bool isNeedToInitActivePageIdx = false)
        {
            _activePageCache = GetActivePage().Build().ToList();

            foreach (var pageItem in _activePageCache)
            {
                pageItem.Setup();
            }

            // 選択中のインデックスを初期化する
            if (isNeedToInitActivePageIdx)
            {
                _pageIndicies.Pop();
                for (int idx = 0; idx < _activePageCache.Count; ++idx)
                {
                    if (_activePageCache[idx].IsEnabled)
                    {
                        _pageIndicies.Push(idx);
                        return;
                    }
                }
                // フェイルセーフ
                _pageIndicies.Push(0);
            }
        }

        IPage? GetActivePage()
        {
            return _pages.Count > 0 ? _pages.Peek() : null;
        }

        void PopPage()
        {
            _pages.Pop();
            _pageIndicies.Pop();
            RefreshActivePageImpl();
        }

        int GetActivePageItemIndex()
        {
            return _pageIndicies.Peek();
        }

        void ChangeActivePageItemIndex(int idx)
        {
            _pageIndicies.Pop();
            _pageIndicies.Push(idx);
        }

        IPageItem GetActivePageItem()
        {
            return _activePageCache[GetActivePageItemIndex()];
        }

        InputResult HandleInput()
        {
            if (_inputType == InputType.Mouse)
            {
                return HandleMouseInput();
            }

            return HandleGamePadInput();
        }

        InputResult HandleMouseInput()
        {
            Debug.Assert(_inputType is InputType.Mouse);

            return InputResult.None;
        }

        InputResult HandleGamePadInput()
        {
            Debug.Assert(_inputType is not InputType.Mouse);

            // 優先順
            // 1. ValuePicker の選択切り替え
            // 2. PageItem の選択切り替え
            // 3. Button の決定
            // 4. キャンセル

            var inputProxies = GetInputProxies(_inputType);
            var activePageItem = GetActivePageItem();

            // 1.
            // 現在の選択項目が ValuePicker か
            if (activePageItem is PageItem.ValuePicker)
            {
                foreach (var inputProxy in inputProxies)
                {
                    var axisCode = _alignmentType switch
                    {
                        AlignmentType.Vertical => TadaLib.Input.AxisCode.Horizontal,
                        AlignmentType.Horizontal => TadaLib.Input.AxisCode.Vertical,
                        _ => throw new System.Exception()
                    };
                    if (inputProxy.AxisTrigger(axisCode, out var isPositive))
                    {
                        return isPositive ? InputResult.MoveToNextValuePickItem : InputResult.MoveToPrevValuePickItem;
                    }
                }
            }

            // 2.
            {
                foreach (var inputProxy in inputProxies)
                {
                    var axisCode = _alignmentType switch
                    {
                        AlignmentType.Vertical => TadaLib.Input.AxisCode.Vertical,
                        AlignmentType.Horizontal => TadaLib.Input.AxisCode.Horizontal,
                        _ => throw new System.Exception()
                    };
                    if (inputProxy.AxisTrigger(axisCode, out var isPositive))
                    {
                        return isPositive ? InputResult.MoveToPrevPageItem : InputResult.MoveToNextPageItem;
                    }
                }

                // ValuePicker の場合は決定で移動する
                if (activePageItem is PageItem.ValuePicker)
                {
                    foreach (var inputProxy in inputProxies)
                    {
                        if (inputProxy.IsPressedTrigger(Input.ButtonCode.Decide))
                        {
                            return InputResult.MoveToNextPageItem;
                        }
                    }
                }
            }

            // 3.
            // 現在の選択項目が Button か
            if (activePageItem is PageItem.Button)
            {
                foreach (var inputProxy in inputProxies)
                {
                    if (inputProxy.IsPressedTrigger(Input.ButtonCode.Decide))
                    {
                        return InputResult.Decide;
                    }
                }
            }

            // 4.
            {
                foreach (var inputProxy in inputProxies)
                {
                    if (inputProxy.IsPressedTrigger(Input.ButtonCode.Cancel))
                    {
                        return _cancelBehaviorType switch
                        {
                            CancelBehaviorType.BackToPrevPage => InputResult.Cancel,
                            CancelBehaviorType.BackToPrevItem => InputResult.MoveToPrevPageItem,
                            _ => throw new System.Exception()
                        };
                    }
                }
            }

            // 何もなし
            return InputResult.None;
        }

        IEnumerable<TadaLib.Input.PlayerInputProxy> GetInputProxies(InputType inputType)
        {
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            Debug.Assert(inputManager != null);

            return inputType switch
            {
                InputType.Gamepad1P => new[] { inputManager.InputProxy(0) },
                InputType.Gamepad2P => new[] { inputManager.InputProxy(1) },
                InputType.Gamepad3P => new[] { inputManager.InputProxy(2) },
                InputType.Gamepad4P => new[] { inputManager.InputProxy(3) },
                InputType.GamepadAll => inputManager.InputProxies,
                _ => throw new System.Exception()
            };
        }
        #endregion
    }
}