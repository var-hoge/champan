using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using KanKikuchi.AudioManager;

namespace TadaLib.Sound
{
    /// <summary>
    /// BgmVolumeManager
    /// </summary>
    public class BgmVolumeManager
        : BaseManagerProc<BgmVolumeManager>
        , IProcManagerUpdate
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region IManagerProcUpdate の実装
        public void OnUpdate()
        {
            // TimeScale 
            var targetVolume = TimeScaleToVolume(Time.timeScale);

            if (targetVolume < _volume)
            {
                _volume = targetVolume;
            }
            else
            {
                _volume = Util.InterpUtil.Linier(_volume, targetVolume, 0.25f, Time.deltaTime);
            }

            BGMManager.Instance.ChangeBaseVolume(_volume);
        }
        #endregion

        #region private フィールド
        float _volume = 1.0f;
        #endregion

        #region private メソッド
        float TimeScaleToVolume(float timeScale)
        {
            if (timeScale >= 1.0f)
            {
                return 1.0f;
            }

            return Util.InterpUtil.Remap(timeScale, 0.0f, 1.0f, 0.25f, 1.0f);
        }
        #endregion
    }
}