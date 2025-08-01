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

        [SerializeField]
        UnityEngine.UI.Image _bgCharaSelect;

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
            _introPlayer.enabled = false;
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

        static bool IsFirst = true;

        async UniTask SelectItem()
        {
            // 項目生成

            for (int idx = 0; idx < _items.Count; ++idx)
            {
                _items[idx].OnUnselected();
                _items[idx].GetComponent<RectTransform>().DOScale(Vector3.one, 0.15f);
            }

            // コントローラー接続がなければ、接続を待つ
            // キーボードの場合も考慮する
            if (IsFirst)
            {
                IsFirst = false;
                var playSe = false;
                while (true)
                {
                    if (TadaLib.Input.PlayerInputManager.Instance.IsExistGamePad())
                    {
                        break;
                    }

                    var isFinish = false;
                    foreach (var inputProxy in TadaLib.Input.PlayerInputManager.Instance.InputProxies)
                    {
                        if (inputProxy.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                        {
                            isFinish = true;
                            break;
                        }
                    }

                    if (isFinish)
                    {
                        break;
                    }

                    playSe = true;
                    await UniTask.Yield();
                }
                if (playSe)
                {
                    SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
                }
            }

            _items[_selectedIdx].OnSelected();

            await UniTask.WaitForSeconds(0.2f);

            while (true)
            {
                var isFinish = false;
                foreach (var inputProxy in TadaLib.Input.PlayerInputManager.Instance.InputProxies)
                {
                    // 決定優先
                    if (inputProxy.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                    {
                        isFinish = true;
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

                        break;
                    }
                }

                if (isFinish)
                {
                    break;
                }

                await UniTask.Yield();
            }

            // この時点で決定確定

            // SE 再生
            SEManager.Instance.Play(SEPath.MENU_VALIDATION);

            if (_selectedIdx == 0)
            {
                // 次シーンへと繋ぐ
                async UniTask PreUnload()
                {
                    var titleBgmask = FindAnyObjectByType<TitleBgMask>();
                    DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, new Vector3(titleBgmask.Scale.x * 0.8f, 0.0f, 0.0f), 0.3f);

                    Vector2 ParentPos = new Vector3(0.0f, 82.11f);
                    const float ParentScale = 0.84f;
                    //var charInfos = new (string name, Vector2 pos, float rotZ, float scale)[]
                    //{
                    //    ("Chara1", new Vector2(-481.7f, -37.5f), 17.49f, 1.08f),
                    //    ("Chara2", new Vector2(187.3f, -68.8f), 0.0f, 1.2f),
                    //    ("Chara3", new Vector2(542.3f, 149.7f), -13.3f, 1.12f),
                    //    ("Chara4", new Vector2(-104.4f, 188.7f), -10.06f, 1.16f),
                    //};

                    var charInfos = new (string name, Vector2 pos, float rotZ, float scale)[]
                    {
                        ("Chara1", new Vector2(-481.7f, -37.5f), 0.0f, 1.02f),
                        ("Chara2", new Vector2(187.3f, -68.8f), 0.0f, 1.01f),
                        ("Chara3", new Vector2(542.3f, 149.7f), 0.0f, 1f),
                        ("Chara4", new Vector2(-104.4f, 188.7f), 0.0f, 1.08f),
                    };


                    foreach (var (name, pos, rotZ, scale) in charInfos)
                    {
                        var rt = GameObject.Find(name).GetComponent<RectTransform>();
                        rt.DOKill();
                        rt.DOLocalMove(pos * ParentScale + ParentPos, 0.5f);
                        rt.DORotate(new Vector3(0.0f, 0.0f, rotZ), 0.5f);
                        rt.DOScale(Vector3.one * scale * ParentScale, 0.5f);

                        rt.GetComponent<CharaImageChanger>().Change();
                    }

                    var main = GameObject.Find("Main").GetComponent<UnityEngine.UI.Image>().DOFade(0.0f, 0.2f);

                    foreach (var item in _items)
                    {
                        item.GetComponent<CanvasGroup>().DOFade(0.0f, 0.2f);
                    }

                    _bgCharaSelect.DOFade(1.0f, 0.5f);

                    await UniTask.WaitForSeconds(0.5f);
                }

                TadaLib.Scene.TransitionManager.Instance.StartTransitionAdvancedForTitle(
                    nextScene: "CharaSelect",
                    preUnloadFunc: PreUnload
                    );

                //TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.3f, 0.3f);
            }
            else if (_selectedIdx == 1)
            {
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Credits", 0.3f, 0.3f);
            }
            else
            {
                Debug.LogError("未設定");
            }
        }
        #endregion
    }
}