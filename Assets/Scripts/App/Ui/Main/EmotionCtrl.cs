using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App.Actor.Player;
using App.Actor;

namespace App.Ui.Main
{
    /// <summary>
    /// EmotionCtrl
    /// </summary>
    public class EmotionCtrl
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void NotifyHitCrown()
        {
            //if (Actor.Gimmick.Crown.Manager.Instance.LastCrownRidePlayerIdx == GetComponent<DataHolder>().PlayerIdx)
            //{
            //    EmotionManager.Instance.Spawn(_emotionRoot, EmotionManager.EmotionKind.Happy);
            //}

            if (Time.time - _latesetHitCrownTime < _happyTriggerIntervalSec)
            {
                EmotionManager.Instance.Spawn(_emotionRoot, EmotionManager.EmotionKind.Happy);
                _latesetHitCrownTime = 0.0f; // 連続で出ないようにする
                return;
            }
            _latesetHitCrownTime = Time.time;
        }

        public void NotifyStepedOn()
        {
            if (Time.time - _latesetStepedOnTime < _angryTriggerIntervalSec)
            {
                EmotionManager.Instance.Spawn(_emotionRoot, EmotionManager.EmotionKind.Angry);
                _latesetStepedOnTime = 0.0f; // 連続で出ないようにする
                return;
            }

            var playerIdx = GetComponent<DataHolder>().PlayerIdx;
            var charaIdx = Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(playerIdx);
            _meshRoot.sprite = CharacterManager.Instance.GetSadCharaImage(charaIdx);
            _sadSpriteTimer = _sadSpriteDurationSec;
            _latesetStepedOnTime = Time.time;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }

        void Update()
        {
            if (_sadSpriteTimer > 0.0f)
            {
                _sadSpriteTimer -= Time.deltaTime;
                if (_sadSpriteTimer <= 0.0f)
                {
                    var playerIdx = GetComponent<DataHolder>().PlayerIdx;
                    var charaIdx = Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(playerIdx);
                    _meshRoot.sprite = CharacterManager.Instance.GetCharaImage(charaIdx);
                }
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Transform _emotionRoot;

        [SerializeField]
        SpriteRenderer _meshRoot;

        [SerializeField]
        float _angryTriggerIntervalSec = 5.0f;

        [SerializeField]
        float _happyTriggerIntervalSec = 3.0f;

        [SerializeField]
        float _sadSpriteDurationSec = 3.0f;

        float _sadSpriteTimer = 0.0f;
        float _latesetStepedOnTime = -10.0f;
        float _latesetHitCrownTime = -10.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}