﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using UniRx;
using Cysharp.Threading.Tasks;
using System;

namespace TadaLib.Scene
{
    /// <summary>
    /// シーン遷移エフェクト管理
    /// </summary>
    public class TransitionEffectManager
        : BaseManagerProc<TransitionEffectManager>
    {
        #region プロパティ
        #endregion

        #region メソッド
        /// <summary>
        /// フェード開始
        /// </summary>
        /// <param name="durationSec"></param>
        /// <returns></returns>
        public async UniTask FadeIn(float durationSec, bool isReverse)
        {
            await FadeImpl(durationSec, true, isReverse);
        }

        /// <summary>
        /// フェード終了
        /// </summary>
        /// <param name="durationSec"></param>
        /// <returns></returns>
        public async UniTask FadeOut(float durationSec, bool isReverse)
        {
            await FadeImpl(durationSec, false, isReverse);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        TransitionEffect _transitoinEffect;
        #endregion

        #region privateメソッド
        async UniTask FadeImpl(float durationSec, bool isFadeIn, bool isReverse)
        {
            var startTime = Time.time;
            while (true)
            {
                var progress = (Time.time - startTime) / durationSec;
                if (!isFadeIn)
                {
                    progress = 1.0f - progress;
                }

                progress = Mathf.Clamp01(progress);

                _transitoinEffect.SetProgress(progress, isFadeIn, isReverse);

                if (Time.time - startTime >= durationSec)
                {
                    break;
                }

                await UniTask.Yield();
            }
        }
        #endregion
    }
}