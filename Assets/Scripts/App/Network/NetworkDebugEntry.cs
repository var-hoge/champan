#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦の入口 (仮)
    ///
    /// 正式にはタイトル画面のメニューから入る想定だが、
    /// 通しの流れを確認するための暫定的な入口。
    /// エディタと開発ビルドでのみ動く。
    ///
    /// 自動では参加しない。必ずボタンを押した時だけ参加する。
    /// </summary>
    public class NetworkDebugEntry
        : MonoBehaviour
    {
        #region MonoBehaviour の実装
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var obj = new GameObject(nameof(NetworkDebugEntry));
            DontDestroyOnLoad(obj);
            obj.AddComponent<NetworkDebugEntry>();
        }

        void OnGUI()
        {
            if (!_isOpen)
            {
                if (GUI.Button(new Rect(8.0f, 8.0f, 140.0f, 32.0f), "ネット対戦(仮)"))
                {
                    _isOpen = true;
                }

                return;
            }

            using (new GUILayout.AreaScope(new Rect(8.0f, 8.0f, 380.0f, 260.0f), string.Empty, GUI.skin.box))
            {
                GUILayout.Label("ネットワーク対戦 (仮の入口)");

                var launcher = NetworkGameLauncher.Instance;

                if (launcher == null || !launcher.IsSessionReady)
                {
                    GUILayout.Label("ルーム名");
                    _sessionName = GUILayout.TextField(_sessionName);

                    GUILayout.Label($"この台の人数: {_localPlayerCount}");
                    if (GUILayout.Button("人数を切り替える"))
                    {
                        _localPlayerCount = _localPlayerCount % 4 + 1;
                    }

                    if (GUILayout.Button("参加する", GUILayout.Height(32.0f)))
                    {
                        NetworkGameLauncher.GetOrCreate()
                            .JoinAsync(_sessionName, _localPlayerCount)
                            .Forget();
                    }
                }
                else
                {
                    var runner = NetworkSession.Runner;

                    GUILayout.Label($"参加中: {_sessionName}");
                    GUILayout.Label($"自分: PlayerRef={runner?.LocalPlayer.PlayerId} ホスト={NetworkSession.HasAuthority}");
                    GUILayout.Label($"接続人数: {runner?.SessionInfo?.PlayerCount ?? 0}");

                    if (NetworkSeatTable.Instance != null)
                    {
                        GUILayout.Label($"使用中の席: {NetworkSeatTable.Instance.OccupiedSeatCount}");
                    }

                    GUILayout.Label(
                        NetworkSession.HasAuthority
                            ? "画面を進めるのはホストの操作です"
                            : "画面の進行はホストに追従します");

                    if (GUILayout.Button("抜ける"))
                    {
                        launcher.LeaveAsync().Forget();
                    }
                }

                if (GUILayout.Button("閉じる"))
                {
                    _isOpen = false;
                }
            }
        }
        #endregion

        #region private フィールド
        bool _isOpen = false;
        string _sessionName = "champan";
        int _localPlayerCount = 2;
        #endregion
    }
}
#endif
