using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace TadaLib.Window
{
    /// <summary>
    /// AspectAutoResizer
    /// </summary>
    public class AspectAutoResizer
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
#if UNITY_EDITOR
            return;
#endif
            EnforceAspectRatio(forceFullScreen: true);
        }

        void Update()
        {
#if UNITY_EDITOR
            return;
#endif
            _timer.Advance(Time.deltaTime);

            if (_timer.IsTimout is false)
            {
                return;
            }
            if (_isFullScreen != Screen.fullScreen)
            {
                EnforceAspectRatio();
            }
        }
        #endregion

        #region privateメソッド
        void EnforceAspectRatio(bool forceFullScreen = false)
        {
            Vector2Int aspectRatio = new(16, 9);

            Vector2Int size = default;
            float currentRatio = (float)Screen.width / Screen.height;
            float ratioGoal = (float)aspectRatio.x / aspectRatio.y;
            if (currentRatio > ratioGoal)
            {
                size.y = Screen.height;
                size.x = (int)(Screen.height * ratioGoal);
            }
            else
            {
                size.y = (int)(Screen.width / ratioGoal);
                size.x = Screen.width;
            }

            _isFullScreen = Screen.fullScreen;
            if (forceFullScreen)
            {
                _isFullScreen = true;
            }
            Screen.SetResolution(size.x, size.y, _isFullScreen);
        }
        #endregion

        #region privateフィールド
        bool _isFullScreen = true;
        Util.Timer _timer = new Util.Timer(1.0f);
        #endregion
    }
}