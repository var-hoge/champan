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
        /// <summary>
        /// BGM の基準となる音量 (0.0 〜 1.0)
        ///
        /// このクラスは演出で音量を一時的に下げる。
        /// 下げた音量の戻し先がここになるため、
        /// ユーザーの音量設定はここへ渡す。
        ///
        /// シーンをまたいで持ち回るため static で持つ。
        /// このコンポーネントはシーンに置かれており、
        /// 設定を反映する側から常に触れるとは限らないため。
        /// </summary>
        public static float BaseVolume { get; set; } = 1.0f;
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region TadaLib.ProcSystem.IProcManagerUpdate の実装
        public void OnUpdate()
        {
            // TimeScale
            var targetRate = TimeScaleToRate(Time.timeScale);

            if (targetRate < _rate)
            {
                _rate = targetRate;
            }
            else
            {
                _rate = Util.InterpUtil.Linier(_rate, targetRate, 0.25f, Time.deltaTime);
            }

            // 演出で下げる分は基準の音量に掛ける。
            // 固定値で戻すと、ユーザーの音量設定を上書きしてしまうため。
            var volume = BaseVolume * _rate;

            if (_volumePrev != volume)
            {
                _volumePrev = volume;
                BGMManager.Instance.ChangeBaseVolume(volume);
            }
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 演出で音量に掛ける割合 (0.0 〜 1.0)
        /// </summary>
        float _rate = 1.0f;

        float _volumePrev = 1.0f;
        #endregion

        #region private メソッド
        float TimeScaleToRate(float timeScale)
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