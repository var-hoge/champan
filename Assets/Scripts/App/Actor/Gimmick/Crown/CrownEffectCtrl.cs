using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Gimmick.Crown
{
    /// <summary>
    /// CrownEffectCtrl
    /// </summary>
    public class CrownEffectCtrl
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void SetRemainHitCount(int count)
        {
            // @todo: 整理
            // エフェクトの出るタイミングを調整
            if (Manager.Instance.InitShieldValue == count && Manager.Instance.InitShieldValue == 9)
            {
                count++;
            }

            // 残りヒット数を考慮せず、呼ばれるたびにエフェクトを追加する
            _curAppearedEffectCount = count switch
            {
                var cnt when cnt >= 10 => 1,
                var cnt when cnt >= 9 => 2,
                var cnt when cnt >= 8 => 2,
                var cnt when cnt >= 7 => 2,
                var cnt when cnt >= 6 => 3,
                var cnt when cnt >= 5 => 3,
                var cnt when cnt >= 4 => 4,
                var cnt when cnt >= 3 => 4,
                var cnt when cnt >= 2 => 5,
                var cnt when cnt >= 0 => 5,
                _ => 1,
            };

            var speedRate = count switch
            {
                var cnt when cnt >= 10 => 0.4f,
                var cnt when cnt >= 9 => 0.6f,
                var cnt when cnt >= 8 => 0.8f,
                var cnt when cnt >= 7 => 1.0f,
                var cnt when cnt >= 6 => 1.2f,
                var cnt when cnt >= 5 => 1.4f,
                var cnt when cnt >= 4 => 1.6f,
                var cnt when cnt >= 3 => 1.8f,
                var cnt when cnt >= 2 => 2.0f,
                var cnt when cnt >= 0 => 2.0f,
                _ => 1.0f,
            };

            for (int idx = 0; idx < _curAppearedEffectCount; ++idx)
            {
                _effects[idx].enabled = true;
                _effects[idx].GetComponent<SimpleRotator>().SpeedRate = speedRate;
            }

            // 一つ目のエフェクトだけは例外的に無効化する
            _effects[0].enabled = false;
 
            //var burstCount = count switch
            //{
            //    var cnt when cnt >= 10 => 0,
            //    var cnt when cnt >= 9 => 0,
            //    var cnt when cnt >= 8 => 1,
            //    var cnt when cnt >= 7 => 1,
            //    var cnt when cnt >= 6 => 2,
            //    var cnt when cnt >= 5 => 2,
            //    var cnt when cnt >= 4 => 2,
            //    var cnt when cnt >= 3 => 2,
            //    var cnt when cnt >= 2 => 3,
            //    var cnt when cnt >= 1 => 3,
            //    _ => 0,
            //};

            //if (burstCount == 0)
            //{
            //    return;
            //}

            //foreach (var particleEffect in _particleEffects)
            //{
            //    var emission = particleEffect.emission;
            //    var bursts = new ParticleSystem.Burst[emission.burstCount];
            //    bursts[0].count = burstCount - 1;
            //    emission.SetBursts(bursts);

            //    particleEffect.Play();
            //}

            {
                var emission = _particleEffectNew.emission;

                var rateOverTime = count switch
                {
                    var cnt when cnt >= 10 => 00.0f,
                    var cnt when cnt >= 9 => 0.0f,
                    var cnt when cnt >= 8 => 4.0f,
                    var cnt when cnt >= 7 => 8.0f,
                    var cnt when cnt >= 6 => 12.0f,
                    var cnt when cnt >= 5 => 16.0f,
                    var cnt when cnt >= 4 => 20.0f,
                    var cnt when cnt >= 3 => 24.0f,
                    var cnt when cnt >= 2 => 28.0f,
                    var cnt when cnt >= 0 => 32.0f,
                    _ => 0,
                };

                emission.rateOverTime = rateOverTime;

                _particleEffectNew.Play();
            }
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        List<SpriteRenderer> _effects;

        [SerializeField]
        List<ParticleSystem> _particleEffects;

        [SerializeField]
        GameObject _particleEffectRoot;

        [SerializeField]
        ParticleSystem _particleEffectNew;

        static int _curAppearedEffectCount = 0;
        #endregion

        #region privateメソッド
        #endregion
    }
}