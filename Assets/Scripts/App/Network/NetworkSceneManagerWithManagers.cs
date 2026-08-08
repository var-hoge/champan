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
        /// オンラインではシーンのロードを Fusion が行うため、
        /// 遷移演出もここに合わせて再生する
        /// (TransitionManager の遷移処理は通らない)
        /// </summary>
        protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters sceneParams)
        {
            var effect = TadaLib.Scene.TransitionEffectManager.Instance;

            if (effect != null)
            {
                yield return effect.FadeIn(_fadeDurationSec, false).ToCoroutine();
            }

            yield return base.LoadSceneCoroutine(sceneRef, sceneParams);

            // ロード後は別のシーンのインスタンスになっているため取り直す
            effect = TadaLib.Scene.TransitionEffectManager.Instance;

            if (effect != null)
            {
                yield return effect.FadeOut(_fadeDurationSec, false).ToCoroutine();
            }
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
        }
        #endregion

        #region private メソッド
        IEnumerator LoadManagerScenes()
        {
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
        const float _fadeDurationSec = 0.3f;

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
