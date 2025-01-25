using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using TadaLib;
using AnnulusGames.SceneSystem;

namespace Scene
{
    /// <summary>
    /// Awake前にManagerSceneを自動でロードするクラス
    /// </summary>
    public class ManagerSceneLoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadManagerScene()
        {
#if UNITY_EDITOR
            Scenes.LoadScenes("TadaLibManager", "TadaLibGlobalManager", "TadaLibDebug");
            // シーン終了時にリロードされる Manager Scene
            TadaLib.Scene.TransitionManager.SetNeedReloadScenes("TadaLibManager", "TadaLibDebug");
#else
            Scenes.LoadScenes("TadaLibManager", "TadaLibGlobalManager");
            // シーン終了時にリロードされる Manager Scene
            TadaLib.Scene.TransitionManager.SetNeedReloadScenes("TadaLibManager");
#endif
        }
    }
}