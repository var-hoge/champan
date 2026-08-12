using KanKikuchi.AudioManager;
using UnityEngine;

namespace App.Sound
{
    /// <summary>
    /// 音量の設定
    ///
    /// 音の鳴らし方は各所に散っているが、音量はどれも
    /// BGMManager / SEManager の基準値を通る。
    /// 個々の再生に手を入れず、その基準値だけを変える。
    ///
    /// BGM と SE は分けずに、まとめて一つの音量で扱う。
    ///
    /// 覚えた値は次に遊ぶときにも使う。
    /// 毎回設定し直させるものではないため。
    /// </summary>
    public static class VolumeSettings
    {
        #region プロパティ
        /// <summary>
        /// 音量の段階 (0 〜 StepMax)
        ///
        /// 細かく刻んでも聞き分けられないため、段階で持つ。
        /// 段階のまま覚えることで、読み直したときにずれない。
        /// </summary>
        public static int Step { get; private set; } = DefaultStep;

        /// <summary>
        /// 音に渡す音量 (0.0 〜 1.0)
        ///
        /// AudioSource の音量は 1.0 で頭打ちになるため、
        /// それより大きい値を渡しても意味がない。
        /// </summary>
        public static float Volume => Step / (float)StepMax;

        /// <summary>
        /// 段階の上限 (0 を含めて 10 段階)
        /// </summary>
        public const int StepMax = 9;
        #endregion

        #region メソッド
        /// <summary>
        /// 覚えている音量を読んで、音に反映する
        ///
        /// BGMManager / SEManager は最初に触れられたときに
        /// 設定アセットの値で基準を上書きする。
        /// そのため、必ず先に触れてから設定する。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Load()
        {
            Step = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeyStep, DefaultStep), 0, StepMax);

            Apply();
        }

        /// <summary>
        /// 音量の段階を変える
        /// </summary>
        public static void SetStep(int step)
        {
            Step = Mathf.Clamp(step, 0, StepMax);

            Apply();

            PlayerPrefs.SetInt(PrefKeyStep, Step);
        }

        /// <summary>
        /// 覚えた音量を書き出す
        ///
        /// つまみを動かすたびに書き出すと、その回数だけ保存が走る。
        /// 手を離したときにまとめて書き出す。
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        #endregion

        #region private メソッド
        static void Apply()
        {
            BGMManager.Instance.ChangeBaseVolume(Volume);
            SEManager.Instance.ChangeBaseVolume(Volume);
        }
        #endregion

        #region private フィールド
        const string PrefKeyStep = "Sound.VolumeStep";

        /// <summary>
        /// 初めて遊ぶときの音量
        ///
        /// 真ん中より少し上に置く。
        /// 大きすぎると最初の一音で驚かせるが、
        /// 上げ幅も残しておきたいため。
        /// </summary>
        const int DefaultStep = 5;
        #endregion
    }
}
