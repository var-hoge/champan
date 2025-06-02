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


namespace App.Actor
{
    public class CharacterManager: TadaLib.Util.SingletonMonoBehaviour<CharacterManager>
    {
        public Sprite GetCharaImage(int charaIdx)
        {
            var hoge =  Images[charaIdx];
            return hoge[Random.Range(0, hoge.Count)];
        }

        [SerializeField]
        List<Sprite> _chara1Images;
        [SerializeField]
        List<Sprite> _chara2Images;
        [SerializeField]
        List<Sprite> _chara3Images;
        [SerializeField]
        List<Sprite> _chara4Images;


        private List<Sprite>[] _images = null;
        private List<Sprite>[] Images
        {
            get
            {
                if (_images == null)
                {
                    _images = new List<Sprite>[]
                    {
                        _chara1Images,
                        _chara2Images,
                        _chara3Images,
                        _chara4Images
                    };
                }
                return _images;
            }
        }
    }
}
