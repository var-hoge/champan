using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using UnityEngine.UIElements;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// CharaSelectFinishCheckerUnit
    /// </summary>
    public class CharaSelectFinishCheckerUnit
        : MonoBehaviour
    {
        public bool IsFinishReady { private set; get; }

        void Update()
        {
            IsFinishReady = IsInner();
        }

        [SerializeField]
        BoxCollider2D _collider;

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