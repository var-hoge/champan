using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace App.Network.Spike
{
    /// <summary>
    /// 実際の Player (PlayerNetwork.prefab) をネットワーク越しに動かす検証用ランチャー
    ///
    /// SpikeLauncher が四角で仕組みを確かめるのに対し、こちらは本物の Player を使って
    /// MoveCtrl / StateMachine / アニメーションがリモートで破綻しないかを見る。
    /// </summary>
    public class NetworkPlayerTestLauncher
        : MonoBehaviour
    {
        #region メソッド
        public async Task JoinAsync()
        {
            if (_runner != null)
            {
                return;
            }

            try
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = false;

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = _sessionName,
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                });

                if (!result.Ok)
                {
                    _status = $"接続失敗: {result.ShutdownReason}";
                    Debug.LogError($"[NetworkPlayerTest] StartGame に失敗しました: {result.ShutdownReason}");
                    return;
                }

                _status = $"接続成功 PlayerRef={_runner.LocalPlayer.PlayerId}";
                Debug.Log($"[NetworkPlayerTest] {_status}");

                await SetUpSeatsAsync();

                SpawnLocalPlayers();
            }
            catch (System.Exception e)
            {
                _status = $"例外: {e.Message}";
                Debug.LogException(e);
            }
        }
        #endregion

        #region MonoBehaviour の実装
        void Start()
        {
            if (_autoJoinOnStart)
            {
                _ = JoinAsync();
            }
        }

        void OnGUI()
        {
            GUI.skin.label.fontSize = 24;

            using (new GUILayout.AreaScope(new Rect(24.0f, 24.0f, 640.0f, 300.0f)))
            {
                GUILayout.Label(_runner == null ? "接続中..." : _status);

                if (NetworkSeatTable.Instance != null)
                {
                    GUILayout.Label($"使用中の席: {NetworkSeatTable.Instance.OccupiedSeatCount}");
                }
            }
        }
        #endregion

        #region private メソッド
        async UniTask SetUpSeatsAsync()
        {
            if (_runner.IsSharedModeMasterClient)
            {
                _runner.Spawn(_seatTablePrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
            }

            await UniTask.WaitUntil(() => NetworkSeatTable.Instance != null)
                .Timeout(_seatWaitTimeout);

            // 検証用のため、部屋に入ると同時に席も取る
            NetworkSeatTable.Instance.RequestJoinRoom(_localPlayerCount, "");
            for (int slot = 0; slot < _localPlayerCount; ++slot)
            {
                NetworkSeatTable.Instance.RequestSeat(slot, "");
            }

            await UniTask.WaitUntil(() =>
            {
                for (int slot = 0; slot < _localPlayerCount; ++slot)
                {
                    if (!NetworkSeatTable.Instance.TryGetSeatIdx(slot, out _))
                    {
                        return false;
                    }
                }

                return true;
            }).Timeout(_seatWaitTimeout);
        }

        void SpawnLocalPlayers()
        {
            for (int slot = 0; slot < _localPlayerCount; ++slot)
            {
                if (!NetworkSeatTable.Instance.TryGetSeatIdx(slot, out var seatIdx))
                {
                    Debug.LogError($"[NetworkPlayerTest] 席が割り当てられていません: localSlot={slot}");
                    continue;
                }

                var position = new Vector3(-6.0f + seatIdx * 4.0f, 2.0f, 0.0f);

                // 席番号は Spawned() より前に渡す必要がある
                var player = _runner.Spawn(
                    _playerPrefab,
                    position,
                    Quaternion.identity,
                    _runner.LocalPlayer,
                    (runner, obj) =>
                    {
                        var binder = obj.GetComponent<NetworkPlayerBinder>();
                        binder.SeatIdxOverride = seatIdx;
                        binder.UseSeatOverride = true;
                    });

                if (player == null)
                {
                    Debug.LogError($"[NetworkPlayerTest] Spawn に失敗しました: seatIdx={seatIdx}");
                    continue;
                }

                // この検証シーンには Main シーン固有のマネージャ (CpuViewDataManager) が無く、
                // CpuInput.OnPostMove が ActionEnabled に関わらず走って NullReference になるため切る。
                // CPU との共存自体は Phase 7 で扱う。
                var cpuInput = player.GetComponent<Cpu.CpuInput>();
                if (cpuInput != null)
                {
                    cpuInput.enabled = false;
                }

                Debug.Log($"[NetworkPlayerTest] Player を Spawn しました: seatIdx={seatIdx}");
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        NetworkPlayerBinder _playerPrefab;

        [SerializeField]
        NetworkSeatTable _seatTablePrefab;

        [SerializeField]
        string _sessionName = "champan-player-test";

        [SerializeField]
        int _localPlayerCount = 2;

        [SerializeField]
        bool _autoJoinOnStart = true;

        static readonly System.TimeSpan _seatWaitTimeout = System.TimeSpan.FromSeconds(10.0);

        NetworkRunner _runner = null;
        string _status = string.Empty;
        #endregion
    }
}
