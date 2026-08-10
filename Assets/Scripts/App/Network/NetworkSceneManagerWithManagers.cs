using System.Collections;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Network
{
    /// <summary>
    /// マネージャシーンを維持する Fusion のシーンマネージャ
    ///
    /// champan は ManagerSceneLoader が TadaLibManager などを追加ロードして、
    /// ProcManager や PlayerManager をそこに置いている。
    ///
    /// Fusion はシーンを Single モードでロードするため、そのままだと
    /// これらのマネージャシーンごと落ちてしまい、
    /// PlayerManager.Instance が null になって各所で NullReference になる。
    ///
    /// そこで、Fusion がシーンをロードした直後にマネージャシーンを復帰させる。
    /// </summary>
    public class NetworkSceneManagerWithManagers
        : NetworkSceneManagerDefault
    {
        #region NetworkSceneManagerDefault の実装
        /// <summary>
        /// オンラインではシーンのロードを Fusion が行う
        /// (TransitionManager の遷移処理は通らない)
        ///
        /// 遷移演出もここで出す。
        /// 全台が必ず通る場所がここしかないため。
        /// 長さは画面ごとに違うので、遷移の要求と一緒に配られたものを使う。
        /// </summary>
        protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters sceneParams)
        {
            var fadeInDurationSec = NetworkTransition.FadeInDurationSec;
            if (fadeInDurationSec > 0.0f)
            {
                yield return TadaLib.Scene.TransitionEffectManager.Instance
                    .FadeIn(fadeInDurationSec, NetworkTransition.IsReverse)
                    .ToCoroutine();
            }

            // Single モードで読み込むと Unity がマネージャシーンごと破棄し、
            // その直後に新しいシーンの Start が走ってしまう。
            // (PlayerInputManager などが無い状態で初期化されて NullReference になる)
            //
            // NetworkLoadSceneParameters は差し替えられないため、
            // マネージャ側を DontDestroyOnLoad へ移して破棄されないようにする。
            PersistManagerScenes();

            yield return base.LoadSceneCoroutine(sceneRef, sceneParams);
        }

        protected override IEnumerator OnSceneLoaded(
            SceneRef sceneRef,
            UnityEngine.SceneManagement.Scene scene,
            NetworkLoadSceneParameters sceneParams)
        {
            yield return base.OnSceneLoaded(sceneRef, scene, sceneParams);

            // シーン上の NetworkObject が Spawned() で参照する前に用意しておく必要があるため、
            // ここでロードを完了させておく
            yield return LoadManagerScenes();

            var fadeOutDurationSec = NetworkTransition.FadeOutDurationSec;
            if (fadeOutDurationSec > 0.0f)
            {
                yield return TadaLib.Scene.TransitionEffectManager.Instance
                    .FadeOut(fadeOutDurationSec, NetworkTransition.IsReverse)
                    .ToCoroutine();
            }
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// マネージャシーンの中身を DontDestroyOnLoad へ移して、
        /// Fusion の Single ロードで破棄されないようにする
        ///
        /// オンラインではシーン遷移を Fusion が行うため、
        /// オフラインのようにマネージャシーンを毎回読み直す必要はない。
        /// </summary>
        void PersistManagerScenes()
        {
            if (_isPersisted)
            {
                return;
            }

            _isPersisted = true;

            foreach (var sceneName in ManagerSceneNames)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    Object.DontDestroyOnLoad(root);
                }

                Debug.Log($"[NetworkSceneManagerWithManagers] マネージャを常駐させました: {sceneName}");
            }
        }

        IEnumerator LoadManagerScenes()
        {
            // 常駐させた後に読み直すとマネージャが二重になる
            if (_isPersisted)
            {
                yield break;
            }

            foreach (var sceneName in ManagerSceneNames)
            {
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    continue;
                }

                Debug.Log($"[NetworkSceneManagerWithManagers] マネージャシーンを再ロードします: {sceneName}");
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
        }
        #endregion

        #region private フィールド
        bool _isPersisted = false;

        /// <summary>
        /// ManagerSceneLoader と揃えること
        /// </summary>
        static readonly string[] ManagerSceneNames =
        {
            "TadaLibManager",
            "TadaLibGlobalManager",
#if UNITY_EDITOR
            "TadaLibDebug",
#endif
        };
        #endregion
    }
}
