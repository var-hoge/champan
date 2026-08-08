using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App.Actor;
using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine.UIElements;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// PlayerSelectSlot
    /// </summary>
    public class PlayerSelectSlot
        : BaseProc
        , IProcUpdate
    {
        #region プロパティ
        #endregion

        #region メソッド
        /// <summary>
        /// この席を操作するローカルのコントローラ番号を取得する
        ///
        /// オフラインでは席番号がそのままコントローラ番号になる。
        /// 他の台が担当する席は、この台では操作できない (-1 を返す)。
        /// </summary>
        /// <summary>
        /// 操作元を解決する
        /// 席テーブルはセッション参加後に届くため、決まるまで毎回試す
        /// </summary>
        void ResolveInputProxy()
        {
            if (_isInputProxyResolved)
            {
                return;
            }

            if (Network.NetworkSession.IsOnline && Network.NetworkSeatTable.Instance == null)
            {
                // まだ席が配られていない
                return;
            }

            var localInputIdx = GetLocalInputIdx();

            _inputProxy = localInputIdx >= 0
                ? TadaLib.Input.PlayerInputManager.Instance.InputProxy(localInputIdx)
                : null;

            _isInputProxyResolved = true;
        }

        int GetLocalInputIdx()
        {
            if (!Network.NetworkSession.IsOnline || Network.NetworkSeatTable.Instance == null)
            {
                return _playerIdx;
            }

            var seat = Network.NetworkSeatTable.Instance.Seats[_playerIdx];

            if (seat.IsEmpty || !Network.NetworkSeatTable.Instance.IsLocalSeat(_playerIdx))
            {
                return -1;
            }

            return seat.LocalSlot;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            // 席番号とローカルのコントローラ番号は一致しない。
            // ネットワーク対戦では「2 台目の席 2」を、その台のローカル 1 人目が操作する。
            // 席テーブルは後から届くため、ここでは解決しない (ResolveInputProxy で毎回見る)
            _cursor.AddMoveCallback((bool isRight) => OnCharaChanged(isRight));
            _cursor.AddSelectCallback(() => OnCharaSelected());
            _cursor.AddCancelCallback(() => OnCharaCanceled());
            _breadCrunchPaths = new[]
            {
                SEPath.BREAD_CRUNCH_1,
                SEPath.BREAD_CRUNCH_2,
                SEPath.BREAD_CRUNCH_3,
            };

            _arrowOriginalScale = _arrowLeft.rectTransform.localScale;
        }
        #endregion

        #region IProcUpdate の実装
        public void OnUpdate()
        {
            // 他の台が担当する席は、受け取った状態を表示するだけ
            if (Network.NetworkSession.IsOnline && !Network.SeatInput.IsLocalSeat(_playerIdx))
            {
                ApplyRemoteState();
                return;
            }

            switch (_phase)
            {
                case Phase.WaitingForEntry:
                    WaitingForEntry();
                    break;
                case Phase.InCharacterSelection:
                    InCharacterSelection();
                    break;
                case Phase.CharacterSelected:
                    CharacterSelected();
                    break;
            }
        }
        #endregion

        #region 定義
        enum Phase
        {
            WaitingForEntry, // エントリー待ち
            InCharacterSelection, // キャラ選択中
            CharacterSelected, // キャラ選択後
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        int _playerIdx = 0;

        [SerializeField]
        CharaSelectCursor _cursor;

        [SerializeField]
        CanvasGroup _charaGroup;

        [SerializeField]
        List<UnityEngine.UI.Image> _charaImages;

        [SerializeField]
        GameObject _joinButton;

        [SerializeField]
        GameObject _player;

        [SerializeField]
        UnityEngine.UI.Image _arrowLeft;

        [SerializeField]
        UnityEngine.UI.Image _arrowRight;

        Phase _phase = Phase.WaitingForEntry;
        TadaLib.Input.PlayerInputProxy _inputProxy = null;
        bool _isInputProxyResolved = false;

        bool _isReselect = false;

        string[] _breadCrunchPaths = null;

        Vector3 _arrowOriginalScale = Vector3.one;
        #endregion

        #region privateメソッド
        /// <summary>
        /// 他の台が担当する席の状態を反映する
        /// </summary>
        void ApplyRemoteState()
        {
            var state = Network.NetworkCharaSelectState.Instance;
            if (state == null)
            {
                return;
            }

            var remotePhase = state.GetPhase(_playerIdx);

            // エントリー
            if (remotePhase != Network.NetworkCharaSelectState.Phase.WaitingForEntry
                && _phase == Phase.WaitingForEntry)
            {
                _phase = Phase.InCharacterSelection;
                _cursor.Show();
                ShowChara();
            }

            if (_phase == Phase.WaitingForEntry)
            {
                return;
            }

            // カーソル位置 (= 選んでいるキャラ)
            var remoteSelectIdx = state.GetSelectIdx(_playerIdx);
            if (remoteSelectIdx != _cursor.SelectIdx)
            {
                _cursor.Setup(_cursor.Manager, remoteSelectIdx);
                _cursor.Manager.NotifyCursorOver(_playerIdx, remoteSelectIdx);
                OnCharaChanged(true);
            }

            // 決定
            if (remotePhase == Network.NetworkCharaSelectState.Phase.Selected
                && _phase != Phase.CharacterSelected)
            {
                // キャラの使用中フラグと CPU 判定はここで確定する
                // (ローカルの決定と同じ経路を通す)
                _cursor.Manager.NotifySelect(_playerIdx, remoteSelectIdx);

                _phase = Phase.CharacterSelected;
                OnCharaSelected();
            }

            // 決定の取り消し
            if (remotePhase == Network.NetworkCharaSelectState.Phase.InSelection
                && _phase == Phase.CharacterSelected)
            {
                _cursor.Manager.NotifyCancelSelect(_playerIdx);

                _phase = Phase.InCharacterSelection;
                OnCharaCanceled();
            }
        }

        /// <summary>
        /// 自分の席の状態を全員に知らせる
        /// </summary>
        void PublishState()
        {
            if (!Network.NetworkSession.IsOnline || Network.NetworkCharaSelectState.Instance == null)
            {
                return;
            }

            var phase = _phase switch
            {
                Phase.InCharacterSelection => Network.NetworkCharaSelectState.Phase.InSelection,
                Phase.CharacterSelected => Network.NetworkCharaSelectState.Phase.Selected,
                _ => Network.NetworkCharaSelectState.Phase.WaitingForEntry,
            };

            Network.NetworkCharaSelectState.Instance.Publish(_playerIdx, phase, _cursor.SelectIdx);
        }

        void WaitingForEntry()
        {
            // 最初から CPU じゃなければ自動エントリーする
            if (Cpu.CpuManager.Instance.IsCpu(_playerIdx) is false)
            {
                // 決定するまでは CPU 状態に戻す
                Cpu.CpuManager.Instance.SetIsCpu(_playerIdx, true);
                _phase = Phase.InCharacterSelection;

                _cursor.Show();
                ShowChara();
                PublishState();
                return;
            }

            // ボタン入力待ち
            ResolveInputProxy();

            // 他の台が担当する席は、この台では操作できない
            if (_inputProxy == null)
            {
                return;
            }

            if (_inputProxy.IsPressed(TadaLib.Input.ButtonCode.Action))
            {
                SEManager.Instance.Play(SEPath.PLAYER_JOIN);
                _phase = Phase.InCharacterSelection;

                _cursor.Show();
                ShowChara();
                PublishState();
            }
        }

        void InCharacterSelection()
        {

        }

        void CharacterSelected()
        {
        }

        void ShowChara()
        {
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
            var charaImage = CharacterManager.Instance.GetCharaImage(charaIdx);

            _charaGroup.gameObject.SetActive(true);

            _charaGroup.GetComponent<RectTransform>().localScale = Vector3.zero;
            _charaGroup.GetComponent<RectTransform>().DOScale(1.2f, 0.4f).SetEase(Ease.OutBack);
            _charaGroup.alpha = 0.0f;
            _charaGroup.DOFade(1.0f, 0.2f);

            foreach (var chara in _charaImages)
            {
                chara.SetSprite(charaImage);
            }
            _joinButton.gameObject.SetActive(false);
        }

        void OnCharaChanged(bool isRight)
        {
            PublishState();

            SEManager.Instance.Play(SEPath.MOVING_CURSOR);
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerIdx);
            var charaImage = CharacterManager.Instance.GetCharaImage(charaIdx);

            foreach (var chara in _charaImages)
            {
                chara.SetSprite(charaImage);
            }

            var reactionedArrow = isRight ? _arrowRight : _arrowLeft;

            reactionedArrow.rectTransform.localScale = _arrowOriginalScale;
            reactionedArrow.rectTransform.DOKill();
            reactionedArrow.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f);
        }

        void OnCharaSelected()
        {
            _phase = Phase.CharacterSelected;
            PublishState();

            var path = _breadCrunchPaths[Random.Range(0, _breadCrunchPaths.Length)];
            SEManager.Instance.Play(path, 0.3f);

            // キャラ生成
            _player.gameObject.SetActive(true);

            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, GetComponent<RectTransform>().position);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10.0f));
            _player.transform.position = worldPos;

            _charaGroup.gameObject.SetActive(false);

            if (_isReselect)
            {
                _player.GetComponent<Actor.Player.CharaCtrl>().UpdateCharaSprite();
                // ジャンプスタート
                _player.GetComponent<TadaLib.ActionStd.StateMachine>().ChangeState(typeof(Actor.Player.State.StateJump));
            }
        }

        void OnCharaCanceled()
        {
            _phase = Phase.InCharacterSelection;
            PublishState();

            SEManager.Instance.Play(SEPath.CANCEL_SELECTION);

            // キャラ削除
            _player.gameObject.SetActive(false);
            _charaGroup.gameObject.SetActive(false);
            _joinButton.gameObject.SetActive(true);

            _phase = Phase.WaitingForEntry;
            _isReselect = true;
        }
        #endregion
    }
}