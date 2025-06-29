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
        public GameObject RentCrown(Vector3 pos, bool playAnim = false)
        {
            var obj = Instantiate(_crownTemplate.gameObject, transform);
            obj.gameObject.SetActive(true);
            obj.GetComponent<RectTransform>().position = pos;

            if (playAnim)
            {
                // @todo: アニメーション対応
            }

            return obj.gameObject;
        }

        public GameObject RentSlotMark(Vector3 pos)
        {
            var obj = Instantiate(_slotMarkTemplate.gameObject, transform);
            obj.gameObject.SetActive(true);
            obj.GetComponent<RectTransform>().position = pos;

            return obj.gameObject;
        }

        [SerializeField]
        UnityEngine.UI.Image _crownTemplate;

        [SerializeField]
        UnityEngine.UI.Image _slotMarkTemplate;
    }
}