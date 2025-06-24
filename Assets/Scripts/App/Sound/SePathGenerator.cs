using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Sound
{
    /// <summary>
    /// SePathGenerator
    /// </summary>
    public static class SePathGenerator
    {
        public static IEnumerable<string> GetSEPath(string path, int count)
        {
            for (var n = 1; n <= count; ++n)
            {
                yield return (n < 10) ? $"{path}0{n}" : $"{path}{n}";
            }
        }
    }
}