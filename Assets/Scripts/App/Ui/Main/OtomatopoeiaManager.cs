﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.Main
{
    /// <summary>
    /// OtomatopoeiaManager
    /// </summary>
    public class OtomatopoeiaManager
        : TadaLib.ProcSystem.BaseManagerProc<OtomatopoeiaManager>
    {
        #region 定義
        public enum Kind
        {
            Chan,
            Pyon,
            Pan,
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        public void Spawn(int playerIdx, Vector3 pos, Vector3 playerVelocity)
        {
            var sprites = playerIdx switch
            {
                var idx when idx == 0 => _spritesP1,
                var idx when idx == 1 => _spritesP2,
                var idx when idx == 2 => _spritesP3,
                _ => _spritesP4,
            };

            if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
            {
                sprites = _spritesCP;
            }

            var max = _history == null ? sprites.Count : sprites.Count - 1;
            var sprite = sprites[UnityEngine.Random.Range(0, max)];
            if (sprite == _history)
            {
                sprite = sprites[sprites.Count - 1];
            }
            _history = sprite;

            var screenPos = Camera.main.WorldToScreenPoint(pos);
            var obj = Instantiate(_template, screenPos, Quaternion.identity);
            obj.transform.SetParent(transform);
            obj.GetComponent<OtomatopoeiaObj>().Init(sprite, playerVelocity);
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region private フィールド
        [SerializeField]
        OtomatopoeiaObj _template;

        [SerializeField]
        List<Sprite> _spritesP1;
        [SerializeField]
        List<Sprite> _spritesP2;
        [SerializeField]
        List<Sprite> _spritesP3;
        [SerializeField]
        List<Sprite> _spritesP4;
        [SerializeField]
        List<Sprite> _spritesCP;

        Sprite _history = null;
        //List<OtomatopoeiaObj> _objs = new List<OtomatopoeiaObj>();
        #endregion

        #region private メソッド
        #endregion
    }
}