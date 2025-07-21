using System.Collections.Generic;
using DG.Tweening;
using TadaLib.ProcSystem;
using UnityEngine;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// CharaCancelImage
    /// </summary>
    public class CharaCancelImage
        : BaseProc
        , IProcPostMove
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _progressImage.fillAmount = 0.0f;
            GetComponent<CanvasGroup>().alpha = 0.0f;
        }
        #endregion

        #region TadaLib.ProcSys.IProcPostMove の実装
        public void OnPostMove()
        {
            var player = TadaLib.ActionStd.PlayerManager.TryGetPlayer(_playerId);

            if (player == null || !player.gameObject.activeSelf)
            {
                // プレイヤーがいなくなったら、キャンセル状態をリセット
                _isCancelStaging = false;
            }

            var isActive = player != null && player.gameObject.activeSelf && _progress > 0.0f;
            if (_isCancelStaging)
            {
                isActive = true;
            }
            if (_selectCursor.Manager.IsSceneChanging)
            {
                isActive = false;
            }

            if (isActive != _isActivePrev)
            {
                _isActivePrev = isActive;
                var alpha = isActive ? 1.0f : 0.0f;
                var canvasGroup = GetComponent<CanvasGroup>();
                canvasGroup.DOKill();
                canvasGroup.DOFade(alpha, 0.1f);
            }

            if (player != null)
            {
                UpdatePos(player.transform);
                UpdateProgress();
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        CharaSelectCursor _selectCursor;

        [SerializeField]
        int _playerId = 0;

        [SerializeField]
        UnityEngine.UI.Image _progressImage;

        [SerializeField]
        float _cancelTimeSec = 1.2f;

        float _progress = 0.0f;
        bool _isActivePrev = false;

        bool _isCancelStaging = false;
        #endregion

        #region privateメソッド
        void UpdateProgress()
        {
            if (_isCancelStaging)
            {
                return;
            }

            bool IsPreseed()
            {
                var inputManager = TadaLib.Input.PlayerInputManager.Instance;
                if (inputManager.InputProxy(_playerId).IsPressed(TadaLib.Input.ButtonCode.Cancel))
                {
                    return true;
                }
                return false;
            }

            if (IsPreseed())
            {
                _progress += Time.deltaTime;
            }
            else
            {
                _progress = 0.0f;
            }

            var rate = _progress / _cancelTimeSec;
            _progressImage.fillAmount = Mathf.Min(rate, 1.0f);
            if (rate >= 1.0f)
            {
                _selectCursor.ForceCancel();
                _isCancelStaging = true;
            }
        }

        void UpdatePos(Transform playerTrans)
        {
            // プレイヤーの頭上に移動させる
            var screenPos = Camera.main.WorldToScreenPoint(playerTrans.position);

            var offsetY = 230.0f;

            GetComponent<RectTransform>().position = screenPos + offsetY * Vector3.up;
        }
        #endregion
    }
}