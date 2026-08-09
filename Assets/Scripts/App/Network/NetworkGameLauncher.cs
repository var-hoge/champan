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

            // ネットワーク用プレハブへの参照を持たせるため、プレハブから生成する。
            // Resources から NetworkObject を直接読むと、Fusion が同期生成に失敗することがある
            // (NetworkObjectSpawnException: Failed to load prefab synchronously)
            var prefab = Resources.Load<NetworkGameLauncher>(LauncherResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[NetworkGameLauncher] プレハブが見つかりません: Resources/{LauncherResourcePath}");
                return null;
            }

            var instance = Instantiate(prefab);
            instance.name = nameof(NetworkGameLauncher);
            DontDestroyOnLoad(instance.gameObject);

            return instance;
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

                PreloadNetworkPrefabs();

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
            if (_runner == null)
            {
                return;
            }

            // シーンを切り替えると未使用アセットが破棄され、
            // プレハブのロード状態が失われて同期生成に失敗する。
            // (バブルの生成が例外になり、バブルも王冠も出なくなっていた)
            PreloadNetworkPrefabs();

            // キャラセレクトにも Player が置かれており、そこでも相手の動きを見せたい。
            // Player がいるシーンなら対戦シーンと同じように席と権威を設定する。
            SetUpSceneAsync().Forget();
        }

        async UniTask SetUpSeatsAsync()
        {
            // シーンのロード中に Spawn すると
            // "spawned and despawned in the same tick" となって消えてしまう
            await UniTask.WaitUntil(() => !_runner.SceneManager.IsBusy)
                .Timeout(_waitTimeout);

            if (_runner.IsSharedModeMasterClient)
            {
                // 実行時に Spawn したオブジェクトはシーン切り替えで破棄されるため、
                // タイトルからキャラセレクト、対戦まで持ち越す
                SpawnSharedState(_seatTablePrefab);
                SpawnSharedState(_charaSelectStatePrefab);
                SpawnSharedState(_flowStatePrefab);
                SpawnSharedState(_matchStatePrefab);
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
        /// Player が置かれているシーンでの準備
        /// </summary>
        async UniTask SetUpSceneAsync()
        {
            IsMatchReady = false;

            try
            {
                // シーンのロードが完全に終わるまでは Spawn できない。
                // 準備完了の判定にこれを含めないと、
                // これを待って生成しようとしている側が失敗する。
                await UniTask.WaitUntil(() => !_runner.SceneManager.IsBusy)
                    .Timeout(_waitTimeout);

                await UniTask.WaitUntil(() => NetworkSeatTable.Instance != null)
                    .Timeout(_waitTimeout);

                // シーン上の Player が Spawn されるまで待つ
                // キャラセレクトの Player は非アクティブで置かれているため、
                // 非アクティブも探索対象に含める必要がある
                await UniTask.WaitUntil(() => FindObjectsByType<NetworkPlayerBinder>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None).Length > 0)
                    .Timeout(_waitTimeout);

                // CPU 席の確定は対戦シーンに入ってから行う。
                // キャラセレクトで確定させると「席が埋まっている = 人間」と判定され、
                // ボタンを押す前に自動でエントリーされてしまう
                // (ローカル対戦ではボタンを押して参加する)。
                if (SceneManager.GetActiveScene().name == NetworkSession.MatchSceneName)
                {
                    // Player の生成可否 (PlayerRegistorator) が CPU 判定に依存する
                    ApplyCpuSeats();
                }

                // 権威の要求は各 Player が自分で行う (NetworkPlayerBinder)
                IsMatchReady = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// ネットワーク用プレハブを事前に読み込む
        ///
        /// Fusion は未ロードのプレハブを同期生成しようとすると例外を投げる
        /// (NetworkObjectSpawnException: Failed to load prefab synchronously)。
        /// バブルなど対戦中に生成するものがこれで失敗していた。
        ///
        /// 生成を非同期にする設定もあるが、その場合は生成直後の戻り値が null になり、
        /// 生成したオブジェクトをその場で使っている既存コードが壊れるため採らない。
        /// </summary>
        void PreloadNetworkPrefabs()
        {
            var table = _runner.Prefabs;
            var loadedCount = 0;
            var failedCount = 0;

            foreach (var (prefabId, _) in table.GetEntries())
            {
                if (table.Load(prefabId, isSynchronous: true) != null)
                {
                    ++loadedCount;
                }
                else
                {
                    ++failedCount;
                }
            }

            if (failedCount > 0)
            {
                Debug.LogError($"[NetworkGameLauncher] プレハブの読み込みに失敗しました: {failedCount} 件");
                return;
            }

            Debug.Log($"[NetworkGameLauncher] プレハブを読み込みました: {loadedCount} 件");
        }

        /// <summary>
        /// 全員で共有する状態オブジェクトを生成する
        /// </summary>
        void SpawnSharedState(NetworkBehaviour prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[NetworkGameLauncher] 共有状態のプレハブが設定されていません");
                return;
            }

            // プレハブ資産では NetworkBehaviour.Object が null のため、コンポーネントから取る
            var networkObject = prefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError($"[NetworkGameLauncher] NetworkObject がありません: {prefab.name}");
                return;
            }

            var spawned = NetworkSession.Spawn(
                networkObject.transform, Vector3.zero, Quaternion.identity);

            if (spawned == null)
            {
                Debug.LogError($"[NetworkGameLauncher] 生成に失敗しました: {prefab.name}");
                return;
            }

            KeepAcrossScenes(spawned.gameObject);
        }

        /// <summary>
        /// シーンが切り替わっても破棄されないようにする
        /// </summary>
        void KeepAcrossScenes(GameObject obj)
        {
            if (_runner.SceneManager is NetworkSceneManagerDefault sceneManager)
            {
                sceneManager.MakeDontDestroyOnLoad(obj);
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

        #endregion

        #region private フィールド
        [SerializeField]
        NetworkSeatTable _seatTablePrefab;

        [SerializeField]
        NetworkCharaSelectState _charaSelectStatePrefab;

        [SerializeField]
        NetworkFlowState _flowStatePrefab;

        [SerializeField]
        NetworkMatchState _matchStatePrefab;

        /// <summary>
        /// ランチャー自身は実行時に生成するため、これだけ Resources から取る
        /// </summary>
        const string LauncherResourcePath = "Network/NetworkGameLauncher";

        static readonly System.TimeSpan _waitTimeout = System.TimeSpan.FromSeconds(15.0);

        NetworkRunner _runner = null;
        #endregion
    }
}
