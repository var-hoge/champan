using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace TadaLib.Ui
{
    /// <summary>
    /// PlayerColorImageAutoSetter
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class PlayerColorImageAutoSetter
        : MonoBehaviour
    {
        void Start()
        {
            // @memo: App 以下へのアクセスが必要だから、TadaLib で管理すべきではない
            //        アクセスしたい要素を全部 TadaLib に持ってきたら OK だけど

            var playerIndex = _kind switch
            {
                Kind.WinnerPlayer => App.GameSequenceManager.WinnerPlayerIdx,
                Kind.Player0 => 0,
                Kind.Player1 => 1,
                Kind.Player2 => 2,
                Kind.Player3 => 3,
                _ => 0,
            };

            var idx = App.Cpu.CpuManager.Instance.IsCpu(playerIndex) ? 4 : playerIndex;
            GetComponent<UnityEngine.UI.Image>().SetSprite(_sprites[idx]);
        }

        enum Kind
        {
            WinnerPlayer,
            Player0,
            Player1,
            Player2,
            Player3,
        }

        [SerializeField]
        List<Sprite> _sprites;

        [SerializeField]
        Kind _kind;
    }
}