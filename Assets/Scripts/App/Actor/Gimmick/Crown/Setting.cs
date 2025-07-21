using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Gimmick.Crown
{
    [CreateAssetMenu(fileName = nameof(Setting), menuName = "ScriptableObjects/Actor/Crown/Setting")]
    public class Setting
        : ScriptableObject
    {
        public RunAwayCrown RunAwayCrownPrefab;
        public FinishCrown FinishCrownPrefab;
        public int InitShieldValue = 10;
    }
}