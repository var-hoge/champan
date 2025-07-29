using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using KanKikuchi.AudioManager;

namespace TadaLib.Scene
{
    /// <summary>
    /// TimeScaleManager
    /// </summary>
    public class TimeScaleManager
        : BaseManagerProc<TimeScaleManager>
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void SetTemporaryTimeScale(float timeScale, float durationSec, float delaySec = 0.0f)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _ = ApplyTimeScaleAsync(timeScale, durationSec, delaySec, _cts.Token);
        }
        #endregion

        #region private フィールド
        float _originalTimeScale = 1.0f;
        CancellationTokenSource _cts;
        #endregion

        #region private メソッド
        async UniTaskVoid ApplyTimeScaleAsync(float timeScale, float durationSec, float delaySec, CancellationToken token)
        {
            try
            {
                if (delaySec > 0.0f)
                {
                    await UniTask.WaitForSeconds(delaySec, cancellationToken: token);
                }

                Time.timeScale = timeScale;
                BGMManager.Instance.ChangeBaseVolume(timeScale);

                if (durationSec > 0.0f)
                {
                    await UniTask.WaitForSeconds(durationSec, cancellationToken: token);
                }

                Time.timeScale = _originalTimeScale;
                BGMManager.Instance.ChangeBaseVolume(1.0f);
            }
            catch (OperationCanceledException)
            {
                Time.timeScale = _originalTimeScale;
                BGMManager.Instance.ChangeBaseVolume(1.0f);
            }
        }
        #endregion
    }
}