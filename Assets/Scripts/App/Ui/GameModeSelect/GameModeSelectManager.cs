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
        public void StartGame()
        {
            SEManager.Instance.Play(SEPath.MENU_VALIDATION);
            // シーン遷移
            TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.4f, 0.4f);
            _isEnd = true;

            _menuCtrl.IsEnabled = false;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _menuCtrl.ActivePageItemChanged += () =>
            {
                SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
            };
            _menuCtrl.ActivePageItemPickedValueChanged += () =>
            {
                SEManager.Instance.Play(SEPath.MOVING_CURSOR);
            };

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

            UpdateNetworkRule();

            // ネットワーク対戦では、戻る操作もホストだけが行う
            if (Network.NetworkSession.IsOnline && !Network.NetworkSession.HasAuthority)
            {
                // 操作できないことを伝える。
                // 何も出ないと、固まっているように見えてしまう
                Ui.Common.NetworkNoticeUi.SetStatus(
                    Ui.Common.NetworkNoticeUi.StatusWaitingForHost);
                return;
            }

            UpdateBack();
        }
        #endregion

        #region private フィールド
        [SerializeField]
        TadaLib.Ui.Menu.MenuCtrl _menuCtrl;

        [SerializeField]
        MenuPages.Root _rootPage;

        [SerializeField]
        Material _backUiMaterial;

        [SerializeField]
        UnityEngine.UI.Image _backUi;

        [SerializeField]
        float _backTimeSecToDecide = 2.0f;

        bool _isEnd = false;

        float _backProgress = 0.0f;
        #endregion

        #region private メソッド
        /// <summary>
        /// ルール設定をホストからゲストへ流す
        ///
        /// ゲストはメニューを操作できない。
        /// 台ごとに設定が食い違うと勝敗条件がずれるため、決定権はホストに集める。
        /// </summary>
        void UpdateNetworkRule()
        {
            if (!Network.NetworkSession.IsOnline)
            {
                return;
            }

            var flowState = Network.NetworkFlowState.Instance;

            if (Network.NetworkSession.HasAuthority)
            {
                _rootPage.PublishRuleToGuests();

                // 選んでいる項目もゲストに伝える
                if (flowState != null && flowState.RuleMenuItemIdx != _menuCtrl.ActivePageItemIndex)
                {
                    flowState.SetRuleMenuItemIdx(_menuCtrl.ActivePageItemIndex);
                }

                return;
            }

            // ゲストは操作を受け付けず、受け取った状態を反映するだけ
            _menuCtrl.IsEnabled = false;
            _rootPage.ApplyRuleFromHost();

            if (flowState != null)
            {
                _menuCtrl.SetActivePageItemIndex(flowState.RuleMenuItemIdx);
            }
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