using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class CharaSelectCursor
        : MonoBehaviour
    {
        #region プロパティ
        public int SelectIdx => _selectIdx;
        public int PlayerIdx => _playerIdx;
        public CharaSelectUiManager Manager => _manager;

        public bool IsSelectDone { private set; get; } = false;
        #endregion

        #region メソッド
        public void Setup(CharaSelectUiManager manager, int selectIdx)
        {
            _manager = manager;
            _selectIdx = selectIdx;
            GetComponent<RectTransform>().position = _manager.GetPickIconTransform(_playerIdx, _selectIdx).position;
            GetComponent<RectTransform>().rotation = _manager.GetPickIconTransform(_playerIdx, _selectIdx).rotation;
        }

        public void Show()
        {
            _body.enabled = true;

            // 振動
            TadaLib.Input.PlayerInputManager.Instance.InputProxy(_playerIdx).VibrateAdvanced(0.3f, 0.3f, 0.04f);

            var rectTransform = GetComponent<RectTransform>();
            _initScale = rectTransform.localScale;
            rectTransform.localScale = Vector3.zero;
            GetComponent<RectTransform>().DOScale(_initScale, 0.4f).SetEase(Ease.OutBack);
            GetComponent<UnityEngine.UI.Image>().DOFade(1.0f, 0.2f);

            _manager.NotifyCursorOver(_playerIdx, _selectIdx);

            _isReady = true;
        }

        public void AddMoveCallback(System.Action<bool> callback)
        {
            _moveCallbacks.Add(callback);
        }

        public void AddSelectCallback(System.Action callback)
        {
            _selectCallbacks.Add(callback);
        }

        public void AddCancelCallback(System.Action callback)
        {
            _cancelCallbacks.Add(callback);
        }

        public void ForceMove(bool isRight)
        {
            MoveImpl(isRight);
        }

        /// <summary>
        /// 他の台が担当する席の決定状態を見た目に反映する
        /// 決定するとカーソルは消える
        /// </summary>
        public void ApplyRemoteSelected(bool isSelected)
        {
            if (IsSelectDone == isSelected)
            {
                return;
            }

            IsSelectDone = isSelected;
            _body.enabled = !isSelected;
        }

        public void ForceCancel()
        {
            OnCancelSelect();
        }
        #endregion

        #region privateフィールド
        CharaSelectUiManager _manager;

        [SerializeField]
        int _playerIdx = 0;

        [SerializeField]
        UnityEngine.UI.Image _body;

        int _selectIdx = 0;

        /// <summary>
        /// 購読中の入力 (他の台が担当する席なら null)
        /// </summary>
        TadaLib.Input.PlayerInputProxy _subscribedProxy = null;
        bool _isInputResolved = false;

        float _moveInputValuePrev = 0.0f;
        bool _isReady = false;
        Vector3 _initScale;

        List<System.Action<bool>> _moveCallbacks = new List<System.Action<bool>>();
        List<System.Action> _selectCallbacks = new List<System.Action>();
        List<System.Action> _cancelCallbacks = new List<System.Action>();
        #endregion

        #region privateメソッド
        void OnMove(Vector2 value)
        {
            if (!_isReady)
            {
                return;
            }

            if (IsSelectDone)
            {
                return;
            }

            // 入力開始時だけ受け付ける
            if ((value.x > 0.5f && _moveInputValuePrev > 0.5) ||
                    (value.x < -0.5f && _moveInputValuePrev < -0.5f))
            {
                // 同じ
                return;
            }

            _moveInputValuePrev = value.x;

            // 入力値が少ない場合はなし
            if (Mathf.Abs(value.x) < 0.5f)
            {
                return;
            }

            MoveImpl(value.x > 0.0f);
        }

        void OnAction()
        {
            if (!_isReady)
            {
                return;
            }

            if (IsSelectDone)
            {
                return;
            }

            if (!_manager.NotifySelect(_playerIdx, _selectIdx))
            {
                // 選べなかった
                return;
            }

            // 決定
            OnSelect();
        }

        void OnCancel()
        {
            if (IsSelectDone)
            {
                return;
            }

            if (!_isReady)
            {
                return;
            }

            OnCancelSelect(isPreEntry: true);
        }


        private void Start()
        {
            _selectIdx = _manager.CharaIdxToSelectIdx(CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx));

            // 入力の購読は席が決まってから行う (TrySubscribeInput)
            // ネットワーク対戦では席テーブルが届くまで、どの入力を使うか決まらない

            // 最初は非表示
            var image = GetComponent<UnityEngine.UI.Image>();
            image.color = image.color.SetAlpha(0.0f);

            _initScale = GetComponent<RectTransform>().localScale;
        }

        private void Update()
        {
            TrySubscribeInput();
        }

        private void OnDestroy()
        {
            if (_subscribedProxy == null)
            {
                return;
            }

            _subscribedProxy.OnAction -= OnAction;
            _subscribedProxy.OnMove -= OnMove;
            _subscribedProxy.OnCancel -= OnCancel;
            _subscribedProxy = null;
        }

        /// <summary>
        /// この席を担当する入力を購読する
        ///
        /// 他の台が担当する席では購読しない (受信した状態で表示するだけ)。
        /// 席テーブルはセッション参加後に届くため、決まるまで毎フレーム試す。
        /// </summary>
        void TrySubscribeInput()
        {
            if (_isInputResolved)
            {
                return;
            }

            if (Network.NetworkSession.IsOnline && Network.NetworkSeatTable.Instance == null)
            {
                return;
            }

            _isInputResolved = true;

            _subscribedProxy = Network.SeatInput.GetProxyOrNull(_playerIdx);
            if (_subscribedProxy == null)
            {
                return;
            }

            _subscribedProxy.OnAction += OnAction;
            _subscribedProxy.OnMove += OnMove;
            _subscribedProxy.OnCancel += OnCancel;
        }

        void MoveImpl(bool isRight)
        {
            var addIdx = isRight ? 1 : -1;

            var nextSelectIdx = _selectIdx;
            while (true)
            {
                nextSelectIdx = (nextSelectIdx + addIdx + _manager.CharaMaxCount) % _manager.CharaMaxCount;

                if (!_manager.IsUsed(nextSelectIdx))
                {
                    break;
                }
            }

            if (_selectIdx != nextSelectIdx)
            {
                _selectIdx = nextSelectIdx;
                GetComponent<RectTransform>().DOKill();
                GetComponent<RectTransform>().localScale = _initScale;
                GetComponent<RectTransform>().DOMove(_manager.GetPickIconTransform(_playerIdx, _selectIdx).position, 0.2f);
                GetComponent<RectTransform>().DORotate(_manager.GetPickIconTransform(_playerIdx, _selectIdx).eulerAngles, 0.2f);

                _manager.NotifyCursorOver(_playerIdx, _selectIdx);

                foreach (var callback in _moveCallbacks)
                {
                    callback(isRight);
                }
            }
        }

        void OnSelect()
        {
            Debug.Assert(!IsSelectDone);

            IsSelectDone = true;

            _body.enabled = false;

            foreach (var callback in _selectCallbacks)
            {
                callback();
            }

            // 振動
            TadaLib.Input.PlayerInputManager.Instance.InputProxy(_playerIdx).VibrateAdvanced(0.2f, 0.8f, 0.04f);
        }

        void OnCancelSelect(bool isPreEntry = false)
        {
            if (!isPreEntry)
            {
                // なぜか何度も呼ばれる
                if (!IsSelectDone)
                {
                    return;
                }
                Debug.Assert(IsSelectDone);

                _manager.NotifyCancelSelect(PlayerIdx);
            }

            IsSelectDone = false;
            _body.enabled = false;

            _isReady = false;

            foreach (var callback in _cancelCallbacks)
            {
                callback();
            }

            TadaLib.Input.PlayerInputManager.Instance.InputProxy(_playerIdx).VibrateAdvanced(0.2f, 0.3f, 0.04f);
        }
        #endregion
    }
}