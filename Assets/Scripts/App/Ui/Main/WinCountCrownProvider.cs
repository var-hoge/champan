using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.Main
{
    /// <summary>
    /// WinCountCrownProvider
    /// </summary>
    public class WinCountCrownProvider
        : MonoBehaviour
    {
        public void RentCrown(Vector3 pos, bool playAnim = false)
        {
            var obj = Instantiate(_crownTemplate.gameObject, transform);
            obj.gameObject.SetActive(true);
            obj.GetComponent<RectTransform>().position = pos;

            if (playAnim)
            {
                // @todo: アニメーション対応
            }
        }

        public void RentSlotMark(Vector3 pos)
        {
            var obj = Instantiate(_slotMarkTemplate.gameObject, transform);
            obj.gameObject.SetActive(true);
            obj.GetComponent<RectTransform>().position = pos;
        }

        [SerializeField]
        UnityEngine.UI.Image _crownTemplate;

        [SerializeField]
        UnityEngine.UI.Image _slotMarkTemplate;
    }
}