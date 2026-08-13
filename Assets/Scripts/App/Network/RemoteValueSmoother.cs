using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 他の台が動かしているものの値を、滑らかにたどる
    ///
    /// 届いた値を追いかける形にすると、届く間隔のばらつきが
    /// そのまま動きのムラになる。
    /// 少し遅らせて「既に届いている 2 点の間」を描けば、
    /// 間隔がばらついても動きは一定になる。
    /// 代わりに、遅らせたぶんだけ表示が遅れる。
    ///
    /// 座標にも拡縮にも使う。
    /// 配り方は場面ごとに違うが、たどり方は同じなのでここにまとめる。
    /// 別々に書くと、片方だけ直して食い違う。
    /// </summary>
    public class RemoteValueSmoother
    {
        #region メソッド
        /// <param name="predictSecMaxX">次が届かないとき、x へ先を推測する上限</param>
        /// <param name="predictSecMaxY">次が届かないとき、y へ先を推測する上限</param>
        /// <param name="snapDistanceSqr">これ以上離れたら、補間せずに合わせる</param>
        public RemoteValueSmoother(
            float predictSecMaxX,
            float predictSecMaxY,
            float snapDistanceSqr)
        {
            _predictSecMaxX = predictSecMaxX;
            _predictSecMaxY = predictSecMaxY;
            _snapDistanceSqr = snapDistanceSqr;
        }

        /// <summary>
        /// 届いた値を控えて、今描くべき値を返す
        /// </summary>
        public bool TryFollow(Vector3 syncValue, Vector3 currentValue, out Vector3 nextValue)
        {
            nextValue = currentValue;

            Record(syncValue);

            if (!TryGetSmoothed(out var smoothed))
            {
                return false;
            }

            // 離れすぎたら控えを捨てて、そこへ合わせる (復帰やワープ時)
            if ((currentValue - smoothed).sqrMagnitude > _snapDistanceSqr)
            {
                Reset(syncValue);
                smoothed = syncValue;
            }

            nextValue = smoothed;
            return true;
        }

        /// <summary>
        /// 控えを捨てて、その値から始め直す
        /// </summary>
        public void Reset(Vector3 syncValue)
        {
            _samples.Clear();
            _samples.Add(new Sample
            {
                Value = syncValue,
                Time = Time.unscaledTime,
            });
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 配られてきた値を、届いた時刻とともに控える
        /// </summary>
        void Record(Vector3 syncValue)
        {
            // 同じ値を控えると、その間は止まって見えてしまう
            if (_samples.Count > 0 && _samples[_samples.Count - 1].Value == syncValue)
            {
                return;
            }

            var now = Time.unscaledTime;

            RecordArrivalInterval(now);

            _samples.Add(new Sample
            {
                Value = syncValue,
                Time = now,
            });

            while (_samples.Count > SampleCountMax)
            {
                _samples.RemoveAt(0);
            }
        }

        /// <summary>
        /// 届く間隔を控える
        ///
        /// どれだけ遅らせるかを、この間隔から決める。
        /// </summary>
        void RecordArrivalInterval(float now)
        {
            if (_latestArrivalTime > 0.0f)
            {
                var interval = now - _latestArrivalTime;

                // 一番空いた間隔を覚えておく。
                //
                // 直近の数件から最大を取る形にすると、
                // 大きい値が窓から外れるたびに目標が跳ね、
                // 遅らせる量が動き続けて、かえって進みがムラになる。
                //
                // 空いたときは即座に合わせ、詰まってきたらゆっくり戻す。
                _arrivalIntervalPeak = interval > _arrivalIntervalPeak
                    ? interval
                    : Mathf.Lerp(_arrivalIntervalPeak, interval, IntervalPeakDecayRate);
            }

            _latestArrivalTime = now;
        }

        /// <summary>
        /// どれだけ遅らせて描くかを決める
        ///
        /// 固定にすると、届く間隔が場面で違うため合わない。
        /// 短すぎると、描きたい時点が最新の値より先に行き、
        /// 補間ではなく推測に回り続けてカクついて見える。
        ///
        /// 直近で一番空いた間隔をもとに、それを越える余裕を持たせる。
        /// </summary>
        float GetDelaySec()
        {
            var target = Mathf.Clamp(
                _arrivalIntervalPeak * DelayRate, DelaySecMin, DelaySecMax);

            // 遅らせる量が動いている間は、描く時点の進み方が
            // 実時間とずれるため、そのぶん進みがムラになる。
            // 近ければ動かさない。
            if (Mathf.Abs(target - _delaySec) > DelayDeadZoneSec)
            {
                _delaySec = Mathf.MoveTowards(
                    _delaySec, target, DelayAdjustSpeed * Time.unscaledDeltaTime);
            }

            return _delaySec;
        }

        /// <summary>
        /// 少し前の時点の値を、控えた値の間から求める
        /// </summary>
        bool TryGetSmoothed(out Vector3 result)
        {
            result = Vector3.zero;

            if (_samples.Count == 0)
            {
                return false;
            }

            if (_samples.Count == 1)
            {
                result = _samples[0].Value;
                return true;
            }

            var renderTime = Time.unscaledTime - GetDelaySec();

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

                result = Vector3.Lerp(from.Value, to.Value, rate);
                return true;
            }

            // 描きたい時点が最後の値より新しい = 次がまだ届いていない。
            // 少しだけ先を推測して間を持たせる。
            var last = _samples[_samples.Count - 1];
            var prev = _samples[_samples.Count - 2];

            var elapsed = renderTime - last.Time;
            if (elapsed <= 0.0f)
            {
                result = last.Value;
                return true;
            }

            var interval = last.Time - prev.Time;
            var velocity = interval > 0.0f
                ? (last.Value - prev.Value) / interval
                : Vector3.zero;

            result = last.Value + new Vector3(
                GetPredictedOffset(velocity.x, elapsed, _predictSecMaxX),
                GetPredictedOffset(velocity.y, elapsed, _predictSecMaxY),
                0.0f);

            return true;
        }

        /// <summary>
        /// 次が届かないときに、最後に届いた値からどれだけ先へ進めるかを求める
        ///
        /// 上限まで進めたあとは、進めたぶんを戻していく。
        ///
        /// 止まっているものは値が送られてこないため、
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
        /// 遅らせる量を、届く間隔の何倍にするか
        ///
        /// 1 倍では間隔のばらつきを吸収できず、たびたび足りなくなる。
        /// </summary>
        const float DelayRate = 1.5f;

        /// <summary>
        /// 遅らせる量の下限 (毎フレーム届く場面向け)
        /// </summary>
        const float DelaySecMin = 3.0f / 60.0f;

        /// <summary>
        /// 遅らせる量の上限
        ///
        /// これ以上遅らせると、相手の動きが目に見えて遅れる。
        /// 実際に 9 フレーム (150ms) まで許したところ、遅れのほうが気になった。
        ///
        /// 届く間隔が広いときは、遅らせるより送る回数を増やして解く。
        /// (毎ティック送る設定にしてある)
        /// </summary>
        const float DelaySecMax = 5.0f / 60.0f;

        /// <summary>
        /// 遅らせる量を寄せる速さ (秒あたり)
        /// </summary>
        const float DelayAdjustSpeed = 0.2f;

        /// <summary>
        /// これ以内なら、遅らせる量を動かさない
        ///
        /// 動かしている間は進みがムラになるため、常に追い続けない。
        /// </summary>
        const float DelayDeadZoneSec = 1.0f / 60.0f;

        /// <summary>
        /// 詰まってきたときに、覚えた間隔を戻す速さ (1 回あたり)
        ///
        /// 小さくするほど、たまたま空いた 1 回に引きずられ続ける。
        /// 大きくすると、目標が動いて進みがムラになる。
        /// </summary>
        const float IntervalPeakDecayRate = 0.02f;

        /// <summary>
        /// 進めたぶんを、最後に届いた値へ戻すのにかける時間
        /// </summary>
        const float PredictReturnSec = 4.0f / 60.0f;

        /// <summary>
        /// 控えておく値の数
        /// </summary>
        const int SampleCountMax = 16;

        struct Sample
        {
            public Vector3 Value;
            public float Time;
        }

        readonly System.Collections.Generic.List<Sample> _samples = new();

        readonly float _predictSecMaxX;
        readonly float _predictSecMaxY;
        readonly float _snapDistanceSqr;

        /// <summary>
        /// 一番空いた届く間隔
        /// </summary>
        float _arrivalIntervalPeak = 0.0f;
        float _latestArrivalTime = 0.0f;

        float _delaySec = DelaySecMin;
        #endregion
    }
}
