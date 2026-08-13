using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using System.Linq;

namespace TadaLib.Ui.Menu.PageItem
{
    /// <summary>
    /// ValuePicker
    /// </summary>
    public class ValuePicker : IPageItem
    {
        #region 定義
        public struct Recipe
        {
            public GameObject Obj;
            public IEnumerable<string> Options;
            public int ActiveOptionIdx;
            public bool CanLoop;
            public System.Action Decided;
            public System.Action<string> ValueChanged;
            public System.Func<bool> IsEnabledFunc;
        }
        #endregion

        #region コンストラクタ
        public ValuePicker(in Recipe recipe)
        {
            _obj = recipe.Obj;
            _options = recipe.Options.ToList();
            Debug.Assert(_options.Count >= 1, "ValuePickerの選択肢は1つ以上必要です");
            ActiveOptionIdx = recipe.ActiveOptionIdx;
            CanLoop = recipe.CanLoop;
            _decided = recipe.Decided;
            _valueChanged = recipe.ValueChanged;
            _isEnabledFunc = recipe.IsEnabledFunc;
        }
        #endregion

        #region メソッド
        /// <summary>
        /// 今の値を、値が変わったときと同じように知らせる
        ///
        /// 作った時点では知らせていない。
        /// 表示は初期値になるが、それを受け取る側は前の値のままになる。
        /// 画面を作り直しても設定が引き継がれてしまうため、作った後に呼ぶ。
        /// </summary>
        public void ApplyCurrentValue()
        {
            _valueChanged?.Invoke(_options[ActiveOptionIdx]);
        }
        #endregion

        #region プロパティ
        /// <summary>
        /// 選択肢
        /// </summary>
        public IReadOnlyList<string> Options => _options;

        /// <summary>
        /// 現在選択中のインデックス
        /// </summary>
        public int ActiveOptionIdx { get; private set; }

        /// <summary>
        /// 選択をループできるか
        /// </summary>
        public bool CanLoop { get; private set; }

        /// <summary>
        /// 選択を一つ前に移動できるか
        /// </summary>
        public bool CanMoveBack => CanLoop || ActiveOptionIdx != 0;

        /// <summary>
        /// 選択を一つ先に移動できるか
        /// </summary>
        public bool CanMoveNext => CanLoop || ActiveOptionIdx != _options.Count - 1;
        #endregion

        #region メソッド
        public void Move(int nextIdx, bool isPositiveMove)
        {
            ActiveOptionIdx = nextIdx;
            OnValueChanged(_options[ActiveOptionIdx], isPositiveMove);
        }
        #endregion

        #region IPageItem の実装
        public bool IsEnabled => _isEnabledFunc();

        public void Setup()
        {
            AnimUtil.SetupAnimIfNeed(_obj, IsEnabled, _options[ActiveOptionIdx], ActiveOptionIdx);
        }

        public void OnSelected()
        {
            AnimUtil.PlaySelectedAnimIfNeed(_obj, CanMoveBack, CanMoveNext);
        }

        public void OnUnselected()
        {
            AnimUtil.PlayUnselectedAnimIfNeed(_obj);
        }

        public void OnDecided()
        {
            _decided?.Invoke();
            AnimUtil.PlayDecidedAnimIfNeed(_obj);
        }
        #endregion

        #region private フィールド
        readonly GameObject _obj;
        readonly List<string> _options;
        event System.Action _decided;
        event System.Action<string> _valueChanged;
        System.Func<bool> _isEnabledFunc;
        #endregion

        #region private メソッド
        void OnValueChanged(string optionValue, bool isPositiveMove)
        {
            _valueChanged?.Invoke(optionValue);
            AnimUtil.PlayValueChangedAnimIfNeed(_obj, optionValue, ActiveOptionIdx, isPositiveMove, CanMoveBack, CanMoveNext);
        }
        #endregion
    }
}