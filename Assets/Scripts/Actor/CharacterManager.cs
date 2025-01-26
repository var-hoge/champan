using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;


namespace Scripts.Actor
{
    public class CharacterManager: TadaLib.Util.SingletonMonoBehaviour<CharacterManager>
    {
        public Sprite GetCharaImage(int charaIdx)
        {
            return _charaImages[charaIdx];
        }

        [SerializeField]
        List<Sprite> _charaImages;
    }
}
