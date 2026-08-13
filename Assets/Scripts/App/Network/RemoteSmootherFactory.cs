namespace App.Network
{
    /// <summary>
    /// 値のたどり方の設定を配る
    ///
    /// 座標も拡縮も、対戦シーンとキャラセレクトの両方で使う。
    /// 設定を呼び出し側それぞれに書くと、場面ごとに挙動が食い違う。
    /// ここから配って、どこでも同じ動きにする。
    /// </summary>
    public static class RemoteSmootherFactory
    {
        #region メソッド
        /// <summary>
        /// 座標をたどるもの
        /// </summary>
        public static RemoteValueSmoother CreateForPosition()
        {
            return new RemoteValueSmoother(
                predictSecMaxX: PositionPredictSecMaxX,
                predictSecMaxY: PositionPredictSecMaxY,
                snapDistanceSqr: PositionSnapDistanceSqr);
        }

        /// <summary>
        /// 拡縮をたどるもの
        /// </summary>
        public static RemoteValueSmoother CreateForScale()
        {
            return new RemoteValueSmoother(
                predictSecMaxX: ScalePredictSecMax,
                predictSecMaxY: ScalePredictSecMax,
                snapDistanceSqr: ScaleSnapDistanceSqr);
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 横は走っている間ずっと同じ速さなので、少し先を推測してもよく当たる
        /// </summary>
        const float PositionPredictSecMaxX = 2.0f / 60.0f;

        /// <summary>
        /// 縦は落下から着地へ急に変わるため、長く推測すると地面へめり込む
        /// </summary>
        const float PositionPredictSecMaxY = 1.0f / 60.0f;

        /// <summary>
        /// これ以上離れたら、補間せずに合わせる (復帰やワープ)
        /// </summary>
        const float PositionSnapDistanceSqr = 25.0f;

        /// <summary>
        /// 拡縮は上下左右で性質が変わらないため、縦横を分けない
        /// </summary>
        const float ScalePredictSecMax = 2.0f / 60.0f;

        /// <summary>
        /// 拡縮の飛びはごく小さい。
        /// 大きく変わるのは踏まれた瞬間などで、そこは追わずに合わせる
        /// </summary>
        const float ScaleSnapDistanceSqr = 1.0f;
        #endregion
    }
}
