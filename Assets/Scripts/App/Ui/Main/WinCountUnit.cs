using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using App.Actor;
using App.Ui.CharaSelect;
using Ui.Common;

namespace App.Ui.Main
{
    /// <summary>
    /// WinCountUnit
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WinCountUnit
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void Setup(int playerIdx, bool isWinPlayer)
        {
            _playerIdx = playerIdx;
            _isWinPlayer = isWinPlayer;
        }
        #endregion

        #region MonoBehavior の実装
        void OnEnable()
        {
            Appear();
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        UnityEngine.UI.Image _charaImage;
        [SerializeField]
        UnityEngine.UI.Image _playerIdImage;
        [SerializeField]
        UnityEngine.UI.Image _panel;

        [SerializeField]
        WinCountCrownProvider _winCountCrownProvider;

        [SerializeField]
        float _fadeDurationSec = 0.2f;
        [SerializeField]
        float _moveDurationSec = 0.4f;

        [SerializeField]
        float _moveDistanceX = 200.0f;

        [SerializeField]
        float _crownAppearWaitDurationSec = 2.0f;

        [SerializeField]
        float _panelLeftPadding = 50.0f;

        [SerializeField]
        float _panelRightPadding = 20.0f;

        int _playerIdx = -1;
        bool _isWinPlayer = false;
        #endregion

        #region privateメソッド
        async void Appear()
        {
            Debug.Assert(_playerIdx >= 0);

            // 初期設定
            List<Vector3> _crownSlotGlobalPosList = new List<Vector3>();
            var curWinCount = GameMatchManager.Instance.GetWinCount(_playerIdx);
            {
                var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
                var charaSprite = CharacterManager.Instance.GetCharaMainVisualImage(charaIdx);
                _charaImage.SetSprite(charaSprite);
                _playerIdImage.SetSprite(PlayerUiManager.Instance.GetPlayerIdSprite(_playerIdx));
                // @todo: panel の画像セット

                // 王冠の設定
                {
                    var winSlotCount = GameMatchManager.Instance.WinCountToMatchFinish;

                    var width = _panel.rectTransform.rect.width - _panelLeftPadding - _panelRightPadding;
                    var center = (Vector3)_panel.rectTransform.rect.center + _panel.rectTransform.position;
                    center.x += _panelLeftPadding * 0.5f;
                    center.x -= _panelRightPadding * 0.5f;

                    if (winSlotCount == 1)
                    {
                        _crownSlotGlobalPosList.Add(center);
                    }
                    else
                    {
                        for (int idx = 0; idx < winSlotCount; ++idx)
                        {
                            var x = center.x - (width * 0.5f) + (width / (winSlotCount - 1)) * idx;
                            var pos = center;
                            pos.x = x;
                            _crownSlotGlobalPosList.Add(pos);
                        }
                    }

                    var crownCount = curWinCount;
                    if (_isWinPlayer)
                    {
                        // 一つは演出で追加するため、ここでは -1 する
                        crownCount--;
                    }

                    for (int idx = 0; idx < winSlotCount; ++idx)
                    {
                        var isCrown = idx < crownCount;
                        var pos = _crownSlotGlobalPosList[idx];
                        if (isCrown)
                        {
                            _winCountCrownProvider.RentCrown(pos);
                        }
                        else
                        {
                            _winCountCrownProvider.RentSlotMark(pos);
                        }
                    }
                }
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0.0f;
            _ = canvasGroup.DOFade(1.0f, _fadeDurationSec);
            if (_moveDistanceX > 0.0f)
            {
                var rectTransform = GetComponent<RectTransform>();
                var pos = rectTransform.localPosition;
                pos.x = -_moveDistanceX;
                rectTransform.localPosition = pos;
                _ = rectTransform.DOLocalMoveX(0, _moveDurationSec).SetEase(Ease.OutBack);
            }

            if (!_isWinPlayer)
            {
                // 勝者じゃなければここで終了
                return;
            }

            await UniTask.WaitForSeconds(_crownAppearWaitDurationSec);

            // 王冠が降ってくる
            _winCountCrownProvider.RentCrown(_crownSlotGlobalPosList[curWinCount - 1], playAnim: true);
        }
        #endregion
    }
}