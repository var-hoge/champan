using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Cpu
{
    public struct CpuViewData
    {
        #region static メソッド
        public static CpuViewData Create()
        {
            var data = new CpuViewData();
            data.bubblePositions = new List<Vector2>();
            data.playerPositions = new List<Vector2>();
            return data;
        }
        #endregion

        #region フィールド
        public List<Vector2> bubblePositions;
        public List<Vector2> playerPositions;
        public Vector2 crownPosition;
        public float crownChangedTime;
        public int playerIndex;
        public bool isGround;
        public Vector2 maxVelocity;
        public Vector2 velocity;
        public float jumpPower;
        public float gravity;
        public float bubbleOffsetY;
        #endregion
    }
}