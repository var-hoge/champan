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

                items.Add(new TadaLib.Ui.Menu.PageItem.ValuePicker(recipe));
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

                items.Add(new TadaLib.Ui.Menu.PageItem.ValuePicker(recipe));
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
        #endregion

        #region privateメソッド
        #endregion
    }
}