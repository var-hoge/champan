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
using Scripts.Actor;
using System.Linq;

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
        public static int PlayerUseCharaIdList(int playerIdx)
        {
            if(_playerUseCharaIdList == null)
            {
                _playerUseCharaIdList = new List<int>();
                for (int idx = 0; idx < 4; idx++)
                {
                    _playerUseCharaIdList.Add(idx);
                }
            }

            return _playerUseCharaIdList[playerIdx];
        }
        static List<int> _playerUseCharaIdList;
        #endregion

        #region メソッド
        public RectTransform GetPickIconTransform(int playerIdx, int charaIdx)
        {
            return _charas[charaIdx].GetChild(playerIdx + 1).GetComponent<RectTransform>();
        }

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
            Destroy(_charaSelectIcons[playerIdx].gameObject);

            var sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
            _charaPickedIcons[playerIdx].sprite = sprite;
            _charaPickedIcons[playerIdx].rectTransform.sizeDelta = sprite.textureRect.size;
            _charaPickedIcons[playerIdx].rectTransform.localScale = Vector3.one * 0.6f;
            _charaPickedIcons[playerIdx].rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 25.0f);
            _charaPickedIcons[playerIdx].GetComponent<RectTransform>().DOPunchScale(Vector3.one * 2.0f, 0.1f);


            var isAllUsed = !_isUsedList.Any(a => !a);

            if (isAllUsed)
            {
                SceneChange().Forget();
            }

            return true;
        }

        private void Start()
        {
            // 初期化
            for (int idx = 0; idx < CharaMaxCount; idx++)
            {
                _isUsedList.Add(false);
                _charaSelectIcons[idx].Setup(this, PlayerUseCharaIdList(idx));
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        List<RectTransform> _charas;

        List<bool> _isUsedList = new();

        [SerializeField]
        List<CharaSelectIcon> _charaSelectIcons;

        [SerializeField]
        List<UnityEngine.UI.Image> _charaPickedIcons;
        #endregion

        #region privateメソッド
        async UniTask SceneChange()
        {
            await UniTask.WaitForSeconds(2.0f);

            TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 1.0f, 1.0f);
        }
        #endregion
    }
}