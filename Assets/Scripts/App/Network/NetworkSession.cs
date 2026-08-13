using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦かどうかの違いを吸収する層
    ///
    /// Main シーンはローカル対戦とネットワーク対戦で共通のものを使うため、
    /// シーンやプレハブを分けるのではなく、生成処理をここに通して実行時に分岐する。
    ///
    /// オフライン時は今まで通り Instantiate するだけなので、
    /// 既存のローカル対戦・CPU 対戦の挙動は変わらない。
    /// </summary>
    public static class NetworkSession
    {
        #region プロパティ
        /// <summary>
        /// ネットワーク対戦中かどうか
        /// </summary>
        public static bool IsOnline => Runner != null;

        /// <summary>
        /// Fusion がシーンを読み込んだことがあるか
        ///
        /// Fusion が読み込んだシーンは、いつものシーン管理の記録に載らない。
        /// その状態でいつもの手順で読み直すと、記録に無いものを外そうとして落ちる。
        ///
        /// 部屋を抜けた後もシーンはそのまま残るため、一度立ったら戻さない。
        /// </summary>
        public static bool HasFusionLoadedScene { get; set; } = false;

        /// <summary>
        /// ギミックの生成や勝敗判定を行ってよいかどうか
        ///
        /// オフラインでは常に true。
        /// オンラインでは MasterClient だけが true になる。
        /// </summary>
        public static bool HasAuthority => !IsOnline || Runner.IsSharedModeMasterClient;

        /// <summary>
        /// 実行中の NetworkRunner
        /// オフラインなら null
        /// </summary>
        public static NetworkRunner Runner
        {
            get
            {
                if (_cachedRunner != null && _cachedRunner.IsRunning)
                {
                    return _cachedRunner;
                }

                _cachedRunner = null;

                foreach (var runner in NetworkRunner.Instances)
                {
                    if (runner != null && runner.IsRunning)
                    {
                        _cachedRunner = runner;
                        break;
                    }
                }

                return _cachedRunner;
            }
        }
        #endregion

        #region メソッド
        /// <summary>
        /// 対戦シーンに属するオブジェクトかどうか
        ///
        /// Player プレハブはキャラセレクトなど対戦以外の画面にも置かれている。
        /// そこではネットワークの権威に関係なくローカルに動く必要があるため、
        /// 権威による制御は対戦シーンに限る。
        /// </summary>
        public static bool IsInMatchScene(GameObject obj)
        {
            return obj.scene.name == MatchSceneName;
        }

        /// <summary>
        /// 今このフレームに生成してよいか
        ///
        /// シーンのロード中はプレハブの読み込みが完了扱いにならず、生成に失敗する。
        /// </summary>
        public static bool IsReadyToSpawn
        {
            get
            {
                if (!IsOnline)
                {
                    return true;
                }

                var sceneManager = Runner.SceneManager;

                return sceneManager == null || !sceneManager.IsBusy;
            }
        }

        /// <summary>
        /// ギミックを生成する
        ///
        /// オンラインなら Runner.Spawn で全員に複製され、オフラインなら通常の Instantiate になる。
        ///
        /// 生成した台がそのものを持ち、位置を他の台へ配ることになる。
        /// 「全員で一致している必要があるもの」はホストだけが生成すること。
        /// (バブルの配置や王冠のように、乱数で決まるもの)
        ///
        /// 逆に、持ち主が動かすものは持たせたい台から生成する。
        /// 復帰バブルは落ちた本人の台が生成する。
        /// ホストに生成させると、本人が左右に動かせなくなる。
        /// </summary>
        /// <param name="onBeforeSpawned">
        /// Spawned() より前に初期化したい場合に渡す。
        /// Spawn の戻り値に代入しても Spawned() には間に合わないため、
        /// ネットワーク状態の初期化は必ずここで行う。
        /// </param>
        public static T Spawn<T>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            System.Action<T> onBeforeSpawned = null)
            where T : Component
        {
            if (!IsOnline)
            {
                var instance = Object.Instantiate(prefab, position, rotation);
                onBeforeSpawned?.Invoke(instance);
                return instance;
            }

            // @memo: ここでホスト以外からの生成を弾いていたが、やめた。
            //        Shared Mode ではどの台からも生成でき、
            //        生成した台がそのものを持つ。
            //        弾いていたため、落ちた本人が復帰バブルを持てなかった。

            // NetworkObject を持たないもの (エフェクトなど) は同期対象ではないのでローカルに生成する
            var networkPrefab = prefab.GetComponent<NetworkObject>();
            if (networkPrefab == null)
            {
                var localInstance = Object.Instantiate(prefab, position, rotation);
                onBeforeSpawned?.Invoke(localInstance);
                return localInstance;
            }

            // シーンのロード中に生成しようとすると、プレハブの読み込みが完了扱いにならず失敗する。
            if (!IsReadyToSpawn)
            {
                Debug.LogError(
                    $"[NetworkSession] シーンのロード中は生成できません: {prefab.name}"
                    + " (IsReadyToSpawn が true になるまで待ってから生成してください)");
                return null;
            }

            // プレハブが自分の登録 ID を持っていないことがあるため、
            // テーブルから引いた ID で生成する
            if (!TryGetPrefabId(networkPrefab, out var prefabId))
            {
                Debug.LogError($"[NetworkSession] プレハブがテーブルに登録されていません: {prefab.name}");
                return null;
            }

            var spawned = Runner.Spawn(
                prefabId,
                position,
                rotation,
                Runner.LocalPlayer,
                (runner, obj) => onBeforeSpawned?.Invoke(obj.GetComponent<T>()));

            return spawned != null ? spawned.GetComponent<T>() : null;
        }

        /// <summary>
        /// ギミックを生成する (GameObject 版)
        /// </summary>
        public static GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            System.Action<GameObject> onBeforeSpawned = null)
        {
            var spawned = Spawn(
                prefab.transform,
                position,
                rotation,
                onBeforeSpawned == null ? null : trans => onBeforeSpawned(trans.gameObject));

            return spawned != null ? spawned.gameObject : null;
        }

        /// <summary>
        /// ギミックを破棄する
        /// </summary>
        public static void Despawn(GameObject target)
        {
            if (!IsOnline)
            {
                Object.Destroy(target);
                return;
            }

            var networkObject = target.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                // ネットワーク対象でない (エフェクトなど) のでそのまま破棄する
                Object.Destroy(target);
                return;
            }

            if (!HasAuthority)
            {
                return;
            }

            Runner.Despawn(networkObject);
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// プレハブの登録 ID を得る
        ///
        /// NetworkObject.NetworkTypeId は焼き込まれていないことがあるため、
        /// テーブルを実際に引いて対応する ID を探す。
        /// あわせて同期生成に必要なロードも済ませる。
        /// </summary>
        static bool TryGetPrefabId(NetworkObject prefab, out NetworkPrefabId prefabId)
        {
            var table = Runner.Prefabs;

            foreach (var (id, _) in table.GetEntries())
            {
                var loaded = table.Load(id, isSynchronous: true);

                if (loaded == prefab)
                {
                    prefabId = id;
                    return true;
                }
            }

            prefabId = default;
            return false;
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 対戦シーンの名前
        /// </summary>
        public const string MatchSceneName = "Main";

        static NetworkRunner _cachedRunner = null;
        #endregion
    }
}
