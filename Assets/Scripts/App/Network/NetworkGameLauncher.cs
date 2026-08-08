using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦のセッションを管理する
    ///
    /// セッションはシーンをまたいで存続するため、特定のシーンには置かず
    /// DontDestroyOnLoad で持つ。
    ///
    /// 参加 (ロビー) と、対戦開始時の席・CPU の確定は分けている。
    /// 参加した瞬間に対戦シーンへ飛び込む形にすると、
    /// 参加者が揃う前に席や CPU が確定してしまい、台ごとに認識が食い違うため。
    /// </summary>
    public class NetworkGameLauncher
        : MonoBehaviour
    {
        #region プロパティ
        public static NetworkGameLauncher Instance { get; private set; }

        /// <summary>
        /// セッションに参加し、自分の席が決まっているか
        /// </summary>
        public bool IsSessionReady { get; private set; } = false;

        /// <summary>
        /// 対戦の準備 (席と CPU の確定、権威の取得) が済んでいるか
        /// </summary>
        public bool IsMatchReady { get; private set; } = false;

        /// <summary>
        /// このピアが担当するローカル人数
        /// </summary>
        public int LocalPlayerCount { get; private set; } = 1;
        #endregion

        #region メソッド
        /// <summary>
        /// ランチャーを取得する (無ければ作る)
        /// </summary>
        public static NetworkGameLauncher GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var obj = new GameObject(nameof(NetworkGameLauncher));
            DontDestroyOnLoad(obj);

            return obj.AddComponent<NetworkGameLauncher>();
        }

        /// <summary>
        /// セッションに参加する
        /// オフラインで遊ぶ場合はこれを呼ばなければよい
        /// </summary>
        public async UniTask JoinAsync(string sessionName, int localPlayerCount)
        {
            if (_runner != null)
            {
                return;
            }

            LocalPlayerCount = localPlayerCount;

            try
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = false;

                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    // ここではシーンを指定しない。
                    // 対戦シーンへは参加者が揃ってから全員一緒に移動する。
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerWithManagers>(),
                });

                if (!result.Ok)
                {
                    Debug.LogError($"[NetworkGameLauncher] 参加に失敗しました: {result.ShutdownReason}");
                    return;
                }

                Debug.Log($"[NetworkGameLauncher] 参加しました PlayerRef={_runner.LocalPlayer.PlayerId}");

                await SetUpSeatsAsync();

                IsSessionReady = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// セッションから抜ける
        /// </summary>
        public async UniTask LeaveAsync()
        {
            if (_runner == null)
            {
                return;
            }

            await _runner.Shutdown();

            IsSessionReady = false;
            IsMatchReady = false;
            _runner = null;

            Debug.Log("[NetworkGameLauncher] セッションから抜けました");
        }
        #endregion

        #region MonoBehaviour の実装
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }
        #endregion

        #region private メソッド
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene.name != NetworkSession.MatchSceneName)
            {
                return;
            }

            if (_runner == null)
            {
                return;
            }

            // 対戦シーンに入った時点で、席と CPU を確定させる
            SetUpMatchAsync().Forget();
        }

        async UniTask SetUpSeatsAsync()
        {
            // シーンのロード中に Spawn すると
            // "spawned and despawned in the same tick" となって消えてしまう
            await UniTask.WaitUntil(() => !_runner.SceneManager.IsBusy)
                .Timeout(_waitTimeout);

            if (_runner.IsSharedModeMasterClient)
            {
                var prefab = Resources.Load<NetworkSeatTable>(SeatTableResourcePath);
                if (prefab == null)
                {
                    Debug.LogError($"[NetworkGameLauncher] 席テーブルのプレハブが見つかりません: Resources/{SeatTableResourcePath}");
                    return;
                }

                _runner.Spawn(prefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
            }

            await UniTask.WaitUntil(() => NetworkSeatTable.Instance != null)
                .Timeout(_waitTimeout);

            NetworkSeatTable.Instance.RequestSeats(LocalPlayerCount);

            await UniTask.WaitUntil(() =>
            {
                for (int slot = 0; slot < LocalPlayerCount; ++slot)
                {
                    if (!NetworkSeatTable.Instance.TryGetSeatIdx(slot, out _))
                    {
                        return false;
                    }
                }

                return true;
            }).Timeout(_waitTimeout);
        }

        /// <summary>
        /// 対戦シーンでの準備
        /// </summary>
        async UniTask SetUpMatchAsync()
        {
            IsMatchReady = false;

            try
            {
                await UniTask.WaitUntil(() => NetworkSeatTable.Instance != null)
                    .Timeout(_waitTimeout);

                // Player の生成可否 (PlayerRegistorator) が CPU 判定に依存するため、
                // 権威を取りに行くより先に確定させる
                ApplyCpuSeats();

                TakeAuthorityOfLocalSeats();

                IsMatchReady = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 人間が着いていない席を CPU 席として CpuManager に反映する
        ///
        /// 席テーブルは全員に複製されているため、どのピアでも同じ結果になる。
        /// CPU を出すかどうかはルール設定 (GameMatchManager.IsExistCpu) に従う。
        /// </summary>
        void ApplyCpuSeats()
        {
            var cpuManager = Cpu.CpuManager.Instance;
            if (cpuManager == null)
            {
                Debug.LogError("[NetworkGameLauncher] CpuManager が見つかりません");
                return;
            }

            var isExistCpu = GameMatchManager.Instance.IsExistCpu;

            for (int seatIdx = 0; seatIdx < NetworkSeatTable.SeatCountMax; ++seatIdx)
            {
                var isCpuSeat = isExistCpu && NetworkSeatTable.Instance.IsCpuSeat(seatIdx);
                cpuManager.SetIsCpu(seatIdx, isCpuSeat);
            }

            Debug.Log($"[NetworkGameLauncher] CPU 席を反映しました (CPU 有無: {isExistCpu}, CPU 数: {cpuManager.CpuCount()})");
        }

        /// <summary>
        /// 自分の席に対応するシーン上の Player の権威を取りに行く
        /// CPU 席は MasterClient が権威を持ったままにする
        /// </summary>
        void TakeAuthorityOfLocalSeats()
        {
            var binders = FindObjectsByType<NetworkPlayerBinder>(FindObjectsSortMode.None);

            for (int slot = 0; slot < LocalPlayerCount; ++slot)
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
        /// <summary>
        /// ランチャーは実行時に生成されるため、参照は Resources から取る
        /// </summary>
        const string SeatTableResourcePath = "Network/NetworkSeatTable";

        static readonly System.TimeSpan _waitTimeout = System.TimeSpan.FromSeconds(15.0);

        NetworkRunner _runner = null;
        #endregion
    }
}
