﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib;
using TadaLib.ProcSystem;
using UnityEngine.SceneManagement;
using TadaLib.Input;
using UniRx;
using Cysharp.Threading.Tasks;
using System;
using AnnulusGames.SceneSystem;
using System.Linq;

namespace TadaLib.Scene
{
    /// <summary>
    /// シーン遷移管理
    /// </summary>
    public class TransitionManager
        : BaseManagerProc<TransitionManager>
    {
        #region staticメソッド
        /// <summary>
        /// シーン遷移のたびにリロードするシーンを追加する
        /// @attention: 追加した順にリロードが走る
        /// </summary>
        /// <param name="sceneName"></param>
        public static void SetNeedReloadScenes(params string[] sceneName)
        {
            _reloadTargetScenes = sceneName.ToList();
        }
        #endregion

        #region プロパティ
        public bool IsTransitioning => _isLocked;
        #endregion

        #region メソッド
        /// <summary>
        /// 遷移を開始する
        /// </summary>
        /// <param name="nextScene">次シーン名</param>
        /// <param name="fadeInDurationSec">遷移エフェクトの時間(遷移開始時)</param>
        /// <param name="fadeOutDurationSec">遷移エフェクトの時間(遷移終了時)</param>
        /// <param name="guranteedWaitDurationSec">遷移中の最低待ち時間(エフェクト時間は含めない)</param>
        public async void StartTransition(string nextScene, float fadeInDurationSec, float fadeOutDurationSec, float guranteedWaitDurationSec = 0.05f, bool isReverse = false)
        {
            if (_isLocked)
            {
                Debug.LogWarning("Scene.TransitionManager: 既に遷移を開始しています");
                return;
            }

            OnTransitionBegin();

            // 遷移エフェクトを開始
            await TransitionEffectManager.Instance.FadeIn(fadeInDurationSec, isReverse);

            // 最低待ち時間
            await UniTask.Delay(TimeSpan.FromSeconds(guranteedWaitDurationSec));

            var unloadScenes = new List<string>()
            {
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };
            unloadScenes.AddRange(_reloadTargetScenes);

            var unloadScene = Scenes.UnloadScenesAsync(unloadScenes.ToArray());
            await UniTask.Yield();
            while (!unloadScene.IsDone)
            {
                await UniTask.Yield();
            }
            //await Scenes.UnloadScenesAsync(unloadScenes.ToArray()).ToUniTask();

            //await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            var loadScenes = new List<string>();
            loadScenes.AddRange(Enumerable.Reverse(_reloadTargetScenes).ToList());
            loadScenes.Add(nextScene);

            //await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            //await Scenes.LoadScenesAsync(loadScenes.ToArray()).ToUniTask();
            var loadScene = Scenes.LoadScenesAsync(loadScenes.ToArray());
            await UniTask.Yield();
            while (!loadScene.IsDone)
            {
                await UniTask.Yield();
            }

            // nextScene を activeScene にする (メインはこれなので)
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByName(nextScene));

            await TransitionEffectManager.Instance.FadeOut(fadeOutDurationSec, isReverse);

            // すべて完了
            OnTransitionEnd();
        }

        // タイトル以外では使用禁止
        public async void StartTransitionAdvancedForTitle(
            string nextScene,
            Func<UniTask> preUnloadFunc,
            float fadeInDurationSec = 0.0f,
            float fadeOutDurationSec = 0.0f,
            float guranteedWaitDurationSec = 0.0f,
            bool isReverse = false)
        {
            if (_isLocked)
            {
                Debug.LogWarning("Scene.TransitionManager: 既に遷移を開始しています");
                return;
            }

            OnTransitionBegin();

            if (fadeInDurationSec > 0.0f)
            {
                // 遷移エフェクトを開始
                await TransitionEffectManager.Instance.FadeIn(fadeInDurationSec, isReverse);
            }

            if (guranteedWaitDurationSec > 0.0f)
            {
                // 最低待ち時間
                await UniTask.Delay(TimeSpan.FromSeconds(guranteedWaitDurationSec));
            }

            // 次シーンロード (Activate は後ほど)
            var loadScenes = new List<string>();
            //loadScenes.AddRange(Enumerable.Reverse(_reloadTargetScenes).ToList());
            loadScenes.Add(nextScene);

            var loadHandle = Scenes.LoadScenesAsync(loadScenes.ToArray());
            loadHandle.AllowSceneActivation(false);

            var loadStartTime = Time.time;
            await UniTask.WaitUntil(() => loadHandle.Progress > 0.85f || (Time.time - loadStartTime) > 0.3f);

            // 前シーンアンロード前処理
            await preUnloadFunc();

            // 次シーンアクティベート
            loadHandle.AllowSceneActivation(true);

            await loadHandle.ToYieldInteraction();

            // 前シーンアンロード
            {
                var unloadScenes = new List<string>() {
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                };
                //unloadScenes.AddRange(_reloadTargetScenes);

                Scenes.UnloadScenes(unloadScenes.ToArray());
            }

            // nextScene を activeScene にする (メインはこれなので)
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByName(nextScene));

            if (fadeOutDurationSec > 0.0f)
            {
                await TransitionEffectManager.Instance.FadeOut(fadeOutDurationSec, isReverse);
            }

            // すべて完了
            OnTransitionEnd();
        }
        #endregion

        #region private static フィールド
        static List<string> _reloadTargetScenes = new List<string>();
        #endregion

        #region privateフィールド
        bool _isLocked = false;
        #endregion

        #region privateメソッド
        void OnTransitionBegin()
        {
            _isLocked = true;

            // 入力を無効化
            //InputUtil.DisablePlayerInput();
        }

        void OnTransitionEnd()
        {
            _isLocked = false;

            // 入力復活
            //InputUtil.EnablePlayerInput();
        }

        List<string> GetReloadScenes()
        {
            return new List<string>()
            {
                "Manager",
                "TadaLibManager"
            };
        }
        #endregion
    }
}