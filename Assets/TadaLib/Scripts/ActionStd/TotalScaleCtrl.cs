using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using UniRx;

namespace TadaLib.ActionStd
{
    /// <summary>
    /// Scale制御
    /// </summary>
    public class TotalScaleCtrl
        : BaseProc
        , IProcUpdate
    {
        #region プロパティ
        /// <summary>
        /// 今の大きさ
        /// </summary>
        public Vector3 CurrentScale => transform.localScale;

        /// <summary>
        /// 今の見た目の大きさ
        /// </summary>
        public Vector3 CurrentViewScale
            => _transformForViewOffset != null ? _transformForViewOffset.localScale : Vector3.one;
        #endregion

        #region メソッド
        /// <summary>
        /// 大きさを外から与える
        ///
        /// 拡縮はどれもここを通って反映される。
        /// 計算のもとになる値がそろわない状況で、
        /// 他所で計算された結果だけを映したいときに使う。
        ///
        /// 効くのは呼ばれた次の一回だけ。
        /// 与え続けるのをやめれば、自前の計算に戻る。
        /// (状態として持つと、外から与える必要がなくなった後も
        ///  最後の値のまま固まってしまう)
        /// </summary>
        public void SetScaleForcibly(Vector3 scale, Vector3 viewScale)
        {
            _isForcedScale = true;
            _forcedScale = scale;
            _forcedViewScale = viewScale;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _scaleBase = transform.localScale;
        }
        #endregion

        #region TadaLib.ProcSystem.IProcUpdate の実装
        public void OnUpdate()
        {
            if (_isForcedScale)
            {
                // 与えられ続けている間だけ効かせる
                _isForcedScale = false;

                transform.localScale = _forcedScale;
                if (_transformForViewOffset != null)
                {
                    _transformForViewOffset.localScale = _forcedViewScale;
                }

                return;
            }

            var scale = _scaleBase;
            var viewScale = Vector3.one;
            var scaleChangers = GetComponents<IScaleChanger>();
            foreach (var changer in scaleChangers)
            {
                scale = Vector3.Scale(scale, changer.ScaleRate);
                viewScale = Vector3.Scale(viewScale, changer.ViewScaleRate);
            }

            transform.localScale = scale;
            if (_transformForViewOffset != null)
            {
                _transformForViewOffset.localScale = viewScale;
            }
        }
        #endregion

        #region privateメソッド
        #endregion

        #region privateフィールド
        [SerializeField]
        Transform _transformForViewOffset = null;
        Vector3 _scaleBase;

        bool _isForcedScale = false;
        Vector3 _forcedScale = Vector3.one;
        Vector3 _forcedViewScale = Vector3.one;
        #endregion
    }
}