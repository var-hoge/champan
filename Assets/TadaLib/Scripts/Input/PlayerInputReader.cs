using System.Collections;
using System.Collections.Generic;
using App.Cpu;
using UnityEngine;

namespace TadaLib.Input
{
    /// <summary>
    /// Component処理
    /// </summary>
    public class PlayerInputReader
        : ProcSystem.BaseProc
        , ProcSystem.IProcUpdate
        , IInput
    {
        #region プロパティ
        public bool ActionEnabled { get; set; } = true;
        #endregion

        #region メソッド
        #endregion

        #region TadaLib.Input.IInputの実装
        // 入力状態をリセットする
        public void ResetInput()
        {
            // Start より先に呼ばれることがあり、その時点では中身が無い
            foreach (ButtonCode code in System.Enum.GetValues(typeof(ButtonCode)))
            {
                if (!_buttonDict.TryGetValue(code, out var buff))
                {
                    continue;
                }

                buff.Clear();
                buff.AddFirst(new ButtonData(false, Time.unscaledTime));
            }

            foreach (AxisCode code in System.Enum.GetValues(typeof(AxisCode)))
            {
                if (_axisDict.ContainsKey(code))
                {
                    _axisDict[code] = 0.0f;
                }
            }
        }

        /// <summary>
        /// 指定したボタンが入力されたかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <returns></returns>
        public bool GetButtonDown(ButtonCode code, float precedeSec = 0.0f)
        {
            // 画面によっては割り当てられていないボタンがある
            if (!_buttonDict.TryGetValue(code, out var buff))
            {
                return false;
            }

            // 先行入力を考慮する
            var prev = buff.First.Value.IsPushed;
            foreach (var data in buff)
            {
                if (Time.unscaledTime - data.InputTime > precedeSec)
                {
                    if (prev && !data.IsPushed)
                    {
                        return true;
                    }
                    break;
                }

                if (prev && !data.IsPushed)
                {
                    return true;
                }

                prev = data.IsPushed;
            }

            return false;
        }

        /// <summary>
        /// 指定したボタンが入力されているかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <returns></returns>
        public bool GetButton(ButtonCode code, float precedeSec = 0.0f)
        {
            // 画面によっては割り当てられていないボタンがある
            if (!_buttonDict.TryGetValue(code, out var buff))
            {
                return false;
            }

            // 先行入力を考慮する

            if (buff.First.Value.IsPushed)
            {
                return true;
            }

            foreach (var data in buff)
            {
                if (Time.unscaledTime - data.InputTime > precedeSec)
                {
                    break;
                }

                if (data.IsPushed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定したボタンの入力が離されたかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <returns></returns>
        public bool GetButtonUp(ButtonCode code, float precedeSec = 0.0f)
        {
            // 画面によっては割り当てられていないボタンがある
            if (!_buttonDict.TryGetValue(code, out var buff))
            {
                return false;
            }

            // 先行入力を考慮する
            var prev = buff.First.Value.IsPushed;
            foreach (var data in buff)
            {
                if (Time.unscaledTime - data.InputTime > precedeSec)
                {
                    if (!prev && data.IsPushed)
                    {
                        return true;
                    }
                    break;
                }

                if (!prev && data.IsPushed)
                {
                    return true;
                }

                prev = data.IsPushed;
            }

            return false;
        }

        /// <summary>
        /// 過去の入力フラグを全て立てる
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOnHistory(ButtonCode code)
        {
            var buff = _buttonDict[code];

            foreach (var node in buff)
            {
                node.ForceSetIsPushed(true);
            }
        }
        /// <summary>
        /// 過去の入力フラグを全て降ろす
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOffHistory(ButtonCode code)
        {
            var buff = _buttonDict[code];
            foreach (var node in buff)
            {
                node.ForceSetIsPushed(false);
            }
        }

        /// <summary>
        /// 指定したボタンが入力されているかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <returns></returns>
        public float GetAxis(AxisCode code)
        {
            if (_axisDict == null || _axisDict.Count == 0)
            {
                // エラー対策
                return 0.0f;
            }
            return _axisDict[code];
        }
        #endregion

        #region メソッド
        /// <summary>
        /// この Player を操作するローカルのコントローラ番号を設定する
        ///
        /// 席番号 (playerIdx) とは別物。
        /// ネットワーク対戦では「2 台目の席 2」を「その台のローカル 1 人目」が操作するため、
        /// 席番号をそのままコントローラ番号として使うと、
        /// どの台も同じコントローラを見てしまう。
        /// </summary>
        /// <summary>
        /// 操作元が決まっているか (調査用)
        /// </summary>
        public bool HasInputProxy => _playerInputProxy != null;

        /// <summary>
        /// 割り当てられたローカルのコントローラ番号 (調査用)
        /// </summary>
        public int LocalInputIdx => _localInputIdx;

        public void SetLocalInputIdx(int localInputIdx)
        {
            _localInputIdx = localInputIdx;
            _isInputResolved = true;
            _playerInputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(localInputIdx);
        }

        /// <summary>
        /// 操作元を決め直す
        /// 席が確定した後に呼ぶ
        /// </summary>
        public void ResetInputResolve()
        {
            _isInputResolved = false;
            _localInputIdx = -1;
            _playerInputProxy = null;
        }

        /// <summary>
        /// 操作元を解決する
        ///
        /// ネットワーク対戦では席番号とローカルのコントローラ番号が一致しないため、
        /// SeatInput を通して変換する。他の台が担当する席なら操作元は無い。
        /// 席テーブルは後から届くので、決まるまで毎フレーム試す。
        /// </summary>
        void ResolveInputProxy()
        {
            if (_isInputResolved)
            {
                return;
            }

            var playerIdx = GetComponent<App.Actor.Player.DataHolder>().PlayerIdx;

            if (App.Network.NetworkSession.IsOnline && App.Network.NetworkSeatTable.Instance == null)
            {
                // まだ席が配られていない
                return;
            }

            _isInputResolved = true;
            _playerInputProxy = App.Network.SeatInput.GetProxyOrNull(playerIdx);
            _localInputIdx = App.Network.SeatInput.GetLocalInputIdx(playerIdx);

            Debug.Log(
                $"[入力調査] seatIdx={playerIdx}"
                + $" 操作元={(_playerInputProxy != null ? "あり" : "無し")}"
                + $" ローカル番号={_localInputIdx}");
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            var playerIdx = GetComponent<App.Actor.Player.DataHolder>().PlayerIdx;

            // 操作元は OnUpdate の ResolveInputProxy で決める
            // (席テーブルはセッション参加後に届くため、ここでは決まらない)

            // 初期化
            foreach (ButtonCode code in System.Enum.GetValues(typeof(ButtonCode)))
            {
                _buttonDict.Add(code, new LinkedList<ButtonData>());
                _buttonDict[code].AddFirst(new ButtonData(false, Time.unscaledTime));
            }

            foreach (AxisCode code in System.Enum.GetValues(typeof(AxisCode)))
            {
                _axisDict.Add(code, 0.0f);
            }


            ActionEnabled = !CpuManager.Instance.IsCpu(playerIdx);
        }
        #endregion

        #region TadaLib.ProcSystem.IProcUpdate の実装
        /// <summary>
        /// 移動前の更新処理
        /// </summary>
        public void OnUpdate()
        {
            ResolveInputProxy();

            // CPU かどうかは席の確定後に決まるため、一度だけ読むと取り違える。
            // ネットワーク対戦では毎フレーム見て追従する。
            if (App.Network.NetworkSession.IsOnline)
            {
                var playerIdx = GetComponent<App.Actor.Player.DataHolder>().PlayerIdx;
                ActionEnabled = !CpuManager.Instance.IsCpu(playerIdx);
            }

            if (!ActionEnabled || _playerInputProxy == null)
            {
                ResetInput();

                return;
            }

            foreach (ButtonCode code in System.Enum.GetValues(typeof(ButtonCode)))
            {
                var list = _buttonDict[code];

                list.AddFirst(new ButtonData(_playerInputProxy.IsPressed(code), Time.unscaledTime));

                while (list.Count >= 2)
                {
                    var duration = Time.unscaledTime - list.Last.Value.InputTime;
                    if (duration < MaxBuffSec)
                    {
                        break;
                    }
                    list.RemoveLast();
                }
            }


            _axisDict[AxisCode.Horizontal] = AdjustAxis(_playerInputProxy.Axis(AxisCode.Horizontal));
            _axisDict[AxisCode.Vertical] = AdjustAxis(_playerInputProxy.Axis(AxisCode.Vertical));
        }
        #endregion

        #region privateメソッド
        float AdjustAxis(float rawAxis)
        {
            var deadZone = 0.1f;
            if (Mathf.Abs(rawAxis) <= deadZone)
            {
                return 0.0f;
            }

            // [deadZone, 1.0] → [0.0, 1.0] に補正
            var ret = Mathf.Sign(rawAxis) * (Mathf.Abs(rawAxis) - deadZone) / (1.0f - deadZone);

            // 斜め上にスティックを倒しても(1, 1)にできるよう補正
            ret = Mathf.Clamp(ret * _root2, -1.0f, 1.0f);

            return ret;
        }
        #endregion

        #region privateフィールド
        class ButtonData
        {
            public bool IsPushed { get; private set; }
            public float InputTime { get; }

            public ButtonData(bool isPushed, float inputTime)
            {
                IsPushed = isPushed;
                InputTime = inputTime;
            }

            public void ForceSetIsPushed(bool isPushed)
            {
                IsPushed = isPushed;
            }
        }

        const float MaxBuffSec = 0.5f;
        TadaLib.Input.PlayerInputProxy _playerInputProxy = null;

        /// <summary>
        /// この Player を操作するローカルのコントローラ番号
        /// 未設定なら -1
        /// </summary>
        int _localInputIdx = -1;
        bool _isInputResolved = false;
        Dictionary<ButtonCode, LinkedList<ButtonData>> _buttonDict = new Dictionary<ButtonCode, LinkedList<ButtonData>>();
        Dictionary<AxisCode, float> _axisDict = new Dictionary<AxisCode, float>();

        static readonly float _root2 = Mathf.Sqrt(2.0f);
        #endregion
    }
}