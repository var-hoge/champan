using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace App.Ui.Common
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class PlayerIdUi
        : TadaLib.ProcSystem.BaseProc
        , TadaLib.ProcSystem.IProcPostMove
    {
        #region プロパティ
        #endregion

        #region メソッド
        void Start()
        {
            GetComponent<UnityEngine.UI.Image>().enabled = false;

            _isCpu = GameSequenceManager.Instance != null && Cpu.CpuManager.Instance.IsCpu(_playerNumber);

            if (_isCpu)
            {
                GetComponent<RectTransform>().localScale = GetComponent<RectTransform>().localScale * 0.85f;
            }
        }

        public void OnPostMove()
        {
            // 名前の解決はキャラの生成を待たない。
            //
            // 対戦シーンでは、席と CPU の判定が済むまでキャラが作られない。
            // その後ろに置くと、待っている間ずっと名前が出ず、
            // 待ち時間は台ごとに違うため、出る時期も揃わない。
            ApplyNamePlate();

            var player = TadaLib.ActionStd.PlayerManager.TryGetPlayer(_playerNumber);

            if (player == null || !player.activeSelf)
            {
                SetVisible(false);
                return;
            }

            // プレイヤーの頭上に移動させる
            var dataHolder = player.GetComponent<Actor.Player.DataHolder>();
            var isDummyValid = dataHolder.IsValidDummyPlayerPos;
            var playerPos = isDummyValid ? dataHolder.DummyPlayerPos : player.transform.position;

            // 落ちてから復帰用バブルに結び付くまでの間、キャラは画面外で待っている。
            // その位置を追うと、画面の端に UI だけが残って見える。
            if (!isDummyValid
                && playerPos.y <= Actor.Gimmick.RespawnBubble.PlayerSpawner.OutOfScreenPoint.y + 1.0f)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            var screenPos = Camera.main.WorldToScreenPoint(playerPos);

            var offsetY = 155.0f;
            var angle = 0.0f;

            if (_isCpu)
            {
                offsetY *= 0.85f;
            }

            if (!_isDummyPrev && isDummyValid)
            {
                // 風船中に UI の方向が変わらないようにする
                _angleRate = screenPos.x > Screen.width / 2 ? -1.0f : 1.0f;
            }
            _isDummyPrev = isDummyValid;

            // 画面内に収めるよう努力する
            if (isDummyValid)
            {
                offsetY = 84.0f;
                if (_isCpu)
                {
                    offsetY *= 0.85f;
                }
                if (screenPos.y + offsetY > Screen.height)
                {
                    var diff = screenPos.y + offsetY - Screen.height;
                    var rate = Mathf.Clamp01(diff / offsetY);
                    angle = TadaLib.Util.InterpUtil.Linier(0.0f, 60.0f, rate);
                    angle *= _angleRate;

                    offsetY = TadaLib.Util.InterpUtil.Linier(40.0f, offsetY, 1.0f - rate);
                }
            }

            _offsetY = TadaLib.Util.InterpUtil.Linier(_offsetY, offsetY, 0.1f, Time.deltaTime);

            var useOffsetY = _offsetY * (Screen.height / 1080.0f);

            GetComponent<RectTransform>().localEulerAngles = new Vector3(0.0f, 0.0f, -angle);
            GetComponent<RectTransform>().position = screenPos + useOffsetY * new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0.0f);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;
        [SerializeField]
        int _playerNumber = 0;
        float _angleRate = 1.0f;
        bool _isDummyPrev = false;
        float _offsetY = 170.0f;
        bool _isCpu = false;
        PlayerNamePlate _namePlate = null;

        /// <summary>
        /// 一度分かった表示名
        /// </summary>
        string _seatName = "";
        #endregion

        #region privateメソッド
        /// <summary>
        /// 頭上の表示を出し入れする
        ///
        /// 名札に差し替わっている場合もあるため、まとめて扱う。
        /// 元の絵だけを消しても、名札が残って浮いて見える。
        /// </summary>
        void SetVisible(bool isVisible)
        {
            GetComponent<UnityEngine.UI.Image>().enabled = isVisible;

            if (_namePlate != null)
            {
                _namePlate.SetVisible(isVisible);
            }
        }

        /// <summary>
        /// 名前の表示を反映する
        ///
        /// CPU 席は元の表示のままにする。
        /// 人間と同じ吹き出しにすると、CPU を控えめに見せている意図が崩れる。
        /// </summary>
        void ApplyNamePlate()
        {
            // CPU かどうかで判断しない。
            //
            // 対戦シーンでは、席の割り当てが済むまで人間の席も CPU 扱いのままになる。
            // それを見て決めると、確定するまで名前が出ず、
            // 確定の時期は台ごとに違うため、出る時期も揃わない。
            //
            // 席に名前が入っているかどうかだけで決めればよい。
            // CPU 席は誰も着いていないので、そもそも名前を持たない。
            var seatName = Network.SeatInput.GetSeatName(_playerNumber);

            // 一度分かった名前は覚えておく。
            // 席の情報は画面の切り替わりで一時的に途切れることがあり、
            // その間だけ元の番号に戻ると点滅して見える。
            if (seatName.Length > 0)
            {
                _seatName = seatName;
            }

            _namePlate ??= gameObject.AddComponent<PlayerNamePlate>();
            _namePlate.SetName(_seatName, _playerNumber);
        }
        #endregion
    }
}