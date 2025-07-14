using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Player
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
        public void UpdateCharaSprite()
        {
            var playerIdx = GetComponent<DataHolder>().PlayerIdx;
            var charaIdx = Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(playerIdx);

            var sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
            _body.sprite = sprite;
            _shadow.sprite = sprite;

            _body.transform.localPosition = _offsets[charaIdx];

        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            var playerIdx = GetComponent<DataHolder>().PlayerIdx;
            var charaIdx = Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(playerIdx);

            var sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
            _body.sprite = sprite;
            _shadow.sprite = sprite;

            _body.transform.localPosition = _offsets[charaIdx];
        }

        private void Update()
        {
            
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        SpriteRenderer _body;

        [SerializeField]
        SpriteRenderer _shadow;

        [SerializeField]
        List<Vector3> _offsets;
        #endregion

        #region privateメソッド
        #endregion
    }
}