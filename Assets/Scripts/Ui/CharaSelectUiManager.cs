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

namespace Ui
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class CharaSelectUiManager
        : MonoBehaviour
    {
        #region プロパティ
        public int CharaMaxCount => 4;

        // 各プレイヤーの使用キャラクター
        public static List<int> PlayerUseCharaIdList;
        #endregion

        #region メソッド
        public bool NotifySelect(int playerIdx, int charaIdx)
        {
            // すでに使われていたらダメ
            if (_isUsedList[charaIdx])
            {
                return false;
            }

            // 使用済みにする
            _isUsedList[charaIdx] = true;

            // TODO: 使用済み演出

            return true;
        }

        private void Start()
        {
            if (CharaSelectUiManager.PlayerUseCharaIdList == null)
            {
                // 初期化
                for (int idx = 0; idx < CharaMaxCount; idx++)
                {
                    PlayerUseCharaIdList.Add(idx);
                    _isUsedList.Add(false);
                }
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        List<UnityEngine.UI.Image> _charaImages;

        List<bool> _isUsedList;
        #endregion

        #region privateメソッド
        #endregion
    }
}