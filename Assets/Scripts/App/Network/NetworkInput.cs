using Fusion;
using TadaLib.Input;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// リモートのプレイヤーの入力を再現する IInput 実装
    ///
    /// 権威を持つピアが自分の入力をスナップショットとして配り、
    /// 権威を持たないピアがそれを読んで同じ入力が入っているかのように振る舞う。
    ///
    /// 位置そのものは NetworkTransform が同期するため、この入力が必要なのは
    /// ステートマシンとアニメーションが入力を参照するため。
    ///
    /// CpuInput と同じく IInput の差し替えとして機能するので、
    /// 既存の入力を読む側のコード (InputUtil 経由) は変更不要。
    /// </summary>
    public class NetworkInput
        : NetworkBehaviour
        , TadaLib.Input.IInput
    {
        #region 型定義
        /// <summary>
        /// 1 tick 分の入力状態
        /// </summary>
        public struct Snapshot
            : INetworkStruct
        {
            public byte Buttons;
            public Vector2 Axis;
        }
        #endregion

        #region プロパティ
        /// <summary>
        /// 権威を持つ側の入力状態
        /// </summary>
        [Networked]
        public Snapshot Current { get; set; }
        #endregion

        #region TadaLib.Input.IInput の実装
        /// <summary>
        /// 権威を持つ側では自分の入力 (PlayerInputReader / CpuInput) を使わせたいので無効にする。
        /// InputUtil は ActionEnabled が false の IInput を読み飛ばす。
        /// </summary>
        public bool ActionEnabled
        {
            // 対戦シーン以外 (キャラセレクトなど) では、この入力を使わせない。
            // 選ばれてしまうと手元の入力が届かず操作できなくなる。
            get => _actionEnabled
                && !HasStateAuthority
                && NetworkSession.IsInMatchScene(gameObject);
            set => _actionEnabled = value;
        }

        public void ResetInput()
        {
            _prevButtons = 0;
        }

        public bool GetButton(ButtonCode code, float precedeSec = 0.0f)
        {
            // @memo: 先行入力はネットワーク越しには再現しない (precedeSec は無視する)
            //        押しっぱなしの判定はスナップショットだけで足りるため
            return IsButtonOn(Current.Buttons, code);
        }

        public bool GetButtonDown(ButtonCode code, float precedeSec = 0.0f)
        {
            return IsButtonOn(Current.Buttons, code) && !IsButtonOn(_prevButtons, code);
        }

        public bool GetButtonUp(ButtonCode code, float precedeSec = 0.0f)
        {
            return !IsButtonOn(Current.Buttons, code) && IsButtonOn(_prevButtons, code);
        }

        /// <summary>
        /// 入力履歴は権威側にしか存在しないため、リモート側では何もしない
        /// </summary>
        public void ForceFlagOnHistory(ButtonCode code)
        {
        }

        /// <summary>
        /// 入力履歴は権威側にしか存在しないため、リモート側では何もしない
        /// </summary>
        public void ForceFlagOffHistory(ButtonCode code)
        {
        }

        public float GetAxis(AxisCode code)
        {
            return code == AxisCode.Horizontal ? Current.Axis.x : Current.Axis.y;
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void FixedUpdateNetwork()
        {
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                return;
            }

            if (HasStateAuthority)
            {
                Current = CaptureLocalInput();
                return;
            }

            // リモート側は前 tick との差分で押した/離したを復元する
            _prevButtons = Current.Buttons;
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 同じ GameObject にある実際の入力 (人間 or CPU) を読み取る
        /// </summary>
        Snapshot CaptureLocalInput()
        {
            var source = FindLocalInput();
            if (source == null)
            {
                return default;
            }

            byte buttons = 0;
            foreach (ButtonCode code in System.Enum.GetValues(typeof(ButtonCode)))
            {
                if (source.GetButton(code, 0.0f))
                {
                    buttons |= ToFlag(code);
                }
            }

            return new Snapshot
            {
                Buttons = buttons,
                Axis = new Vector2(
                    source.GetAxis(AxisCode.Horizontal),
                    source.GetAxis(AxisCode.Vertical)),
            };
        }

        /// <summary>
        /// 自分以外の有効な IInput を探す
        /// InputUtil.TryGetInput と同じ選び方をする
        /// </summary>
        TadaLib.Input.IInput FindLocalInput()
        {
            var inputs = GetComponents<TadaLib.Input.IInput>();
            foreach (var input in inputs)
            {
                if (ReferenceEquals(input, this))
                {
                    continue;
                }

                if (!input.ActionEnabled)
                {
                    continue;
                }

                if (input is MonoBehaviour { enabled: true })
                {
                    return input;
                }
            }

            return null;
        }

        static byte ToFlag(ButtonCode code)
        {
            return (byte)(1 << (int)code);
        }

        static bool IsButtonOn(byte buttons, ButtonCode code)
        {
            return (buttons & ToFlag(code)) != 0;
        }
        #endregion

        #region private フィールド
        byte _prevButtons = 0;
        bool _actionEnabled = true;
        #endregion
    }
}
