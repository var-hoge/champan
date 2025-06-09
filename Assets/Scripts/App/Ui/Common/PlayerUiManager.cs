using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace Ui.Common
{
    /// <summary>
    /// PlayerUiManager
    /// </summary>
    public class PlayerUiManager
        : BaseManagerProc<PlayerUiManager>
    {
        #region プロパティ
        #endregion

        #region メソッド
        public Sprite GetPlayerIdSprite(int playerIdx)
        {
            return _playerIdList[playerIdx];
        }

        public Sprite GetPlayerWinCountPanelSprite(int playerIdx)
        {
            return _playerWinCountPanelList[playerIdx];
        }

        public Color GetPlayerIdColor(int playerIdx)
        {
            return _playerIdColor[playerIdx];
        }
        #endregion

        #region private メソッド
        [SerializeField]
        List<Sprite> _playerIdList;

        [SerializeField]
        List<Sprite> _playerWinCountPanelList;

        [SerializeField]
        List<Color> _playerIdColor;
        #endregion

        #region private フィールド
        #endregion
    }
}