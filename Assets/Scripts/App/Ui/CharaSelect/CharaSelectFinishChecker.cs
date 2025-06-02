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
                _isFinished = true;
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.5f, 0.5f);
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
        Transform _playerParents;

        bool _isFinished = false;
    }
}