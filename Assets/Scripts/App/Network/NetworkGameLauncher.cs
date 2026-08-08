using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// Main シーンでネットワーク対戦のセッションを開始する
    ///
    /// Main シーンはローカル対戦と共通のものを使うため、Player はシーンに直接置かれている。
    /// そのため実行時に生成するのではなく、席が決まったピアが
    /// 対応する Player の権威を取りに行く形になる。
    ///
    /// Fusion にシーン上の NetworkObject を管理させるには、
    /// StartGameArgs.Scene を指定して Fusion のシーンマネージャ経由でロードする必要がある。
    /// </summary>
    public class NetworkGameLauncher
        : MonoBehaviour
    {
        #region プロパティ
        public static NetworkGameLauncher Instance { get; private set; }

        /// <summary>
        /// セッションへの参加が完了しているか
        /// </summary>
        public bool IsReady { get; private set; } = false;
        #endregion

        #region メソッド
        /// <summary>
        /// ネットワーク対戦を開始する
        /// オフラインで遊ぶ場合はこれを呼ばなければよい
        /// </summary>
        public async UniTask JoinAsync(string sessionName, int localPlayerCount)
        {
            if (_runner != null)
            {
                return;
            }

            try
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = false;

                var sceneRef = SceneRef.FromIndex(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    // シーン上の NetworkObject を Fusion に管理させるために指定する
                    Scene = sceneRef,
                    // Fusion は Single モードでロードするため、
                    // そのままだとマネージャシーンが落ちてしまう
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerWithManagers>(),
                });

                if (!result.Ok)
                {
                    Debug.LogError($"[NetworkGameLauncher] StartGame に失敗しました: {result.ShutdownReason}");
                    return;
                }

                Debug.Log($"[NetworkGameLauncher] 接続しました PlayerRef={_runner.LocalPlayer.PlayerId}");

                await SetUpSeatsAsync(localPlayerCount);

                TakeAuthorityOfLocalSeats(localPlayerCount);

                IsReady = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        #region MonoBehaviour の実装
        void Awake()
        {
            // Fusion はシーンを読み直すため、既に動いているランチャーがある状態で
            // シーン側の新しいランチャーが現れる。
            // 二重にセッションを開始しないよう、後から現れた方を消す。
            if (Instance != null && Instance != this)
            {
                Debug.Log("[NetworkGameLauncher] 既に動作中のため、このインスタンスを破棄します");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            // Awake で破棄された側はここに来ない想定だが、念のため
            if (Instance != this)
            {
                return;
            }

            // 検証用の入口
            // 通常は Title からの流れで JoinAsync を呼ぶため、既定では何もしない
            if (_autoJoinOnStart)
            {
                JoinAsync(_debugSessionName, _debugLocalPlayerCount).Forget();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region private メソッド
        async UniTask SetUpSeatsAsync(int localPlayerCount)
        {
            // シーンのロードが終わる前に Spawn すると、
            // "spawned and despawned in the same tick" となって消えてしまう
            await UniTask.WaitUntil(() => !_runner.SceneManager.IsBusy)
                .Timeout(_seatWaitTimeout);

            if (_runner.IsSharedModeMasterClient)
            {
                _runner.Spawn(_seatTablePrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
            }

            await UniTask.WaitUntil(() => NetworkSeatTable.Instance != null)
                .Timeout(_seatWaitTimeout);

            NetworkSeatTable.Instance.RequestSeats(localPlayerCount);

            await UniTask.WaitUntil(() =>
            {
                for (int slot = 0; slot < localPlayerCount; ++slot)
                {
                    if (!NetworkSeatTable.Instance.TryGetSeatIdx(slot, out _))
                    {
                        return false;
                    }
                }

                return true;
            }).Timeout(_seatWaitTimeout);
        }

        /// <summary>
        /// 自分の席に対応するシーン上の Player の権威を取りに行く
        /// </summary>
        void TakeAuthorityOfLocalSeats(int localPlayerCount)
        {
            var binders = FindObjectsByType<NetworkPlayerBinder>(FindObjectsSortMode.None);

            for (int slot = 0; slot < localPlayerCount; ++slot)
            {
                if (!NetworkSeatTable.Instance.TryGetSeatIdx(slot, out var seatIdx))
                {
                    Debug.LogError($"[NetworkGameLauncher] 席が割り当てられていません: localSlot={slot}");
                    continue;
                }

                var target = System.Array.Find(binders, binder => binder.SeatIdx == seatIdx);
                if (target == null)
                {
                    Debug.LogError($"[NetworkGameLauncher] 席に対応する Player が見つかりません: seatIdx={seatIdx}");
                    continue;
                }

                // 権威の移動は非同期に完了するため、反映は StateAuthorityChanged 側で行う
                target.Object.RequestStateAuthority();

                Debug.Log($"[NetworkGameLauncher] 権威を要求しました: seatIdx={seatIdx}");
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        NetworkSeatTable _seatTablePrefab;

        /// <summary>
        /// 起動時に自動でセッションへ参加するか (検証用)
        /// オフラインのローカル対戦に影響しないよう、既定では false
        /// </summary>
        [SerializeField]
        bool _autoJoinOnStart = false;

        [SerializeField]
        string _debugSessionName = "champan-main";

        [SerializeField]
        int _debugLocalPlayerCount = 2;

        static readonly System.TimeSpan _seatWaitTimeout = System.TimeSpan.FromSeconds(10.0);

        NetworkRunner _runner = null;
        #endregion
    }
}
