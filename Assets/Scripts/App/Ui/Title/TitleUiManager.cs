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
using KanKikuchi.AudioManager;
using UnityEngine.Video;
using TadaLib.Ui;
using static UnityEditor.PlayerSettings;

namespace App.Ui.Title
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class TitleUiManager
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        private void Start()
        {
            Staging().Forget();
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        VideoPlayer _introPlayer;
        [SerializeField]
        Canvas _introCanvas;
        [SerializeField]
        Canvas _mainCanvas;

        [SerializeField]
        List<Button> _items;

        int _selectedIdx = 0;

        public static bool IsBackFromCredit = false;
        #endregion

        #region privateメソッド
        public async UniTask Staging()
        {
            if (IsBackFromCredit)
            {
                AppearLogo(isImmediate: true);
                IsBackFromCredit = false;
            }
            else
            {
                await PlayIntro();

                await AppearLogo(isImmediate: false);

                BGMManager.Instance.Play(BGMPath.TITLE_SCREEN);
            }
            // BGMの再生

            // 選択
            await SelectItem();
        }

        async UniTask PlayIntro()
        {
            // UI が動画を上書きしてしまったので、再生終了まで非表示にする
            _mainCanvas.gameObject.SetActive(false);

            _introCanvas.gameObject.SetActive(true);

            // 動画準備開始
            _introPlayer.Prepare();

            // 動画再生のために、各種システムが安定化するまで待つ
            await UniTask.WaitForSeconds(0.5f);

            var isVideoFinished = false;

            _introPlayer.loopPointReached += (vp) =>
            {
                isVideoFinished = true;
            };

            await UniTask.WaitUntil(() => _introPlayer.isPrepared);

            _introPlayer.frame = 0; // 明示的に最初から再生する
            _introPlayer.Play();

            // 動画が終わるまで待つ
            var lastClickedTime = float.MinValue;
            while (true)
            {
                if (isVideoFinished)
                {
                    break;
                }

                // A ボタンが連続で二回押されたら終了
                var inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(0);
                if (inputProxy.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                {
                    if (Time.time - lastClickedTime < 0.5f)
                    {
                        _introPlayer.Stop();
                        break;
                    }

                    lastClickedTime = Time.time;
                }

                await UniTask.Yield();
            }

            _introCanvas.gameObject.SetActive(false);
        }

        async UniTask AppearLogo(bool isImmediate)
        {
            _mainCanvas.gameObject.SetActive(true);
            var main = GameObject.Find("Main").transform;

            // パンのアニメーション
            var charInfos = new (string name, Vector2 pos, float moveAmount)[]
            {
                ("Chara1", new(-701, -294), 30f),
                ("Chara2", new(704, -341), 30f),
                ("Chara3", new(543, -38), 20f),
                ("Chara4", new(-428, -66), 20f),
            };

            void MainYoyo()
            {
                main.GetComponent<RectTransform>()
                    .DOAnchorPos(new(-67, 89), 2f)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            void CharYoyo(Vector3 startPos, Vector3 anchorPos, float moveAmount, RectTransform rt)
            {
                var moveVec = (startPos - anchorPos).normalized * moveAmount;
                rt.DOAnchorPos(moveVec + anchorPos, 2f)
                  .SetEase(Ease.Linear)
                  .SetLoops(-1, LoopType.Yoyo);
            }

            if (isImmediate)
            {
                main.localScale = Vector3.one;
                MainYoyo();

                var titleBgmask = FindAnyObjectByType<TitleBgMask>();
                titleBgmask.SetScale(new Vector3(1.21f, 1.25f, 1.5f));

                foreach (var (name, pos, moveAmount) in charInfos)
                {
                    var rt = GameObject.Find(name).GetComponent<RectTransform>();
                    var startPos = rt.anchoredPosition;
                    rt.anchoredPosition = pos;
                    CharYoyo(startPos, pos, moveAmount, rt);
                }
            }
            else
            {
                // ロゴのアニメーション
                main.DOScale(Vector3.one, 0.6f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => MainYoyo());

                await UniTask.WaitForSeconds(0.1f);

                // 「チャン・ピョン・パン！」の再生
                SEManager.Instance.Play(SEPath.TITLE_SCREEN_04);

                await UniTask.WaitForSeconds(0.2f);

                // 背景のアニメーション
                var titleBgmask = FindAnyObjectByType<TitleBgMask>();
                DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, new Vector3(1.21f, 1.25f, 1.5f), 0.5f);

                await UniTask.WaitForSeconds(0.4f);

                foreach (var (name, pos, moveAmount) in charInfos)
                {
                    var rt = GameObject.Find(name).GetComponent<RectTransform>();
                    var startPos = rt.anchoredPosition;
                    rt.DOAnchorPos(pos, 0.8f)
                      .SetEase(Ease.OutBack)
                      .OnComplete(() => CharYoyo(startPos, pos, moveAmount, rt));
                }

                await UniTask.WaitForSeconds(1.2f);
            }
        }

        async UniTask SelectItem()
        {
            // 項目生成

            //var _selectedIdx = 0;

            for (int idx = 0; idx < _items.Count; ++idx)
            {
                if (_selectedIdx == idx)
                {
                    _items[idx].OnSelected();
                }
                else
                {
                    _items[idx].OnUnselected();
                }

                _items[idx].GetComponent<RectTransform>().DOScale(Vector3.one, 0.15f);
            }

            await UniTask.WaitForSeconds(0.2f);

            var inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(0);

            while (true)
            {
                // 決定優先
                if (inputProxy.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                {
                    break;
                }

                // 次点で移動
                if (inputProxy.AxisTrigger(TadaLib.Input.AxisCode.Vertical, out var isPositive))
                {
                    // 移動
                    var idxPrev = _selectedIdx;
                    _selectedIdx = (_selectedIdx + (isPositive ? 1 : -1) + _items.Count) % _items.Count;

                    _items[idxPrev].OnUnselected();
                    _items[_selectedIdx].OnSelected();

                    // SE 再生
                    SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
                }

                await UniTask.Yield();
            }

            // この時点で決定確定

            // SE 再生
            SEManager.Instance.Play(SEPath.MENU_VALIDATION);

            if (_selectedIdx == 0)
            {
                async UniTask PreUnload()
                {
                    var titleBgmask = FindAnyObjectByType<TitleBgMask>();
                    DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, new Vector3(0.0f, 0.0f, 0.0f), 0.5f);

                    await UniTask.WaitForSeconds(0.5f);
                }

                TadaLib.Scene.TransitionManager.Instance.StartTransitionAdvanced(
                    nextScene: "CharaSelect",
                    preUnloadFunc: PreUnload
                    );
            }
            else if (_selectedIdx == 1)
            {
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Credits", 0.3f, 0.3f);
                Debug.Log("Credit");
            }
            //                else if(_selectedIdx == 2)
            //                {
            //#if UNITY_EDITOR
            //                    UnityEditor.EditorApplication.isPlaying = false;
            //#else
            //                        Application.Quit();
            //#endif
            //                }
            else
            {
                Debug.LogError("未設定");
            }
        }
        #endregion
    }
}