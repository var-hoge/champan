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
