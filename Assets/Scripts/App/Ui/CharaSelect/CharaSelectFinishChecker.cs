using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

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
                foreach (var starParticle in _starParticles.GetComponentsInChildren<ParticleSystem>())
                {
                    starParticle.Play();
                }

                _isFinished = true;
                StartCoroutine(StartTransition());
            }

            IEnumerator StartTransition()
            {
                yield return new WaitForSeconds(0.0f);
                _ = _manager.SceneChange();
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

                if (!checkerUnit.IsFinishReady)
                {
                    return false;
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
        private GameObject _starParticles;

        bool _isFinished = false;
    }
}