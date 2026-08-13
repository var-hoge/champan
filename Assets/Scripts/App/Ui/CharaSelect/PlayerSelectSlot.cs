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
            // 席が決まると、どの操作元がこの席を担当するかが変わる。
            //
            // オフラインのうちは席番号がそのまま操作元になるが、
            // 部屋に入ると「2 台目の席 2」をその台の 1 人目が操作する形になる。
            // 一度決めたきりにすると、入った後も古い対応のままになり、
            // 自分のキャラを操作できず、他人のキャラを動かしてしまう。
            var isOnline = Network.NetworkSession.IsOnline;
            if (_isInputProxyResolved && _isInputProxyResolvedOnline == isOnline)
            {
                return;
            }

            if (isOnline && Network.NetworkSeatTable.Instance == null)
            {
                // まだ席が配られていない
                return;
            }

            var localInputIdx = GetLocalInputIdx();

            _inputProxy = localInputIdx >= 0
                ? TadaLib.Input.PlayerInputManager.Instance.InputProxy(localInputIdx)
                : null;

            // 席の割り当ては要求してから戻るまでに一往復かかる。
            // 空席のまま決めてしまうと、割り当てられた後も操作できないままになる。
            _isInputProxyResolved = !isOnline
                || !Network.NetworkSeatTable.Instance.Seats[_playerIdx].IsEmpty;

            _isInputProxyResolvedOnline = isOnline;
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
            HideCharaUntilSelected();

            // 席が配られる前は、どの席が自分の担当か分からない。
            // その状態で「他の台の席」として扱うと、自分のキャラの移動を止めてしまう。
            var isSeatKnown = !Network.NetworkSession.IsOnline
                || Network.NetworkSeatTable.Instance != null;

            // 他の台が担当する席は、受け取った状態を表示するだけ
            if (isSeatKnown
                && Network.NetworkSession.IsOnline
                && !Network.SeatInput.IsLocalSeat(_playerIdx))
            {
                // 部屋に入った直後は、画面をやり直すまで触らない。
                // 作り直す前の画面に相手の選択が現れると、ちぐはぐに見える。
                if (NetworkRoomWindow.IsRemoteApplySuspended)
                {
                    return;
                }

                ApplyRemoteState();
                ApplyRemoteCharaPos();
                return;
            }

            // 自分の担当なら、自分で動かせる状態に戻す
            RestoreLocalCharaPhysics();

            // 前回の「決定済み」が残っていると、
            // 戻ってきた相手が即座に扉へ入ってしまう。
            // 自分の席は自分の台しか書き換えられないため、ここで送り直す。
            //
            // Start では早すぎる。
            // 席テーブルもカーソルもまだ整っておらず、送っても弾かれる。
            if (isSeatKnown && !_isInitialStatePublished)
            {
                _isInitialStatePublished = true;
                PublishState();
            }

            PublishCharaPos();

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

        /// <summary>
        /// 操作元を決めたときに部屋へ入っていたか
        /// 入る前と後で対応が変わるため、変わったら決め直す
        /// </summary>
        bool _isInputProxyResolvedOnline = false;

        /// <summary>
        /// この距離以下しか動いていなければ送らない
        /// </summary>
        const float PosPublishThresholdSqr = 0.0001f;

        /// <summary>
        /// 受け取った位置のたどり方
        /// 対戦シーンと同じものを使う
        /// </summary>
        readonly Network.RemoteValueSmoother _posSmoother =
            Network.RemoteSmootherFactory.CreateForPosition();

        Vector2 _lastPublishedPos = Vector2.zero;
        bool _lastPublishedFacingLeft = false;

        /// <summary>
        /// 他の台の席として扱い、キャラの移動を止めているか
        /// </summary>
        bool _isCharaPhysicsDisabled = false;

        bool _isReselect = false;

        /// <summary>
        /// 画面に入ってから自分の席の状態を送り直したか
        /// </summary>
        bool _isInitialStatePublished = false;

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

            // 誰も座っていない席なのに、この枠が入った状態のままなら戻す。
            //
            // 対戦から戻ると、前回遊んでいた人が自動で再エントリーされる
            // (ローカル対戦では正しい動き)。
            // その状態で部屋に入ると席は空から始まるため、
            // 席の無い枠が入ったまま残ってしまう。
            //
            // 席の状態より前に見る。
            // 空席には今回の印が付いておらず、下の判定で弾かれてしまうため。
            var seatTable = Network.NetworkSeatTable.Instance;
            if (seatTable != null
                && seatTable.IsCpuSeat(_playerIdx)
                && _phase != Phase.WaitingForEntry)
            {
                if (_phase == Phase.CharacterSelected)
                {
                    _cursor.Manager.NotifyCancelSelect(_playerIdx);
                }

                _phase = Phase.WaitingForEntry;
                HideForLeave();
                return;
            }

            var remotePhase = state.GetPhase(_playerIdx);

            // 前回の値が残ったまま読むと、相手が即座に扉へ入ってしまう。
            // 消すのはホストの担当で、他の台には少し遅れて届く。
            // 今回のキャラセレクトで書かれた値だけを読む。
            if (!state.IsSeatFresh(_playerIdx))
            {
                return;
            }

            // 退出
            //
            // 部屋を出た席はエントリー待ちに戻される。
            // ここで戻さないと、出た人のキャラが選ばれたまま残り続ける。
            if (remotePhase == Network.NetworkCharaSelectState.Phase.WaitingForEntry
                && _phase != Phase.WaitingForEntry)
            {
                if (_phase == Phase.CharacterSelected)
                {
                    // 使用中のキャラを空ける
                    _cursor.Manager.NotifyCancelSelect(_playerIdx);
                    _cursor.ApplyRemoteSelected(false);
                }

                _phase = Phase.WaitingForEntry;
                HideForLeave();
                return;
            }

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
            // 位置を直接セットするとアニメが飛ぶので、ローカルと同じ移動を再生する
            var remoteSelectIdx = state.GetSelectIdx(_playerIdx);
            if (remoteSelectIdx != _cursor.SelectIdx)
            {
                var isRight = IsRightDirection(_cursor.SelectIdx, remoteSelectIdx);

                // 1 回の移動で追いつかない場合があるため、届くまで動かす
                var guard = 0;
                while (_cursor.SelectIdx != remoteSelectIdx && guard < _cursor.Manager.CharaMaxCount)
                {
                    _cursor.ForceMove(isRight);
                    ++guard;
                }
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

                // 決定したらカーソルを消す
                _cursor.ApplyRemoteSelected(true);
            }

            // 決定の取り消し
            if (remotePhase == Network.NetworkCharaSelectState.Phase.InSelection
                && _phase == Phase.CharacterSelected)
            {
                _cursor.Manager.NotifyCancelSelect(_playerIdx);

                _phase = Phase.InCharacterSelection;
                OnCharaCanceled();

                // 取り消したらカーソルを戻す
                _cursor.ApplyRemoteSelected(false);
            }
        }

        /// <summary>
        /// 現在位置から目的位置へ動かすとき、右回りが近いかどうか
        /// カーソルは端で折り返さず一周するため、近い方を選ぶ
        /// </summary>
        bool IsRightDirection(int fromIdx, int toIdx)
        {
            var count = _cursor.Manager.CharaMaxCount;
            var rightSteps = ((toIdx - fromIdx) % count + count) % count;

            return rightSteps <= count - rightSteps;
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

            var charaPos = _player != null
                ? (Vector2)_player.transform.position
                : Vector2.zero;

            Network.NetworkCharaSelectState.Instance.Publish(
                _playerIdx, phase, _cursor.SelectIdx, charaPos, IsPlayerFacingLeft());
        }

        /// <summary>
        /// キャラが左を向いているか
        /// </summary>
        bool IsPlayerFacingLeft()
        {
            if (_player == null)
            {
                return false;
            }

            var rotateCtrl = _player.GetComponent<Actor.Player.RotateCtrl>();

            return rotateCtrl != null && rotateCtrl.IsFacingLeft;
        }

        /// <summary>
        /// 決定後のキャラの位置を配る
        /// 座標は毎フレーム変わるため、状態の更新とは別に送る
        /// </summary>
        void PublishCharaPos()
        {
            if (_phase != Phase.CharacterSelected)
            {
                return;
            }

            if (!Network.NetworkSession.IsOnline || Network.NetworkCharaSelectState.Instance == null)
            {
                return;
            }

            // @memo: ここで間隔を空けて送っていたが、やめた。
            //
            // 1 フレームぶんの間隔と 1 フレームの経過時間はほぼ同じ長さのため、
            // わずかな揺らぎで「送るフレーム」と「送らないフレーム」が交互になり、
            // 受け取る側から見ると進んだり止まったりして見えていた。
            //
            // 送るのは値の書き込みだけで、実際に運ばれるのはティックごとのため、
            // 毎フレーム書いても通信量は変わらない。

            // 動いておらず向きも変わっていないなら送らない
            var currentPos = (Vector2)_player.transform.position;
            var isFacingLeft = IsPlayerFacingLeft();

            var isPosChanged = (currentPos - _lastPublishedPos).sqrMagnitude >= PosPublishThresholdSqr;
            var isFacingChanged = isFacingLeft != _lastPublishedFacingLeft;

            if (!isPosChanged && !isFacingChanged)
            {
                return;
            }

            _lastPublishedPos = currentPos;
            _lastPublishedFacingLeft = isFacingLeft;

            Network.NetworkCharaSelectState.Instance.Publish(
                _playerIdx,
                Network.NetworkCharaSelectState.Phase.Selected,
                _cursor.SelectIdx,
                _player.transform.position,
                isFacingLeft);
        }

        /// <summary>
        /// 自分が担当する席のキャラを、自分で動かせる状態に戻す
        ///
        /// 席が配られる前に「他の台の席」として扱ってしまうと移動を止めるため、
        /// 判明した時点で戻す必要がある。
        /// </summary>
        void RestoreLocalCharaPhysics()
        {
            if (_player == null || !_isCharaPhysicsDisabled)
            {
                return;
            }

            var moveCtrl = _player.GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null)
            {
                moveCtrl.enabled = true;
            }

            var rigidbody = _player.GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.enabled = true;
            }

            _isCharaPhysicsDisabled = false;
        }

        /// <summary>
        /// 他の台が担当する席のキャラは、受け取った位置へ動かす
        /// 自前で動かすと二重に動いてしまうため、ローカルの移動は止める
        /// </summary>
        void ApplyRemoteCharaPos()
        {
            if (_player == null || !_player.gameObject.activeSelf)
            {
                return;
            }

            var moveCtrl = _player.GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null && moveCtrl.enabled)
            {
                moveCtrl.enabled = false;
            }

            var rigidbody = _player.GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            if (rigidbody != null && rigidbody.enabled)
            {
                rigidbody.enabled = false;
            }

            _isCharaPhysicsDisabled = true;

            // 向きは自前で決められないので、受け取った値をそのまま反映する
            var rotateCtrl = _player.GetComponent<Actor.Player.RotateCtrl>();
            if (rotateCtrl != null)
            {
                rotateCtrl.SetFacingLeft(Network.NetworkCharaSelectState.Instance.IsFacingLeft(_playerIdx));
            }

            var targetPos = Network.NetworkCharaSelectState.Instance.GetCharaPos(_playerIdx);
            if (targetPos == Vector2.zero)
            {
                return;
            }

            var current = _player.transform.position;

            // 届いた座標を追いかけると、届く間隔のばらつきが動きのムラになる。
            // 少し遅らせて、届いた座標の間をたどる。
            var syncPos = new Vector3(targetPos.x, targetPos.y, current.z);
            if (!_posSmoother.TryFollow(syncPos, current, out var next))
            {
                return;
            }

            _player.transform.position = new Vector3(next.x, next.y, current.z);
        }

        void WaitingForEntry()
        {
            // ネットワーク対戦では、席が付くまでエントリーしない。
            //
            // 部屋に入る前はオフラインと同じ状態のため、
            // 「CPU でなければ自動でエントリーする」がそのまま働き、
            // 4 席とも一瞬エントリーされてしまう。
            if (Network.NetworkSession.IsOnline
                && !Network.SeatInput.IsLocalSeat(_playerIdx))
            {
                return;
            }

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

            // ネットワーク対戦では、押されたかどうかを見るのは NetworkEntryWatcher。
            // 席が無い状態で押すため、この枠では受けられない。
            // ここまで来た時点で席が付いているので、そのままエントリーする。
            if (Network.NetworkSession.IsOnline)
            {
                if (_inputProxy == null)
                {
                    return;
                }

                EnterCharacterSelection();
                return;
            }

            // 他の台が担当する席は、この台では操作できない
            if (_inputProxy == null)
            {
                return;
            }

            if (_inputProxy.IsPressed(TadaLib.Input.ButtonCode.Action))
            {
                EnterCharacterSelection();
            }
        }

        /// <summary>
        /// エントリーしてキャラ選択に入る
        /// </summary>
        void EnterCharacterSelection()
        {
            SEManager.Instance.Play(SEPath.PLAYER_JOIN);
            _phase = Phase.InCharacterSelection;

            _cursor.Show();
            ShowChara();
            PublishState();
        }

        void InCharacterSelection()
        {

        }

        void CharacterSelected()
        {
        }

        /// <summary>
        /// キャラを決めるまでは、キャラを出さないでおく
        ///
        /// シーンに置かれたキャラは無効な状態で待っているが、
        /// Fusion はシーン上の NetworkObject を Spawn するときに有効化する。
        /// そのため、こちらが出すつもりのない時点で勝手に現れてしまう。
        ///
        /// 置き場所を決めるのはキャラを決めたときなので、
        /// 勝手に現れたキャラは原点 (画面の中央) に立ってしまう。
        ///
        /// いつ有効にされるか分からないため、
        /// 一度きりの初期化ではなく、毎フレーム見て閉じ直す。
        /// </summary>
        void HideCharaUntilSelected()
        {
            if (_player == null || _phase == Phase.CharacterSelected)
            {
                return;
            }

            if (_player.activeSelf)
            {
                _player.SetActive(false);
            }
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

        /// <summary>
        /// この席を返す
        ///
        /// 席を持っていない状態に戻すため、操作元も決め直させる。
        /// </summary>
        void ReleaseSeatIfOnline()
        {
            if (!Network.NetworkSession.IsOnline)
            {
                return;
            }

            var seatTable = Network.NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                return;
            }

            var seat = seatTable.Seats[_playerIdx];
            if (seat.IsEmpty || !seatTable.IsLocalSeat(_playerIdx))
            {
                return;
            }

            seatTable.ReleaseSeat(seat.LocalSlot);

            _isInputProxyResolved = false;
            _inputProxy = null;
        }

        /// <summary>
        /// 部屋を出た席の見た目を片付ける
        ///
        /// 取り消しと違い、音は鳴らさない。
        /// 自分の操作ではないため、鳴ると誤解を招く。
        /// </summary>
        void HideForLeave()
        {
            _player.gameObject.SetActive(false);
            _charaGroup.gameObject.SetActive(false);
            _joinButton.gameObject.SetActive(true);

            _cursor.ApplyRemoteSelected(false);
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

            // 席を返す。
            // 返さないと、席が空くのを待っている人がいつまでも入れない。
            ReleaseSeatIfOnline();
        }
        #endregion
    }
}