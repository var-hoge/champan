using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace App.Ui.Main
{
    /// <summary>
    /// WinCountCrownProvider
    /// </summary>
    public class WinCountCrownProvider
        : MonoBehaviour
    {
        public GameObject RentCrown(Vector3 pos, bool playAnim = false, float animRate = 1.0f)
        {
            var obj = Instantiate(_crownTemplate.gameObject, transform);
            obj.gameObject.SetActive(true);

            var rectTrasnform = obj.GetComponent<RectTransform>();
            rectTrasnform.position = pos;

            if (playAnim)
            {
                var screenRate = Screen.height / 1080.0f;
                rectTrasnform.position = new Vector3(pos.x + _crownAnimPosOffsetX * screenRate, _crownAnimStartPosY * screenRate, pos.z);
                var angles = rectTrasnform.localEulerAngles;
                angles.z = _crownAnimStartDegZ;
                rectTrasnform.localEulerAngles = angles;
                var moveY = pos.y - _crownAnimStartPosY * screenRate;
                var durationSec = (Mathf.Abs(moveY) / screenRate / 4000.0f + 0.4f) * _crownAnimDurationSecRate * animRate;
                var dir = pos - rectTrasnform.position;

                var seq = DOTween.Sequence();
                seq.Append(rectTrasnform.DOMove(pos + dir.normalized * _crownAnimEndPosOffset * screenRate, durationSec).SetEase(_crownAnimEase));
                seq.Join(rectTrasnform.DOLocalRotate(new Vector3(0.0f, 0.0f, -360.0f), durationSec, RotateMode.FastBeyond360));
                seq.Append(rectTrasnform.DOMove(pos, _crownAnimBackDurationSec * animRate).SetEase(_crownAnimBackEase));
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

        [SerializeField]
        float _crownAnimPosOffsetX = -100.0f;

        [SerializeField]
        float _crownAnimEndPosOffset = 20.0f;

        [SerializeField]
        float _crownAnimStartPosY = 0.0f;

        [SerializeField]
        float _crownAnimStartDegZ = 60.0f;

        [SerializeField]
        Ease _crownAnimEase = Ease.Linear;

        [SerializeField]
        Ease _crownAnimBackEase = Ease.Linear;

        [SerializeField]
        float _crownAnimDurationSecRate = 1.0f;
        [SerializeField]
        float _crownAnimBackDurationSec = 0.3f;
    }
}