using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.ActionStd.Camera;

namespace App.Cpu
{
    /// <summary>
    /// CpuSetting
    /// </summary>
    [CreateAssetMenu(fileName = nameof(CpuSetting), menuName = "ScriptableObjects/Cpu/CpuSetting")]
    public class CpuSetting : ScriptableObject
    {
        public bool IsCpuPlayer0 = false;
        public bool IsCpuPlayer1 = true;
        public bool IsCpuPlayer2 = true;
        public bool IsCpuPlayer3 = true;
    }
}