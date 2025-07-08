using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Graphics.Outline
{
    /// <summary>
    /// OutlineColorManager
    /// </summary>
    public class OutlineManager
        : BaseManagerProc<OutlineManager>
    {
        #region 定義
        public enum OutlineKind
        {
            WinnerPlayer,
            Player0,
            Player1,
            Player2,
            Player3,
        }
        #endregion

        #region プロパティ
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        public bool TryGetOutlineMaterial(OutlineKind kind, bool considerCpu, out Material outMaterial)
        {
            var playerIdx = kind switch
            {
                OutlineKind.Player0 => 0,
                OutlineKind.Player1 => 1,
                OutlineKind.Player2 => 2,
                OutlineKind.Player3 => 3,
                OutlineKind.WinnerPlayer => GameSequenceManager.WinnerPlayerIdx,
                _ => 0
            };

            outMaterial = null;
            if (considerCpu)
            {
                if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
                {
                    return false;
                }
            }

            outMaterial = _outlineMaterials[playerIdx];
            return true;
        }

        public bool TryGetOutlineMaterialForImage(OutlineKind kind, bool considerCpu, out Material outMaterial)
        {
            var playerIdx = kind switch
            {
                OutlineKind.Player0 => 0,
                OutlineKind.Player1 => 1,
                OutlineKind.Player2 => 2,
                OutlineKind.Player3 => 3,
                OutlineKind.WinnerPlayer => GameSequenceManager.WinnerPlayerIdx,
                _ => 0
            };

            outMaterial = null;
            if (considerCpu)
            {
                if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
                {
                    return false;
                }
            }

            outMaterial = _outlineMaterialsForImage[playerIdx];
            return true;
        }
        #endregion

        #region MonoBehavior の実装
        protected override void Awake()
        {
            foreach (var color in _playerColors)
            {
                var material = new Material(_outlineBaseMaterial);
                material.SetColor("_OutlineColor", color);
                _outlineMaterials.Add(material);
            }

            foreach (var color in _playerColors)
            {
                var material = new Material(_outlineBaseMaterialForImage);
                material.SetColor("_OutlineColor", color);
                _outlineMaterialsForImage.Add(material);
            }
        }
        #endregion

        #region private フィールド
        [SerializeField]
        List<Color> _playerColors;

        [SerializeField]
        Material _outlineBaseMaterial;

        [SerializeField]
        Material _outlineBaseMaterialForImage;

        List<Material> _outlineMaterials = new List<Material>();
        List<Material> _outlineMaterialsForImage = new List<Material>();
        #endregion

        #region private メソッド
        #endregion
    }
}