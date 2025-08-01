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
using App.Actor;
using System.Linq;
using UnityEditor;
using App.Ui.Common;

namespace App.Ui.CharaSelect
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

        public bool IsSceneChanging => _isSceneChanging;
        #endregion

        #region メソッド
        public RectTransform GetPickIconTransform(int playerIdx, int selectIdx)
        {
            return _charas[selectIdx].GetPickIconTransform(playerIdx);
        }

        public bool IsUsed(int selectIdx)
        {
            return _isUsedList[selectIdx];
        }

        public void NotifyCursorOver(int playerIdx, int selectIdx)
        {
            // キャラクター ID に変換
            var charaIdx = SelectIdxToCharaIdx(selectIdx);

            // キャラクター仮決定
            _playerUseCharaIdList[playerIdx] = charaIdx;
        }

        public bool NotifySelect(int playerIdx, int selectIdx)
        {
            // すでに使われていたらダメ
            if (_isUsedList[selectIdx])
            {
                return false;
            }

            // 使用済みにする
            _isUsedList[selectIdx] = true;

            // キャラクター ID に変換
            var charaIdx = SelectIdxToCharaIdx(selectIdx);

            // キャラクター決定
            _playerUseCharaIdList[playerIdx] = charaIdx;

            // CPU 扱いにしない
            Cpu.CpuManager.Instance.SetIsCpu(playerIdx, false);

            _charas[selectIdx].OnSelected(() =>
            {
                //// アニメーションが完了したら設定
                //_charaPickedIcons[playerIdx].SetChara(charaIdx);
            });

            var isAllUsed = !_isUsedList.Any(a => !a);

            if (isAllUsed)
            {
                //SceneChange().Forget();
            }
            else
            {
                // 同じcharacterを選択しているカーソルがある場合は別に移す
                foreach (var cursor in _charaSelectCursors)
                {
                    if (playerIdx != cursor.PlayerIdx && cursor.SelectIdx == selectIdx)
                    {
                        cursor.ForceMove(true);
                    }
                }
            }

            // スタートエリアを出現
            if (!_startArea.activeSelf)
            {
                // @todo: アニメ
                _startArea.gameObject.SetActive(true);
            }

            return true;
        }

        public bool NotifyCancelSelect(int playerIdx)
        {
            if (_isSceneChanging)
            {
                return false;
            }

            var charaIdx = _playerUseCharaIdList[playerIdx];
            var selectIdx = CharaIdxToSelectIdx(charaIdx);

            _isUsedList[selectIdx] = false;

            _charas[selectIdx].OnCancelSelected();

            // CPU 扱いに
            Cpu.CpuManager.Instance.SetIsCpu(playerIdx, true);

            return true;
        }

        private void Start()
        {
            // 初期化
            for (int idx = 0; idx < CharaMaxCount; idx++)
            {
                _isUsedList.Add(false);
                _charaSelectCursors[idx].Setup(this, PlayerUseCharaIdList(idx));
            }
            for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                // デフォルトで CPU 扱いに
                Cpu.CpuManager.Instance.SetIsCpu(idx, true);
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        List<CharaSelectChara> _charas;

        List<bool> _isUsedList = new();

        [SerializeField]
        List<CharaSelectCursor> _charaSelectCursors;

        [SerializeField]
        List<PlayerIconSlot> _charaPickedIcons;

        [SerializeField]
        Sprite _selectedCharaSprite;

        [SerializeField]
        GameObject _startArea;

        bool _isSceneChanging = false;

        [SerializeField]
        List<Sprite> _unselectedCharaSprites;
        #endregion

        #region privateメソッド
        public async UniTask SceneChange()
        {
            // CPU に選ばれていないキャラを割り当てる
            {
                var unusedCharaList = new List<int>();
                for (int idx = 0; idx < CharaMaxCount; idx++)
                {
                    unusedCharaList.Add(idx);
                }

                for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
                {
                    if (Cpu.CpuManager.Instance.IsCpu(idx))
                    {
                        continue;
                    }
                    unusedCharaList.Remove(_playerUseCharaIdList[idx]);
                }

                for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
                {
                    if (Cpu.CpuManager.Instance.IsCpu(idx))
                    {
                        _playerUseCharaIdList[idx] = unusedCharaList.First();
                        unusedCharaList.RemoveAt(0);
                    }
                }
            }

            for (int idx = 0; idx < CharaMaxCount; idx++)
            {
                if (_isUsedList[idx])
                {
                    continue;
                }
                _charas[idx].ChangeSprite(_unselectedCharaSprites[SelectIdxToCharaIdx(idx)]);
                _charas[idx].SetRotateZero();
            }

            _isSceneChanging = true;

            await UniTask.WaitForSeconds(0.8f);

            TadaLib.Scene.TransitionManager.Instance.StartTransition("GameModeSelect", 0.5f, 0.5f);
        }

        // キャラクター ID に変換
        // @memo: キャラクターセレクト画面のキャラの並びが ID と一致していないので変換が必要
        int SelectIdxToCharaIdx(int selectIdx)
        {
            return selectIdx switch
            {
                0 => 0,
                1 => 3,
                2 => 1,
                3 => 2,
                _ => throw new System.Exception()
            };
        }
        int CharaIdxToSelectIdx(int charaIdx)
        {
            return charaIdx switch
            {
                0 => 0,
                1 => 2,
                2 => 3,
                3 => 1,
                _ => throw new System.Exception()
            };
        }
        #endregion
    }
}