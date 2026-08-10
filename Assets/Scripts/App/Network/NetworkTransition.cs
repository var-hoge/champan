using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦時の画面遷移
    ///
    /// オンラインでは、シーン上の NetworkObject を全員で共有するために
    /// Fusion のシーンロードを通す必要がある。
    /// TransitionManager が独自にシーンを読み書きすると Fusion が把握できず、
    /// 後から参加した側でシーン上の Player がネットワーク対象にならない。
    ///
    /// そのため遷移は MasterClient が決め、全員が Fusion のシーンロードで追従する。
    /// </summary>
    public static class NetworkTransition
    {
        #region メソッド
        /// <summary>
        /// 画面遷移を要求する
        ///
        /// MasterClient 以外が呼んでも何も起きない。
        /// 全員が同じ画面にいる必要があるため、遷移の決定権は 1 箇所に集める。
        /// </summary>
        /// <summary>
        /// 次の遷移で使う演出の長さ
        ///
        /// 遷移エフェクトの長さは画面ごとに違う (タイトルからはフェード無しでシームレス)。
        /// 一律にすると本来と違う見た目になるため、呼び出し側の値をそのまま運ぶ。
        /// </summary>
        /// 既定値は演出なし。
        /// 万一届かなかったときに、余計な演出が出るより出ないほうが害が小さい。
        /// (最初の遷移であるタイトルからのものは、もともと演出なし)
        public static float FadeInDurationSec { get; private set; } = 0.0f;

        public static float FadeOutDurationSec { get; private set; } = 0.0f;

        public static bool IsReverse { get; private set; } = false;

        public static void RequestSceneChange(
            string sceneName,
            float fadeInDurationSec,
            float fadeOutDurationSec,
            bool isReverse)
        {
            // 決定権を持たない台にも同じ演出をさせる必要がある。
            // 長さを配ると届くまでの間に遷移が始まってしまうため、
            // 遷移の要求と一緒にこの台へ覚えさせておく。
            PublishFadeDurations(fadeInDurationSec, fadeOutDurationSec, isReverse);

            RequestSceneChange(sceneName);
        }

        /// <summary>
        /// 演出の長さを全員に配る
        /// </summary>
        static void PublishFadeDurations(float fadeInDurationSec, float fadeOutDurationSec, bool isReverse)
        {
            ApplyFadeDurations(fadeInDurationSec, fadeOutDurationSec, isReverse);

            NetworkFlowState.Instance?.NotifyFadeDurations(
                fadeInDurationSec, fadeOutDurationSec, isReverse);
        }

        /// <summary>
        /// 配られた演出の長さを覚える
        /// </summary>
        public static void ApplyFadeDurations(float fadeInDurationSec, float fadeOutDurationSec, bool isReverse)
        {
            FadeInDurationSec = fadeInDurationSec;
            FadeOutDurationSec = fadeOutDurationSec;
            IsReverse = isReverse;
        }

        public static void RequestSceneChange(string sceneName)
        {
            var runner = NetworkSession.Runner;
            if (runner == null)
            {
                Debug.LogError($"[NetworkTransition] セッションがありません: {sceneName}");
                return;
            }

            if (!NetworkSession.HasAuthority)
            {
                Debug.Log($"[NetworkTransition] 遷移の決定は MasterClient が行うため、ここでは何もしません: {sceneName}");
                return;
            }

            var sceneRef = runner.SceneManager.GetSceneRef(sceneName);
            if (!sceneRef.IsValid)
            {
                Debug.LogError(
                    $"[NetworkTransition] シーンが見つかりません: {sceneName}"
                    + " (Build Settings に登録されているか確認してください)");
                return;
            }

            Debug.Log($"[NetworkTransition] 全員を遷移させます: {sceneName}");
            runner.LoadScene(sceneRef);
        }
        #endregion
    }
}
