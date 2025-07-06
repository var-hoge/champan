using UnityEngine;

namespace App.Actor.Gimmick.Bubble
{
    public class BubbleUtil
    {
        /// <summary>
        /// オブジェクトを飛ばす
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        public static void Blow(GameObject obj, Vector3 bubblePosition, float blowPower)
        {
            // 振動
            {
                var playerIdx = obj.GetComponent<Player.DataHolder>().PlayerIdx;
                if (Cpu.CpuManager.Instance.IsCpu(playerIdx) is false)
                {
                    TadaLib.Input.PlayerInputManager.Instance.InputProxy(playerIdx).Vibrate(TadaLib.Input.PlayerInputProxy.VibrateType.Happy);
                }
            }

            var dirDiff = obj.transform.position - bubblePosition;
            dirDiff.z = 0.0f;
            var dirUnit = dirDiff.normalized;
            // x 軸方向の成分を強める
            dirUnit.x += Mathf.Sign(dirUnit.x) * 0.5f;
            dirUnit = dirUnit.normalized;
            if (dirUnit.sqrMagnitude < 0.0001f)
            {
                // 座標が一致していたなら適当な方向に飛ばす
                dirUnit = Vector3.right;
            }

            var moveCtrl = obj.GetComponent<Actor.Player.MoveCtrl>();
            moveCtrl.SetVelocityForce(dirUnit * blowPower);
            moveCtrl.SetUncontrollableTime(0.3f);
        }
    }
}