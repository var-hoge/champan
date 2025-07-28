using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App.Actor;

namespace App.Cpu
{
    /// <summary>
    /// CpuViewDataManager
    /// </summary>
    public class CpuViewDataManager
        : BaseManagerProc<CpuViewDataManager>
        , IProcManagerUpdate
    {
        #region プロパティ
        #endregion

        #region メソッド
        public CpuViewData GetCpuViewData(int idx) => _cpuViewData[idx];
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                _cpuViewData.Add(new CpuViewData());
            }
        }
        #endregion

        #region IProcManagerUpdate の実装
        public void OnUpdate()
        {
            var bubbles = GameObject.FindObjectsByType<App.Actor.Gimmick.Bubble.Bubble>(FindObjectsSortMode.None);
            // クラウン座標の更新
            foreach (var bubble in bubbles)
            {
                if (!bubble.HasCrown)
                {
                    continue;
                }

                if (Vector2.Distance(bubble.transform.position, _crownPosPrev) > 2.0f)
                {
                    // 変わった
                    _crownChangedTime = Time.time;
                    _crownPosPrev = bubble.transform.position;
                }
            }

            for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                UpdateCpuViewData(idx, bubbles);
            }
        }
        #endregion

        #region privateフィールド
        List<CpuViewData> _cpuViewData = new List<CpuViewData>();
        Vector2 _crownPosPrev = Vector2.zero;
        float _crownChangedTime = 0.0f;
        #endregion

        #region privateメソッド
        void UpdateCpuViewData(int idx, Actor.Gimmick.Bubble.Bubble[] bubbles)
        {
            var targetPlayer = PlayerManager.TryGetPlayer(idx);
            if (targetPlayer == null)
            {
                return;
            }

            var data = CpuViewData.Create();

            // バブル座標の更新
            data.bubblePositions.Clear();
            foreach (var bubble in bubbles)
            {
                data.bubblePositions.Add(bubble.transform.position);
                if (bubble.HasCrown)
                {
                    data.crownPosition = bubble.transform.position;
                }
            }
            data.crownChangedTime = _crownChangedTime;

            // プレイヤー座標の更新
            data.playerPositions.Clear();
            for (int playerIdx = 0; playerIdx < Actor.Player.Constant.PlayerCountMax; ++playerIdx)
            {
                var player = PlayerManager.TryGetPlayer(playerIdx);

                var playerPos = Vector2.zero;
                if (player != null)
                {
                    playerPos = player.transform.position;
                }

                data.playerPositions.Add(playerPos);
            }

            data.playerIndex = idx;

            var dataHolder = targetPlayer.GetComponent<Actor.Player.DataHolder>();
            data.isGround = dataHolder.NoGroundDurationSec == 0.00f;
            data.maxVelocity = dataHolder.MaxVelocity;
            data.velocity = dataHolder.Velocity;
            data.jumpPower = dataHolder.JumpPower;
            data.gravity = dataHolder.Gravity;

            data.bubbleOffsetY = 0.8f;

            _cpuViewData[idx] = data;
        }
        #endregion
    }
}