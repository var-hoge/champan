using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using UnityEngine.UIElements;
using DG.Tweening;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// CharaSelectFinishCheckerUnit
    /// </summary>
    public class CharaSelectFinishCheckerUnit
        : MonoBehaviour
    {
        public void SetDoorPos(Vector3 doorPos)
        {
            _doorPos = doorPos;
        }

        public void EnterTheDoor(Vector3 doorPos)
        {
            transform.DOMove(doorPos, 0.3f);
            transform.GetChild(1).DOLocalMoveY(0.2f, 0.2f).SetLoops(-1, LoopType.Yoyo);
            transform.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>().DOFade(0.0f, 0.8f).SetEase(Ease.InCirc);
        }

        public bool IsFinishReady { private set; get; }

        void Update()
        {
            IsFinishReady = IsInner();

            if (IsFinishReady && !_isEntered)
            {
                // 動けなくする
                GetComponent<Actor.Player.MoveCtrl>().enabled = false;
                GetComponent<TadaLib.ActionStd.StateMachine>().enabled = false;
                EnterTheDoor(_doorPos);

                _isEntered = true;
            }
        }

        [SerializeField]
        BoxCollider2D _collider;

        bool _isEntered = false;
        Vector3 _doorPos = Vector3.zero;

        bool IsInner()
        {
            var position = transform.position;

            // コライダーの中心とサイズを取得（ワールド座標）
            Vector2 worldCenter = _collider.transform.TransformPoint(_collider.offset);
            Vector2 worldSize = Vector2.Scale(_collider.size, _collider.transform.lossyScale);

            // 範囲の最小・最大を計算
            Vector2 min = worldCenter - worldSize * 0.5f;
            Vector2 max = worldCenter + worldSize * 0.5f;

            return (position.x >= min.x && position.x <= max.x &&
                    position.y >= min.y && position.y <= max.y);
        }
    }
}