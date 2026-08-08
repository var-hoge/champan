using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace App.Network.Spike
{
    /// <summary>
    /// Phase 1 検証用のランチャー
    ///
    /// 検証したいこと
    /// 1. 2 台のビルドが Shared Mode で同じセッションにつながること
    /// 2. 権威を持つ側の移動がもう一方に同期されること
    /// 3. 1 ピアが複数のローカルプレイヤー分の権威を持てること
    ///    (「1 台にローカル 2 人 × 2 台」の実現可否)
    /// 4. Fusion の PlayerRef と席番号 (playerIdx) を分離して扱えること
    /// </summary>
    public class SpikeLauncher
        : MonoBehaviour
    {
        #region メソッド
        /// <summary>
        /// セッションに参加する (Shared Mode なので作成と参加の区別はない)
        /// </summary>
        public async Task JoinAsync()
        {
            if (_runner != null)
            {
                return;
            }

            // 呼び出し側が await しないため、例外はここで受けきってログに出す
            // (握り潰すと Unity のコンソールに何も出ずに沈黙する)
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
                    Debug.LogError($"[SpikeLauncher] StartGame に失敗しました: {result.ShutdownReason}");
                    return;
                }

                _status = $"接続成功 PlayerRef={_runner.LocalPlayer.PlayerId}";
                Debug.Log($"[SpikeLauncher] {_status}");

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
            // 検証用の暫定 UI
            GUI.skin.button.fontSize = 24;
            GUI.skin.label.fontSize = 24;

            using (new GUILayout.AreaScope(new Rect(24.0f, 24.0f, 560.0f, 400.0f)))
            {
                if (_runner == null)
                {
                    GUILayout.Label($"セッション名: {_sessionName}");
                    GUILayout.Label($"このピアのローカル人数: {_localPlayerCount}");

                    if (GUILayout.Button("参加する", GUILayout.Height(60.0f)))
                    {
                        _ = JoinAsync();
                    }

                    if (GUILayout.Button("ローカル人数を切り替える", GUILayout.Height(60.0f)))
                    {
                        _localPlayerCount = _localPlayerCount == 1 ? 2 : 1;
                    }
                }
                else
                {
                    GUILayout.Label(_status);
                    GUILayout.Label($"接続人数: {_runner.SessionInfo?.PlayerCount ?? 0}");
                    GUILayout.Label("操作: 1 人目 = WASD / 2 人目 = 方向キー");
                }
            }
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// このピアが担当するローカルプレイヤーを Spawn する
        ///
        /// Shared Mode では各ピアが自分のオブジェクトを Spawn し、そのまま権威を持つ。
        /// ここで 1 ピアが複数体を Spawn できることが「1 台にローカル 2 人」の根拠になる。
        /// </summary>
        void SpawnLocalPlayers()
        {
            for (int idx = 0; idx < _localPlayerCount; ++idx)
            {
                // 検証用の暫定的な席割り当て
                // 本実装では MasterClient が席テーブルを管理して配る
                var seatIdx = (_runner.LocalPlayer.PlayerId - 1) * 2 + idx;

                var position = new Vector3(-6.0f + seatIdx * 4.0f, 0.0f, 0.0f);

                var slot = idx == 0 ? SpikePlayer.LocalSlot.First : SpikePlayer.LocalSlot.Second;

                // Spawn 後に代入すると Spawned() が初期値のまま走ってしまうため、
                // onBeforeSpawned で Spawned() より先に状態を渡す
                var player = _runner.Spawn(
                    _playerPrefab,
                    position,
                    Quaternion.identity,
                    _runner.LocalPlayer,
                    (runner, obj) =>
                    {
                        var spawned = obj.GetComponent<SpikePlayer>();
                        spawned.SeatIdx = seatIdx;
                        spawned.Slot = slot;
                    });

                if (player == null)
                {
                    Debug.LogError($"[SpikeLauncher] Spawn に失敗しました: seatIdx={seatIdx}");
                    continue;
                }

                Debug.Log($"[SpikeLauncher] Spawn しました: seatIdx={player.SeatIdx} slot={player.Slot}");
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        SpikePlayer _playerPrefab;

        [SerializeField]
        string _sessionName = "champan-spike";

        [SerializeField]
        int _localPlayerCount = 2;

        /// <summary>
        /// 起動時に自動でセッションへ参加するか
        /// 2 台同時に立ち上げて検証するときに有効にしておくと手数が減る
        /// </summary>
        [SerializeField]
        bool _autoJoinOnStart = true;

        NetworkRunner _runner = null;
        string _status = string.Empty;
        #endregion
    }
}
