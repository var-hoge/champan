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
            if (_playerUseCharaIdList == null)
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
        public RectTransform GetPickIconTransform(int playerIdx, int selectIdx)
        {
            return _charas[selectIdx].GetChild(playerIdx + 1).GetComponent<RectTransform>();
        }

        public bool IsUsed(int selectIdx)
        {
            return _isUsedList[selectIdx];
        }

        public bool NotifySelect(int playerIdx, int selectIdx)
        {
            // すでに使われていたらダメ
            if (_isUsedList[selectIdx])
            {
                return false;
            }

            // キャラクター ID に変換
            // @memo: キャラクターセレクト画面のキャラの並びが ID と一致していないので変換が必要
            var charaIdx = selectIdx switch
            {
                0 => 0,
                1 => 3,
                2 => 1,
                3 => 2,
                _ => throw new System.Exception()
            };

            // キャラクター決定
            _playerUseCharaIdList[playerIdx] = charaIdx;

            // 使用済みにする
            _isUsedList[selectIdx] = true;

            Destroy(_charaSelectIcons[playerIdx].gameObject);

            var sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
            _charaPickedIcons[playerIdx].sprite = sprite;
            _charaPickedIcons[playerIdx].rectTransform.sizeDelta = sprite.textureRect.size;
            _charaPickedIcons[playerIdx].rectTransform.localScale = Vector3.one * 0.6f;
            _charaPickedIcons[playerIdx].rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 25.0f);
            _charaPickedIcons[playerIdx].GetComponent<RectTransform>().DOPunchScale(Vector3.one * 1.5f, 0.1f);

            var selectableCharaImage = _charas[selectIdx].GetChild(0).GetComponent<UnityEngine.UI.Image>();
            selectableCharaImage.sprite = _selectedCharaSprite;
            selectableCharaImage.rectTransform.sizeDelta = _selectedCharaSprite.textureRect.size;
            selectableCharaImage.rectTransform.localScale *= 1.3f;
            selectableCharaImage.rectTransform.DOShakePosition(0.20f, 5.0f, 5, fadeOut: true);
            selectableCharaImage.rectTransform.DOScale(selectableCharaImage.rectTransform.localScale * 1.2f, 0.25f).SetEase(Ease.OutQuint);
            selectableCharaImage.rectTransform.DOShakeRotation(0.20f, 20.0f, 30, fadeOut: true);

            var isAllUsed = !_isUsedList.Any(a => !a);

            if (isAllUsed)
            {
                SceneChange().Forget();
            }
            else
            {
                // 同じcharacterを選択しているカーソルがある場合は別に移す
                foreach (var cursor in _charaSelectIcons)
                {
                    if (cursor != null && cursor.SelectIdx == selectIdx)
                    {
                        cursor.ForceMove(true);
                    }
                }
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

        [SerializeField]
        Sprite _selectedCharaSprite;
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