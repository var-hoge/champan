using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;

namespace App.Ui.Main
{
    /// <summary>
    /// EmotionManager
    /// </summary>
    public class EmotionManager
        : BaseManagerProc<EmotionManager>
    {
        #region 定義
        public enum EmotionKind
        {
            Happy,
            Angry,
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        public void Spawn(Transform constraintTrans, EmotionKind kind, float waitDurationSec = 0.0f)
        {
            SpawnImpl(constraintTrans, kind, waitDurationSec).Forget();
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region private フィールド
        [SerializeField]
        EmotionObj _template;

        [SerializeField]
        List<Sprite> _happySprites;
        [SerializeField]
        List<Sprite> _angrySprites;

        Dictionary<Transform, Sprite> _historyHappy = new Dictionary<Transform, Sprite>();
        Dictionary<Transform, Sprite> _historyAngry = new Dictionary<Transform, Sprite>();
        #endregion

        #region private メソッド
        async UniTask SpawnImpl(Transform constraintTrans, EmotionKind kind, float waitDurationSec)
        {
            await UniTask.WaitForSeconds(waitDurationSec);

            // すでにある場合は削除する
            if (constraintTrans.childCount != 0)
            {
                for (int idx = constraintTrans.childCount - 1; idx >= 0; idx--)
                {
                    Destroy(constraintTrans.GetChild(idx).gameObject);
                }
            }

            var sprites = kind switch
            {
                EmotionKind.Happy => _happySprites,
                EmotionKind.Angry => _angrySprites,
                _ => throw new System.Exception()

            };

            var history = kind switch
            {
                EmotionKind.Happy => _historyHappy,
                EmotionKind.Angry => _historyAngry,
                _ => throw new System.Exception()
            };

            var max = history.ContainsKey(constraintTrans) ? sprites.Count - 1 : sprites.Count;
            var sprite = sprites[UnityEngine.Random.Range(0, max)];
            if (history.ContainsKey(constraintTrans))
            {
                if (sprite == history[constraintTrans])
                {
                    sprite = sprites[sprites.Count - 1];
                }
            }

            history[constraintTrans] = sprite;

            var obj = Instantiate(_template, constraintTrans.position, Quaternion.identity);
            obj.transform.SetParent(constraintTrans);
            obj.GetComponent<EmotionObj>().Init(sprite);
        }
        #endregion
    }
}