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
        /// ギミックを生成する
        ///
        /// オンラインなら Runner.Spawn で全員に複製され、オフラインなら通常の Instantiate になる。
        /// 権威を持たない側から呼んではいけない (HasAuthority で判定すること)。
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

            if (!HasAuthority)
            {
                Debug.LogError($"[NetworkSession] 権威を持たない側から Spawn が呼ばれました: {prefab.name}");
                return null;
            }

            // NetworkObject を持たないもの (エフェクトなど) は同期対象ではないのでローカルに生成する
            var networkPrefab = prefab.GetComponent<NetworkObject>();
            if (networkPrefab == null)
            {
                var localInstance = Object.Instantiate(prefab, position, rotation);
                onBeforeSpawned?.Invoke(localInstance);
                return localInstance;
            }

            var spawned = Runner.Spawn(
                networkPrefab,
                position,
                rotation,
                Runner.LocalPlayer,
                (runner, obj) => onBeforeSpawned?.Invoke(obj.GetComponent<T>()));

            return spawned != null ? spawned.GetComponent<T>() : null;
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

        #region private フィールド
        static NetworkRunner _cachedRunner = null;
        #endregion
    }
}
