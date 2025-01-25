using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace Scripts.Actor.Gimmick.CrownBubble
{
    /// <summary>
    /// CrownBubbleCtrl
    /// </summary>
    [RequireComponent(typeof(TadaLib.HitSystem.OwnerCtrl))]
    public class CrownBubbleCtrl
        : BaseProc
        , IProcUpdate
        , IProcMove
        , IProcPostMove
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _bubbleLayers = new List<BubbleLayer>();
            foreach (var data in _bubbleLayerData)
            {
                var bubbleLayer = Instantiate(_bubbleLayerPrefab, transform);
                bubbleLayer.Setup(data.Radius, _bubbleLayers.Count);
                _bubbleLayers.Add(bubbleLayer);
            }
            GetComponent<TadaLib.HitSystem.OwnerCtrl>().Owner.EditNodeRadius(0, _bubbleLayers[0].Radius * 1.5f);
        }
        #endregion

        #region IProcUpdate の実装
        public void OnUpdate()
        {
        }
        #endregion

        #region IProcMove の実装
        public void OnMove()
        {
        }
        #endregion

        #region IProcPostMove の実装
        public void OnPostMove()
        {
            if (TadaLib.HitSystem.OwnerCtrl.TryGetCollResults(gameObject, out var results))
            {
                foreach (var result in results)
                {
                    if (result.Tag == (int)TadaLib.HitSystem.TagKind.Player)
                    {
                        var playerObj = result.OpponentObj;

                        // 下からぶつかった場合はつぶれない
                        if (playerObj.TryGetComponent<Player.MoveCtrl>(out var comp))
                        {
                            if (comp.Velocity.y >= 0)
                            {
                                continue;
                            }
                        }
                        var diffPos = transform.position - playerObj.transform.position;
                        if (diffPos.y > 0)
                        {
                            continue;
                        }

                        if (_bubbleLayers.Count == 0)
                        {
                            GetCrown(playerObj);
                        }
                        else
                        {
                            Break(playerObj);
                        }
                    }
                }
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        BubbleLayer _bubbleLayerPrefab;

        [System.Serializable]
        class BubbleLayerData
        {
            public float Radius;
        }

        [SerializeField]
        List<BubbleLayerData> _bubbleLayerData;

        [SerializeField]
        float _blowPower = 12.0f;

        List<BubbleLayer> _bubbleLayers;
        #endregion

        #region privateメソッド
        void Break(GameObject breakPlayer)
        {
            Debug.Assert(_bubbleLayers.Count >= 1);

            var bubbleLayer = _bubbleLayers[0];

            // break one bubble layer
            bubbleLayer.OnBreak();

            // blow player
            var dirDiff = breakPlayer.transform.position - bubbleLayer.transform.position;
            dirDiff.z = 0.0f;
            var dirUnit = dirDiff.normalized;
            // x 軸方向の成分を強める
            dirUnit.x += Mathf.Sign(dirUnit.x) * 0.3f;
            dirUnit = dirUnit.normalized;
            if (dirUnit.sqrMagnitude < 0.0001f)
            {
                // 座標が一致していたなら適当な方向に飛ばす
                dirUnit = Vector3.right;
            }

            var moveCtrl = breakPlayer.GetComponent<Player.MoveCtrl>();
            moveCtrl.SetVelocityForce(dirUnit * _blowPower);
            moveCtrl.SetUncontrollableTime(0.3f);

            _bubbleLayers.RemoveAt(0);

            // set hit collider
            if (_bubbleLayers.Count != 0)
            {
                GetComponent<TadaLib.HitSystem.OwnerCtrl>().Owner.EditNodeRadius(0, _bubbleLayers[0].Radius * 1.5f);
            }
        }

        void GetCrown(GameObject getPlayer)
        {
            // TODO: implement clear staging
        }
        #endregion
    }
}