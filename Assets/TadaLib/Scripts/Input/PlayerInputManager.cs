using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using UniRx;
using UnityEngine.InputSystem;

namespace TadaLib.Input
{
    /// <summary>
    /// UnityEngine.InputSystem を使った入力制御
    /// </summary>
    [RequireComponent(typeof(UnityEngine.InputSystem.PlayerInputManager))]
    public class PlayerInputManager
        : TadaLib.ProcSystem.BaseManagerProc<PlayerInputManager>
        , TadaLib.ProcSystem.IProcManagerUpdate
    {
        #region プロパティ
        public int MaxPlayerCount { get; } = 4;
        #endregion

        #region メソッド
        public PlayerInputProxy InputProxy(int number = 0)
        {
            Assert.IsTrue(number < _playerInputProxies.Count);
            return _playerInputProxies[number];
        }

        public IEnumerable<PlayerInputProxy> InputProxies => _playerInputProxies;
        #endregion

        #region MonoBehavior の実装
        protected override void Awake()
        {
            base.Awake();

            var manager = GetComponent<UnityEngine.InputSystem.PlayerInputManager>();
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;

            for (int idx = 0; idx < MaxPlayerCount; ++idx)
            {
                string schemeMapping = _inputActionAsset.controlSchemes[idx].name;
                var input = PlayerInput.Instantiate(_keyboardPlayerInputPrefab.gameObject, idx, schemeMapping, pairWithDevices: Keyboard.current);
                input.transform.SetParent(transform);
            }

            // キーボード用オブジェクトが追加された後にコントローラの自動参加を有効にする
            manager.EnableJoining();
        }

        private void Start()
        {
#if UNITY_EDITOR
            TadaLib.Dbg.DebugTextManager.Display(this);
#endif
            // @memo: なぜか 1P の Action だけ無効になってしまったので、ここで有効化
            _playerInputs[0].actions["Action"].Enable();
        }

        public void OnUpdate()
        {
            foreach (var input in _playerInputProxies)
            {
                input.Update();
            }
        }
        #endregion

        #region privateメソッド
        void OnPlayerJoined(UnityEngine.InputSystem.PlayerInput input)
        {
            input.gameObject.transform.SetParent(transform);
            //Debug.Log($"プレイヤー {input.user.index} が入室");
            Debug.Log($"プレイヤーが入室");
            _playerInputs.Add(input);

            if (_playerInputProxies.Count < MaxPlayerCount)
            {
                _playerInputProxies.Add(new PlayerInputProxy(input));
            }
            else
            {
                // @todo: 存在していないコントローラを探す
                _playerInputProxies[(_playerInputs.Count - 1) % 4].SetPlayerInput(input);
            }
        }

        void OnPlayerLeft(UnityEngine.InputSystem.PlayerInput input)
        {
            if (_playerInputProxies.Count >= MaxPlayerCount)
            {
                // メインで使われているのが退出した
                _playerInputProxies[(_playerInputs.Count - 1) % 4].SetPlayerInput(null);
            }

            //Debug.Log($"プレイヤー {input.user.index} が退室");
            Debug.Log($"プレイヤーが退室");
            _playerInputs.Remove(input);
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("PlayerInputManager:");
            foreach (var input in _playerInputs)
            {
                if (input.user.index < MaxPlayerCount)
                {
                    //continue;
                }
                sb.AppendLine($"- Index: {input.user.index}");
                sb.AppendLine($"    Scheme: {input.currentControlScheme}");
                if (input.devices.Count >= 2)
                {
                    sb.AppendLine("    Devices:");
                    foreach (var device in input.devices)
                    {
                        sb.AppendLine($"      Name: {device.displayName}({device.name})");
                    }
                }
                else if (input.devices.Count == 0)
                {

                }
                else
                {
                    sb.AppendLine($"    Devices: {input.devices[0].displayName}({input.devices[0].name})");
                }
                var moveVec2 = input.actions["Move"].ReadValue<Vector2>();
                sb.AppendLine($"    Move/Action/Cancel: ({moveVec2.x.ToString("F2")}, {moveVec2.y.ToString("F2")})/{input.actions["Action"].IsPressed()}/{input.actions["Cancel"].IsPressed()}");
            }

            sb.AppendLine("  AllDevices:");
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                sb.AppendLine($"  - {device.displayName}({device.name})");
            }

            return sb.ToString();
        }

        #endregion

        #region privateフィールド
        [SerializeField]
        UnityEngine.InputSystem.PlayerInput _keyboardPlayerInputPrefab;
        [SerializeField]
        UnityEngine.InputSystem.InputActionAsset _inputActionAsset;

        List<UnityEngine.InputSystem.PlayerInput> _playerInputs = new();
        List<PlayerInputProxy> _playerInputProxies = new();
        #endregion
    }
}