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

        bool _isVideoFinished = false;

        [SerializeField] private Transform _start;
        [SerializeField] private GameObject _startOnImage;
        [SerializeField] private GameObject _startOffImage;
        [SerializeField] private Transform _exit;
        [SerializeField] private GameObject _exitOnImage;
        [SerializeField] private GameObject _exitOffImage;
        #endregion

        #region privateメソッド
        public async UniTask Staging()
        {
            // UI が動画を上書きしてしまったので、再生終了まで非表示にする
            _mainCanvas.gameObject.SetActive(false);

            // 動画準備開始
            _introPlayer.Prepare();

            // 動画再生のために、各種システムが安定化するまで待つ
            await UniTask.WaitForSeconds(0.5f);

            // completed しても動画の読み込みが続いていることがある
            //_introPlayer.prepareCompleted += (vp) =>
            //{
            //    vp.Play();
            //    // 再生開始までの背景
            //    _introCanvas.gameObject.SetActive(false);
            //};

            _introPlayer.loopPointReached += (vp) =>
            {
                _isVideoFinished = true;
            };

            await UniTask.WaitUntil(() => _introPlayer.isPrepared);

            _introPlayer.frame = 0; // 明示的に最初から再生する
            _introPlayer.Play();

            // 動画が終わるまで待つ
            var lastClickedTime = -10.0f;
            while (true)
            {
                if (_isVideoFinished)
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
            _mainCanvas.gameObject.SetActive(true);

            // ロゴのアニメーション
            var main = GameObject.Find("Main").transform;
            main.DOScale(Vector3.one, 0.6f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => MainYoyo());

            void MainYoyo()
            {
                main.GetComponent<RectTransform>()
                    .DOAnchorPos(new(-67, 89), 2f)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            await UniTask.WaitForSeconds(0.1f);

            // 「チャン・ピョン・パン！」の再生
            SEManager.Instance.Play(SEPath.TITLE_SCREEN_04);

            await UniTask.WaitForSeconds(0.2f);

            // 背景のアニメーション
            var titleBgmask = FindAnyObjectByType<TitleBgMask>();
            //DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, Vector3.one * 1.5f, 0.5f);
            //DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, new Vector3(1.32f, 1.14f, 1.5f), 0.5f);
            DOTween.To(() => titleBgmask.Scale, titleBgmask.SetScale, new Vector3(1.21f, 1.25f, 1.5f), 0.5f);

            await UniTask.WaitForSeconds(0.4f);

            // パンのアニメーション
            var charInfos = new (string name, Vector2 pos, float moveAmount)[]
            {
                ("Chara1", new(-701, -294), 30f),
                ("Chara2", new(704, -341), 30f),
                ("Chara3", new(543, -38), 20f),
                ("Chara4", new(-428, -66), 20f),
            };
            foreach (var (name, pos, moveAmount) in charInfos)
            {
                var rt = GameObject.Find(name).GetComponent<RectTransform>();
                var startPos = rt.anchoredPosition;
                rt.DOAnchorPos(pos, 0.8f)
                  .SetEase(Ease.OutBack)
                  .OnComplete(() => CharYoyo());

                void CharYoyo()
                {
                    var moveVec = (startPos - pos).normalized * moveAmount;
                    rt.DOAnchorPos(moveVec + pos, 2f)
                      .SetEase(Ease.Linear)
                      .SetLoops(-1, LoopType.Yoyo);
                }
            }

            await UniTask.WaitForSeconds(1.2f);

            // START・EXITのアニメーション
            _start.DOScale(Vector3.one * 1.1f, 0.15f);
            _exit.DOScale(Vector3.one, 0.15f);
            // BGMの再生
            BGMManager.Instance.Play(BGMPath.TITLE_SCREEN);

            await UniTask.WaitForSeconds(0.2f);

            {
                var inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(0);
                var isPushed = false;
                var isStartSelected = true;
                var isInputValid = true;
                inputProxy.OnAction += OnActoinTrigged;
                inputProxy.OnMove += OnMove;

                while (true)
                {
                    if (isPushed)
                    {
                        if (isStartSelected)
                        {
                            // SE再生
                            SEManager.Instance.Play(SEPath.MENU_VALIDATION);
                            inputProxy.OnAction -= OnActoinTrigged;
                            inputProxy.OnMove -= OnMove;
                            TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.3f, 0.3f);
                            break;
                        }
                        else
                        {
#if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                        }
                    }
                    await UniTask.Yield();
                }

                void OnActoinTrigged()
                {
                    isPushed = true;
                }

                void OnMove(Vector2 value)
                {
                    if (!isInputValid || value.x == 0)
                    {
                        isInputValid = true;
                        return;
                    }

                    isInputValid = false;
                    isStartSelected = !isStartSelected;
                    // 表示イメージの切替
                    _startOnImage.SetActive(isStartSelected);
                    _startOffImage.SetActive(!isStartSelected);
                    _exitOnImage.SetActive(!isStartSelected);
                    _exitOffImage.SetActive(isStartSelected);
                    // 文字サイズの設定
                    var (startValue, exitvalue) = isStartSelected ? (1.1f, 1f) : (1f, 1.1f);
                    _start.DOScale(Vector3.one * startValue, 0.1f);
                    _exit.DOScale(Vector3.one * exitvalue, 0.1f);
                    // SE再生
                    SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
                }
            }
        }

        // ロールバックできるように、もとの実装を残している
        //public async UniTask Staging()
        //{
        //    // 最低 2 秒は待つ

        //    await UniTask.WaitForSeconds(2.0f);

        //    // 誰かがボタンを押したら次へ
        //    var gameController = GameController.Instance;
        //    bool isPushed = false;

        //    void OnActoinTrigged()
        //    {
        //        isPushed = true;
        //    }

        //    for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
        //    {
        //        gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction += OnActoinTrigged;
        //    }

        //    while (!isPushed)
        //    {
        //        await UniTask.Yield();
        //    }

        //    // TODO: UI が動く

        //    // コールバック解除
        //    for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
        //    {
        //        gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction -= OnActoinTrigged;
        //    }

        //    // 次のシーンへ

        //    TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.5f, 0.5f);
        //}


        #endregion
    }
}