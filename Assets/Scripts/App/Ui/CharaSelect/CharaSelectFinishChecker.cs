using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using KanKikuchi.AudioManager;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// CharaSelectFinishChecker
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CharaSelectFinishChecker
        : MonoBehaviour
    {
        void Update()
        {
            if (_isFinished)
            {
                return;
            }

            if (IsFinished())
            {
                SEManager.Instance.Play(SEPath.TRANSITION_TO_NEXT_SCREEN);
                foreach (var starParticle in _starParticles.GetComponentsInChildren<ParticleSystem>())
                {
                    starParticle.Play();
                }

                _isFinished = true;
                _manager.SceneChange();
            }
        }

        bool IsFinished()
        {
            var isFinished = false;
            for (int idx = 0; idx < _playerParents.childCount; ++idx)
            {
                var player = _playerParents.GetChild(idx);

                if (player.gameObject.activeSelf is false)
                {
                    continue;
                }

                var checkerUnit = player.GetComponent<CharaSelectFinishCheckerUnit>();
                if (checkerUnit == null)
                {
                    continue;
                }

                // (良くない)
                checkerUnit.SetDoorPos(_doorTrans.position);

                if (!checkerUnit.IsFinishReady)
                {
                    continue;
                }

                isFinished = true;
            }

            return isFinished;
        }

        [SerializeField]
        CharaSelectUiManager _manager;

        [SerializeField]
        Transform _playerParents;

        [SerializeField]
        Transform _doorTrans;

        [SerializeField]
        private GameObject _starParticles;

        bool _isFinished = false;
    }
}