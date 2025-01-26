using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Ui;

namespace Scripts.Actor.Player
{
    /// <summary>
    /// CharaCtrl
    /// </summary>
    public class CharaCtrl
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            var charaIdx = CharaSelectUiManager.PlayerUseCharaIdList(_playerNumber);
            _body.sprite = CharacterManager.Instance.GetCharaImage(charaIdx);

            _body.transform.localPosition = _offsets[charaIdx];
        }

        private void Update()
        {
            
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        int _playerNumber = 0;

        [SerializeField]
        SpriteRenderer _body;

        [SerializeField]
        List<Vector3> _offsets;
        #endregion

        #region privateメソッド
        #endregion
    }
}