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

namespace Ui
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
            var rectTransform = GetComponent<RectTransform>();
            var initScale = rectTransform.localScale;
            rectTransform.localScale = Vector3.zero;
            GetComponent<RectTransform>().DOScale(initScale, 0.4f).SetEase(Ease.OutBack);
            GetComponent<UnityEngine.UI.Image>().DOFade(1.0f, 0.2f);

            _manager.NotifyCursorOver(_playerIdx, _selectIdx);

            _isReady = true;
        }

        public void AddMoveCallback(System.Action callback)
        {
            _moveCallbacks.Add(callback);
        }

        public void AddSelectCallbac(System.Action callback)
        {
            _selectCallbacks.Add(callback);
        }

        public void ForceMove(bool isRight)
        {
            MoveImpl(isRight);
        }
        #endregion

        #region privateフィールド
        CharaSelectUiManager _manager;

        [SerializeField]
        int _playerIdx = 0;

        [SerializeField]
        UnityEngine.UI.Image _body;

        int _selectIdx = 0;

        float _moveInputValuePrev = 0.0f;
        bool _isReady = false;

        List<System.Action> _moveCallbacks = new List<System.Action>();
        List<System.Action> _selectCallbacks = new List<System.Action>();
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
                //if (!_manager.NotifyCancelSelect(_playerIdx, _selectIdx))
                //{
                //    // キャンセルできなかった
                //    return;
                //}

                //OnCancelSelect();

                //return;
            }

            if (!_manager.NotifySelect(_playerIdx, _selectIdx))
            {
                // 選べなかった
                return;
            }

            // 決定
            OnSelect();
        }

        private void Start()
        {
            _selectIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);

            GameController.Instance.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnAction += OnAction;
            GameController.Instance.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnMove += OnMove;
            //GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered += OnAction;

            // 最初は非表示
            var image = GetComponent<UnityEngine.UI.Image>();
            image.color = image.color.SetAlpha(0.0f);
        }

        private void OnDestroy()
        {
            var gameController = GameController.Instance;
            if (gameController == null)
            {
                return;
            }
            gameController.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnAction -= OnAction;
            gameController.GetPlayerInput(_playerIdx).GetComponent<PlayerInputHandler>().OnMove -= OnMove;
            //GameController.Instance.GetPlayerInput(_playerIdx).onActionTriggered -= OnAction;
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
                GetComponent<RectTransform>().DOMove(_manager.GetPickIconTransform(_playerIdx, _selectIdx).position, 0.2f);
                GetComponent<RectTransform>().DORotate(_manager.GetPickIconTransform(_playerIdx, _selectIdx).eulerAngles, 0.2f);

                _manager.NotifyCursorOver(_playerIdx, _selectIdx);

                foreach (var callback in _moveCallbacks)
                {
                    callback();
                }
            }
        }

        void OnSelect()
        {
            Debug.Assert(!IsSelectDone);

            IsSelectDone = true;

            _body.enabled = false;

            foreach(var callback in _selectCallbacks)
            {
                callback();
            }
        }

        void OnCancelSelect()
        {
            Debug.Assert(IsSelectDone);

            IsSelectDone = false;

            _body.enabled = true;
        }
        #endregion
    }
}