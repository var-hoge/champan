using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.GameModeSelect.MenuPages
{
    /// <summary>
    /// Root
    /// </summary>
    public class Root
        : MonoBehaviour
        , TadaLib.Ui.Menu.IPage
    {
        #region プロパティ
        #endregion

        #region メソッド
        /// <summary>
        /// ホストの設定をゲストに配る
        ///
        /// 変化した瞬間に送るだけだと、その前に合流したゲストへ何も伝わらないため、
        /// 現在値を反映し続ける。
        /// </summary>
        public void PublishRuleToGuests()
        {
            var flowState = Network.NetworkFlowState.Instance;
            if (flowState == null)
            {
                return;
            }

            if (_cpuPicker != null && flowState.RuleCpuOptionIdx != _cpuPicker.ActiveOptionIdx)
            {
                flowState.SetRuleCpuOptionIdx(_cpuPicker.ActiveOptionIdx);
            }

            if (_winCountPicker != null && flowState.RuleWinCountOptionIdx != _winCountPicker.ActiveOptionIdx)
            {
                flowState.SetRuleWinCountOptionIdx(_winCountPicker.ActiveOptionIdx);
            }
        }

        /// <summary>
        /// ホストの設定を反映する
        ///
        /// ValuePicker.Move を通すことで、見た目と設定値の両方が
        /// ホストで操作したときと同じ経路で更新される。
        /// </summary>
        public void ApplyRuleFromHost()
        {
            var flowState = Network.NetworkFlowState.Instance;
            if (flowState == null)
            {
                return;
            }

            ApplyPicker(_cpuPicker, flowState.RuleCpuOptionIdx);
            ApplyPicker(_winCountPicker, flowState.RuleWinCountOptionIdx);
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _menuCtrl.SetupRootPage(this);
        }
        #endregion

        #region IPage の実装
        public IEnumerable<TadaLib.Ui.Menu.IPageItem> Build()
        {
            var items = new List<TadaLib.Ui.Menu.IPageItem>();

            {
                var recipe = new TadaLib.Ui.Menu.PageItem.ValuePicker.Recipe();
                recipe.Obj = _addCpu.gameObject;
                recipe.Options = new string[] { "Yes", "No" };
                recipe.ActiveOptionIdx = 0;
                recipe.CanLoop = true;
                recipe.Decided += () =>
                {
                };
                recipe.ValueChanged += (string value) =>
                {
                    GameMatchManager.Instance.SetIsExistCpu(value == "Yes");
                };
                recipe.IsEnabledFunc = () => Cpu.CpuManager.Instance.CpuCount() != 0; // CPUが0なら選択不可

                _cpuPicker = new TadaLib.Ui.Menu.PageItem.ValuePicker(recipe);
                items.Add(_cpuPicker);
            }

            {
                var recipe = new TadaLib.Ui.Menu.PageItem.ValuePicker.Recipe();
                recipe.Obj = _playsToWin.gameObject;
                recipe.Options = new string[] { "1", "3", "5" };
                recipe.ActiveOptionIdx = 1;
                recipe.CanLoop = true;
                recipe.Decided += () =>
                {
                };
                recipe.ValueChanged += (string value) =>
                {
                    GameMatchManager.Instance.SetWinCountToMatchFinish(int.Parse(value));
                };
                recipe.IsEnabledFunc = () => true;

                _winCountPicker = new TadaLib.Ui.Menu.PageItem.ValuePicker(recipe);
                items.Add(_winCountPicker);
            }

            {
                var recipe = new TadaLib.Ui.Menu.PageItem.Button.Recipe();
                recipe.Obj = _start.gameObject;
                recipe.Decided += () =>
                {
                    _manager.StartGame();
                };
                recipe.IsEnabledFunc = () => true;

                items.Add(new TadaLib.Ui.Menu.PageItem.Button(recipe));
            }

            return items;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        GameModeSelectManager _manager;

        [SerializeField]
        TadaLib.Ui.Menu.MenuCtrl _menuCtrl;

        [SerializeField]
        RectTransform _addCpu;

        [SerializeField]
        RectTransform _playsToWin;

        [SerializeField]
        RectTransform _start;

        /// <summary>
        /// ネットワーク対戦でホストの設定を反映するために保持する
        /// </summary>
        TadaLib.Ui.Menu.PageItem.ValuePicker _cpuPicker;
        TadaLib.Ui.Menu.PageItem.ValuePicker _winCountPicker;
        #endregion

        #region privateメソッド
        static void ApplyPicker(TadaLib.Ui.Menu.PageItem.ValuePicker picker, int optionIdx)
        {
            if (picker == null || picker.ActiveOptionIdx == optionIdx)
            {
                return;
            }

            var isPositive = optionIdx > picker.ActiveOptionIdx;

            picker.Move(optionIdx, isPositive);
        }
        #endregion
    }
}