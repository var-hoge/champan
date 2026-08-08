using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Network.Spike
{
    /// <summary>
    /// Phase 1 検証用のプレイヤー
    /// 既存のゲームロジックには一切依存しない
    /// </summary>
    public class SpikePlayer
        : NetworkBehaviour
    {
        #region 型定義
        /// <summary>
        /// ローカルでの操作割り当て
        /// 1 台に複数人が座るケースを検証するために用意している
        /// </summary>
        public enum LocalSlot
        {
            /// <summary>
            /// WASD
            /// </summary>
            First,

            /// <summary>
            /// 方向キー
            /// </summary>
            Second,
        }
        #endregion

        #region プロパティ
        /// <summary>
        /// 席番号
        /// 本実装では MasterClient が割り当てるが、検証段階では Spawn 時に決め打ちする
        /// </summary>
        [Networked]
        public int SeatIdx { get; set; }

        /// <summary>
        /// このオブジェクトを操作するローカル枠
        /// 権威を持つピアでのみ意味を持つ
        /// </summary>
        [Networked]
        public LocalSlot Slot { get; set; }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            name = $"SpikePlayer_Seat{SeatIdx}_{(HasStateAuthority ? "Local" : "Remote")}";

            if (_body != null)
            {
                _body.color = _seatColors[SeatIdx % _seatColors.Length];
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 権威を持つオブジェクトだけが自分でシミュレートする
            // 権威を持たないオブジェクトは NetworkTransform の補間に任せる
            if (!HasStateAuthority)
            {
                return;
            }

            var axis = ReadLocalAxis();
            transform.position += new Vector3(axis.x, axis.y, 0.0f) * _speed * Runner.DeltaTime;
        }
        #endregion

        #region private メソッド
        Vector2 ReadLocalAxis()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var axis = Vector2.zero;

            if (Slot == LocalSlot.First)
            {
                if (keyboard.aKey.isPressed)
                {
                    axis.x -= 1.0f;
                }
                if (keyboard.dKey.isPressed)
                {
                    axis.x += 1.0f;
                }
                if (keyboard.sKey.isPressed)
                {
                    axis.y -= 1.0f;
                }
                if (keyboard.wKey.isPressed)
                {
                    axis.y += 1.0f;
                }
            }
            else
            {
                if (keyboard.leftArrowKey.isPressed)
                {
                    axis.x -= 1.0f;
                }
                if (keyboard.rightArrowKey.isPressed)
                {
                    axis.x += 1.0f;
                }
                if (keyboard.downArrowKey.isPressed)
                {
                    axis.y -= 1.0f;
                }
                if (keyboard.upArrowKey.isPressed)
                {
                    axis.y += 1.0f;
                }
            }

            return axis.sqrMagnitude > 1.0f ? axis.normalized : axis;
        }
        #endregion

        #region private フィールド
        [SerializeField]
        SpriteRenderer _body;

        [SerializeField]
        float _speed = 8.0f;

        static readonly Color[] _seatColors =
        {
            new Color(1.0f, 0.35f, 0.35f),
            new Color(0.35f, 0.6f, 1.0f),
            new Color(0.4f, 0.85f, 0.4f),
            new Color(1.0f, 0.85f, 0.35f),
        };
        #endregion
    }
}
