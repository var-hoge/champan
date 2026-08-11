using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 他の台が動かしているものの座標を、滑らかにたどる
    ///
    /// 届いた座標を追いかける形にすると、届く間隔のばらつきが
    /// そのまま動きのムラになる。
    /// 少し遅らせて「既に届いている 2 点の間」を描けば、
    /// 間隔がばらついても動きは一定になる。
    /// 代わりに、遅らせたぶんだけ表示が遅れる。
    ///
    /// 対戦シーンとキャラセレクトで座標の配り方は違うが、
    /// たどり方は同じなのでここにまとめる。
    /// </summary>
    public class RemotePosSmoother
    {
        #region public メソッド
        /// <summary>
        /// 届いた座標を控えて、今描くべき座標を返す
        /// </summary>
        /// <param name="syncPos">配られてきた座標</param>
        /// <param name="currentPos">今の座標</param>
        /// <param name="nextPos">今描くべき座標</param>
        public bool TryFollow(Vector3 syncPos, Vector3 currentPos, out Vector3 nextPos)
        {
            nextPos = currentPos;

            Record(syncPos);

            if (!TryGetSmoothedPos(out var smoothedPos))
            {
                return false;
            }

            // 離れすぎたら控えを捨てて、そこへ合わせる (復帰やワープ時)
            if ((currentPos - smoothedPos).sqrMagnitude > PosSnapDistanceSqr)
            {
                Reset(syncPos);
                smoothedPos = syncPos;
            }

            nextPos = smoothedPos;
            return true;
        }

        /// <summary>
        /// 控えを捨てて、その座標から始め直す
        /// </summary>
        public void Reset(Vector3 syncPos)
        {
            _samples.Clear();
            _samples.Add(new Sample
            {
                Position = syncPos,
                Time = Time.unscaledTime,
            });
        }

        #endregion

        #region private メソッド
        /// <summary>
        /// 配られてきた座標を、届いた時刻とともに控える
        /// </summary>
        void Record(Vector3 syncPos)
        {
            // 同じ座標を控えると、その間は止まって見えてしまう
            if (_samples.Count > 0 && _samples[_samples.Count - 1].Position == syncPos)
            {
                return;
            }

            _samples.Add(new Sample
            {
                Position = syncPos,
                Time = Time.unscaledTime,
            });

            while (_samples.Count > SampleCountMax)
            {
                _samples.RemoveAt(0);
            }
        }

        /// <summary>
        /// 少し前の時点の座標を、控えた座標の間から求める
        /// </summary>
        bool TryGetSmoothedPos(out Vector3 result)
        {
            result = Vector3.zero;

            if (_samples.Count == 0)
            {
                return false;
            }

            if (_samples.Count == 1)
            {
                result = _samples[0].Position;
                return true;
            }

            var renderTime = Time.unscaledTime - InterpolationDelaySec;

            // 挟み込む 2 点を探す
            for (int idx = _samples.Count - 1; idx >= 1; --idx)
            {
                var to = _samples[idx];
                var from = _samples[idx - 1];

                if (renderTime > to.Time || renderTime < from.Time)
                {
                    continue;
                }

                var span = to.Time - from.Time;
                var rate = span > 0.0f ? (renderTime - from.Time) / span : 1.0f;

                result = Vector3.Lerp(from.Position, to.Position, rate);
                return true;
            }

            // 描きたい時点が最後の座標より新しい = 次がまだ届いていない。
            // 少しだけ先を推測して間を持たせる。
            var last = _samples[_samples.Count - 1];
            var prev = _samples[_samples.Count - 2];

            var elapsed = renderTime - last.Time;
            if (elapsed <= 0.0f)
            {
                result = last.Position;
                return true;
            }

            var interval = last.Time - prev.Time;
            var velocity = interval > 0.0f
                ? (last.Position - prev.Position) / interval
                : Vector3.zero;

            // 推測できる長さは、横と縦で分ける。
            //
            // 横は走っている間ずっと同じ速さなので、少し先を推測してもよく当たる。
            // 縦は落下から着地へ急に変わるため、落ちる速さのまま長く推測すると
            // 地面をすり抜けて埋まってしまう。
            // 権威を持たない側では当たり判定を止めているので、誰も押し戻してくれない。
            result = last.Position + new Vector3(
                GetPredictedOffset(velocity.x, elapsed, PredictSecMaxX),
                GetPredictedOffset(velocity.y, elapsed, PredictSecMaxY),
                0.0f);

            return true;
        }

        /// <summary>
        /// 次が届かないときに、最後に届いた座標からどれだけ先へ進めるかを求める
        ///
        /// 上限まで進めたあとは、進めたぶんを戻していく。
        ///
        /// 止まっているものは座標が送られてこないため、
        /// 進めたままにすると、ずれたまま戻らなくなる。
        /// 着地して立ち止まったキャラが、動き出すまで地面に埋まり続けていた。
        /// </summary>
        static float GetPredictedOffset(float velocity, float elapsed, float predictSecMax)
        {
            var predictSec = Mathf.Min(elapsed, predictSecMax);

            // 上限を超えても届かないままなら、進めたぶんを戻す
            var overSec = elapsed - predictSecMax;
            if (overSec > 0.0f)
            {
                predictSec *= 1.0f - Mathf.Clamp01(overSec / PredictReturnSec);
            }

            return velocity * predictSec;
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// どれだけ遅らせて描くか
        ///
        /// 座標が届く間隔ぶんの余裕を持たせる。
        /// 短くすると表示は新しくなるが、間に合わずに推測へ回る回数が増える。
        /// </summary>
        const float InterpolationDelaySec = 3.0f / 60.0f;

        /// <summary>
        /// 次が届かないときに横へ先を推測する上限
        /// </summary>
        const float PredictSecMaxX = 2.0f / 60.0f;

        /// <summary>
        /// 次が届かないときに縦へ先を推測する上限
        ///
        /// 着地で急に止まるため、横より短くする
        /// </summary>
        const float PredictSecMaxY = 1.0f / 60.0f;

        /// <summary>
        /// 進めたぶんを、最後に届いた座標へ戻すのにかける時間
        /// </summary>
        const float PredictReturnSec = 4.0f / 60.0f;

        /// <summary>
        /// 控えておく座標の数
        /// </summary>
        const int SampleCountMax = 8;

        /// <summary>
        /// これ以上離れたら、補間せずに合わせる
        /// </summary>
        const float PosSnapDistanceSqr = 25.0f;

        struct Sample
        {
            public Vector3 Position;
            public float Time;
        }

        readonly System.Collections.Generic.List<Sample> _samples = new();
        #endregion
    }
}
