using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using UnityEngine.InputSystem;
using System;
using Cysharp.Threading.Tasks;

namespace TadaLib.Input
{
    /// <summary>
    /// PlayerInputProxy
    /// </summary>
    public class PlayerInputProxy
    {
        #region 定義
        public enum VibrateType
        {
            Dead,
            Happy,
            VeryHappy,
        }
        #endregion

        #region コンストラクタ
        public PlayerInputProxy(UnityEngine.InputSystem.PlayerInput basePlayerInput)
        {
            _basePlayerInput = basePlayerInput;
            if (_basePlayerInput != null)
            {
                _basePlayerInput.onActionTriggered += OnSystemTrigger;
            }
        }
        #endregion

        #region プロパティ
        public Action OnAction { get; set; }
        public Action OnCancel { get; set; }
        public Action<Vector2> OnMove { get; set; }
        #endregion

        #region メソッド
        public void Update()
        {
            _axisPrev = _axis;
            _axis = new Vector2(Axis(AxisCode.Horizontal), Axis(AxisCode.Vertical));
        }

        public void SetPlayerInput(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            if (_playerInput != null)
            {
                _playerInput.onActionTriggered -= OnSystemTrigger;
            }

            _playerInput = playerInput;

            if (_playerInput != null)
            {
                _playerInput.onActionTriggered += OnSystemTrigger;
            }
        }

        public bool IsPressed(ButtonCode code)
        {
            if (_playerInput == null)
            {
                return IsPressedImpl(code, _basePlayerInput);
            }
            return IsPressedImpl(code, _playerInput);
        }

        public bool IsPressedTrigger(ButtonCode code)
        {
            if (_playerInput == null)
            {
                return IsPressedTriggerImpl(code, _basePlayerInput);
            }
            return IsPressedTriggerImpl(code, _playerInput);
        }

        public float Axis(AxisCode code)
        {
            if (_playerInput == null)
            {
                return AxisImpl(code, _basePlayerInput);
            }
            return AxisImpl(code, _playerInput);
        }

        public float AxisTrigger(AxisCode code)
        {
            var deadZone = 0.5f;

            var prev = code is AxisCode.Horizontal ? _axisPrev.x : _axisPrev.y;
            var cur = code is AxisCode.Horizontal ? _axis.x : _axis.y;

            if (Mathf.Abs(cur) < deadZone)
            {
                return 0.0f;
            }

            // 入力開始時ではない
            if (cur > deadZone && prev > deadZone)
            {
                return 0.0f;
            }
            if (cur < -deadZone && prev < -deadZone)
            {
                return 0.0f;
            }

            return cur > 0.0f ? 1.0f : -1.0f;
        }

        public void Vibrate(VibrateType type)
        {
            (var low, var high, var sec) = type switch
            {
                VibrateType.Dead => (0.1f, 0.75f, 0.12f),
                VibrateType.Happy => (0.15f, 0.35f, 0.12f),
                VibrateType.VeryHappy => (0.4f, 1.0f, 0.8f),
                _ => throw new NotImplementedException()
            };

            VibrateAdvanced(low, high, sec);
        }

        public void VibrateAdvanced(float lowFrequency, float highFrequency, float durationSec)
        {
            if (_playerInput == null)
            {
                VibrateImpl(lowFrequency, highFrequency, durationSec, _basePlayerInput);
                return;
            }
            VibrateImpl(lowFrequency, highFrequency, durationSec, _playerInput);
        }
        #endregion

        #region privateフィールド
        UnityEngine.InputSystem.PlayerInput _basePlayerInput;
        UnityEngine.InputSystem.PlayerInput _playerInput;
        Vector2 _axis;
        Vector2 _axisPrev;
        #endregion

        #region privateメソッド
        bool IsPressedImpl(ButtonCode code, UnityEngine.InputSystem.PlayerInput playerInput)
        {
            if (code is ButtonCode.Cancel)
            {
                return playerInput.actions["Cancel"].IsPressed();
            }
            return playerInput.actions["Action"].IsPressed();
        }

        bool IsPressedTriggerImpl(ButtonCode code, UnityEngine.InputSystem.PlayerInput playerInput)
        {
            if (code is ButtonCode.Cancel)
            {
                return playerInput.actions["Cancel"].WasPressedThisFrame();
            }
            return playerInput.actions["Action"].WasPressedThisFrame();
        }

        float AxisImpl(AxisCode code, UnityEngine.InputSystem.PlayerInput playerInput)
        {
            if (code is AxisCode.Vertical)
            {
                return playerInput.actions["Move"].ReadValue<Vector2>().y;
            }
            return playerInput.actions["Move"].ReadValue<Vector2>().x;
        }

        void OnSystemTrigger(InputAction.CallbackContext context)
        {
            if (context.action.name == "Action")
            {
                if (!context.performed)
                {
                    return;
                }

                OnAction?.Invoke();
                return;
            }

            if (context.action.name == "Cancel")
            {
                if (!context.performed)
                {
                    return;
                }

                OnCancel?.Invoke();
                return;
            }

            if (context.action.name == "Move")
            {
                var value = context.ReadValue<Vector2>();
                OnMove?.Invoke(value);
                return;
            }
        }

        void VibrateImpl(float lowFrequency, float highFrequency, float durationSec, UnityEngine.InputSystem.PlayerInput playerInput)
        {
            foreach (var device in playerInput.devices)
            {
                var gamepad = device as Gamepad;

                if (gamepad == null)
                {
                    continue;
                }

                Vibrate(lowFrequency, highFrequency, durationSec, gamepad).Forget();
            }
        }

        async UniTask Vibrate(float lowFrequency, float highFrequency, float durationSec, Gamepad gamepad)
        {
            gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

            await UniTask.WaitForSeconds(durationSec);

            gamepad.SetMotorSpeeds(0.0f, 0.0f);
        }
        #endregion
    }
}