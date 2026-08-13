using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// ネットワーク対戦の部屋建てウィンドウ
    ///
    /// キャラセレクト画面でのみ表示される。
    /// プレイヤー同士が顔を合わせるのがこの画面のため、ここで部屋を決める。
    /// 通信の中身は持たず、NetworkGameLauncher を呼ぶだけ。
    ///
    /// UI はコードから組み立てる。
    /// 文字はゲーム本体と同じフォントを使う。
    /// このフォントに日本語は入っていないため、表記も入力も英数字に揃えてある。
    /// 大きさは CanvasScaler の基準解像度で決まり、画面解像度に依存しない。
    /// </summary>
    public class NetworkRoomWindow
        : MonoBehaviour
    {
        #region プロパティ
        /// <summary>
        /// 他の台の状態をこの画面に映さない期間か
        ///
        /// 部屋に入ってから、画面をやり直し終えるまでの間。
        /// この間に映すと、相手のキャラが選ばれる様子が見えてから
        /// 画面が作り直されることになり、ちぐはぐに見える。
        /// </summary>
        public static bool IsRemoteApplySuspended { get; private set; } = false;

        /// <summary>
        /// 画面隅の開くボタンを出しているか
        ///
        /// 同じ隅に出る他の表示が、重ならない位置を決めるために見る。
        /// </summary>
        public static bool IsButtonVisible { get; private set; } = false;

        /// <summary>
        /// ウィンドウを開いているか
        /// </summary>
        public static bool IsWindowOpen { get; private set; } = false;

        /// <summary>
        /// 画面左上のうち、この表示が使っている下端
        ///
        /// 上を 0 とした負の値。何も出していなければ 0。
        /// 同じ隅に出る他の表示が、その下に並べるために見る。
        /// ウィンドウの高さは中身で変わるため、決め打ちにはできない。
        /// </summary>
        public static float OccupiedBottomY { get; private set; } = 0.0f;
        #endregion

        #region MonoBehaviour の実装
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateSelf()
        {
            if (_instance != null)
            {
                return;
            }

            var obj = new GameObject(nameof(NetworkRoomWindow));
            DontDestroyOnLoad(obj);
            obj.AddComponent<NetworkRoomWindow>();
        }

        void Awake()
        {
            _instance = this;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            BuildUi();
            ApplySceneVisibility(SceneManager.GetActiveScene().name);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                _instance = null;

                // 止めたまま消えると、以降ゲームを操作できなくなる
                TadaLib.Input.PlayerInputProxy.IsSuppressed = false;
            }
        }

        void Update()
        {
            // 隅をどこまで使っているかは、この画面を出していないときも知らせる。
            // 下の早期脱出より後ろに置くと、他の画面へ移っても
            // ここの値が古いまま残り、通知が要らない隙間を空けて出てしまう。
            RefreshOccupiedBottom();

            if (_canvasRoot == null || !_canvasRoot.activeSelf)
            {
                return;
            }

            // EventSystem はこの画面の読み込みより後に用意されることがあるため、
            // 一度だけでは間に合わない
            EnsureUiInputReady();

            if (_window == null || !_window.activeSelf)
            {
                return;
            }

            // ゲームを遊ぶ手で閉じられるようにする。
            // 閉じるためにマウスへ持ち替えるのが煩わしいため。
            //
            // 入力は止めてあるので、いつもの読み取り口は使えない。
            // キーボードは文字入力と重なるため、コントローラだけを見る。
            if (IsGamepadCloseButtonPressed())
            {
                SetWindowOpen(false);
                return;
            }

            // メンバーの出入りを追うため、開いている間は定期的に作り直す
            _memberRefreshTimer -= Time.unscaledDeltaTime;
            if (_memberRefreshTimer <= 0.0f)
            {
                _memberRefreshTimer = MemberRefreshIntervalSec;
                RefreshView();
            }
        }
        #endregion

        #region private メソッド (状態)
        void OnActiveSceneChanged(
            UnityEngine.SceneManagement.Scene prev,
            UnityEngine.SceneManagement.Scene next)
        {
            ApplySceneVisibility(next.name);
        }

        /// <summary>
        /// キャラセレクト画面でのみ表示する
        /// (部屋のセッション自体は NetworkGameLauncher が持ち続けるため切れない)
        /// </summary>
        void ApplySceneVisibility(string sceneName)
        {
            var isCharaSelect = sceneName == CharaSelectSceneName;

            _canvasRoot.SetActive(isCharaSelect);
            IsButtonVisible = isCharaSelect;

            if (!isCharaSelect)
            {
                SetWindowOpen(false);
                return;
            }

            // 開くボタンを押せるようにしておく必要がある
            EnsureUiInputReady();
        }

        /// <summary>
        /// マウスで UI を操作できる状態にする
        ///
        /// この画面の EventSystem は、入力を読む設定が空のまま置かれている。
        /// そのままではボタンを押しても何も起きない。
        /// 画面ごとに EventSystem が違うため、開くたびに確かめる。
        /// </summary>
        static void EnsureUiInputReady()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (eventSystem.currentInputModule is not InputSystemUIInputModule module)
            {
                return;
            }

            // 同じものを何度も設定し直さない
            if (ReferenceEquals(module, _preparedInputModule))
            {
                return;
            }

            _preparedInputModule = module;

            // 既に設定されているものには触れない
            if (module.point != null && module.point.action != null)
            {
                return;
            }

            if (_uiActions == null)
            {
                _uiActions = BuildUiActions();
            }

            module.point = InputActionReference.Create(_uiActions.FindAction(PointActionName));
            module.leftClick = InputActionReference.Create(_uiActions.FindAction(ClickActionName));
            module.scrollWheel = InputActionReference.Create(_uiActions.FindAction(ScrollActionName));

            Debug.Log("[NetworkRoomWindow] UI の入力設定が空だったため、マウス操作を割り当てました");
        }

        /// <summary>
        /// マウスで UI を触るのに要る最小限の入力を組み立てる
        ///
        /// 既存の入力アセットに頼らないのは、
        /// 画面ごとに設定の有無がまちまちで、当てにできないため。
        /// </summary>
        static InputActionAsset BuildUiActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "NetworkRoomWindowUI";

            var map = asset.AddActionMap("UI");
            map.AddAction(PointActionName, InputActionType.PassThrough, "<Mouse>/position");
            map.AddAction(ClickActionName, InputActionType.PassThrough, "<Mouse>/leftButton");
            map.AddAction(ScrollActionName, InputActionType.PassThrough, "<Mouse>/scroll");

            asset.Enable();

            return asset;
        }

        /// <summary>
        /// コントローラの決定 / キャンセルが押されたか
        ///
        /// どのボタンでも閉じると、トリガーや肩ボタンに触れただけで閉じてしまう。
        /// 閉じる意思が読み取れる 2 つに絞る。
        /// </summary>
        static bool IsGamepadCloseButtonPressed()
        {
            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame
                    || gamepad.buttonEast.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 左上のどこまで使っているかを知らせる
        ///
        /// 開いていればウィンドウの下端、閉じていればボタンの下端。
        /// </summary>
        void RefreshOccupiedBottom()
        {
            if (!IsButtonVisible)
            {
                OccupiedBottomY = 0.0f;
                return;
            }

            var rect = (_window != null && _window.activeSelf)
                ? _window.GetComponent<RectTransform>()
                : _toggleButtonRect;

            if (rect == null)
            {
                OccupiedBottomY = 0.0f;
                return;
            }

            OccupiedBottomY = rect.anchoredPosition.y - rect.rect.height;
        }

        void SetWindowOpen(bool isOpen)
        {
            if (isOpen)
            {
                EnsureUiInputReady();
            }

            _window.SetActive(isOpen);
            _dimmer.SetActive(isOpen);
            IsWindowOpen = isOpen;

            // 開いている間は裏でゲームが進まないようにする
            TadaLib.Input.PlayerInputProxy.IsSuppressed = isOpen;

            if (isOpen)
            {
                RefreshView();
            }
        }

        void OnJoinButton(Network.NetworkGameLauncher.JoinMode joinMode)
        {
            if (_isBusy)
            {
                return;
            }

            var nickname = _nicknameField.text.Trim();
            if (nickname.Length == 0)
            {
                nickname = DefaultNickname;
            }

            var passphrase = _passphraseField.text.Trim();
            if (passphrase.Length == 0)
            {
                _errorText.text = "ENTER A PASSWORD";
                RefreshView();
                return;
            }

            // 次回のために覚えておく
            PlayerPrefs.SetString(PrefKeyNickname, nickname);
            PlayerPrefs.SetString(PrefKeyPassphrase, passphrase);
            PlayerPrefs.SetInt(PrefKeyLocalCount, _localPlayerCount);
            PlayerPrefs.Save();

            JoinAsync(passphrase, nickname, joinMode).Forget();
        }

        async UniTask JoinAsync(string passphrase, string nickname, Network.NetworkGameLauncher.JoinMode joinMode)
        {
            _isBusy = true;
            _errorText.text = "";
            _statusText.text = joinMode == Network.NetworkGameLauncher.JoinMode.Create
                ? "CREATING ROOM..."
                : "SEARCHING ROOM...";
            RefreshView();

            // 入る側は、やり直しが済むまで他の台の状態を映さない。
            //
            // 席が配られた時点から同期は始まるが、
            // やり直しはその後になる。
            // そのままだと、相手のキャラが選ばれる様子が見えてから
            // 画面が作り直されることになり、ちぐはぐに見える。
            var isJoining = joinMode == Network.NetworkGameLauncher.JoinMode.JoinOnly;

            try
            {
                if (isJoining)
                {
                    IsRemoteApplySuspended = true;
                }

                var launcher = Network.NetworkGameLauncher.GetOrCreate();

                // 合言葉そのものを部屋名にせず、ゲーム固有の前置きを付ける
                await launcher.JoinAsync(
                    SessionNamePrefix + passphrase,
                    _localPlayerCount,
                    nickname,
                    joinMode);

                _statusText.text = "";

                if (!launcher.IsSessionReady)
                {
                    _errorText.text = launcher.LastJoinErrorMessage.Length > 0
                        ? launcher.LastJoinErrorMessage
                        : "ROOM IS FULL";

                    // 入れなかった以上、つないだままにしない
                    await launcher.LeaveAsync();

                    RefreshView();
                    return;
                }

                _joinedPassphrase = passphrase;

                // 入れたら、この画面をやり直す。
                //
                // 既にキャラを選んだ後で入ると、選択の状態が食い違ったまま残る。
                // 入ってから一つずつ直すより、まっさらにして組み直すほうが確実。
                //
                // 入れなかったときはやり直さない。
                // 合言葉を打ち間違えただけで画面が作り直されると煩わしい。
                if (isJoining)
                {
                    await RestartSceneAsync();

                    // やり直しで閉じた状態から始まるため、開き直す
                    SetWindowOpen(true);
                }
            }
            finally
            {
                // 途中で失敗しても必ず戻す。
                // 止めたままだと、以降ずっと相手の状態が映らなくなる。
                IsRemoteApplySuspended = false;

                _statusText.text = "";
                _isBusy = false;
                RefreshView();
            }
        }

        /// <summary>
        /// この画面をやり直す
        ///
        /// 画面遷移の仕組みをそのまま使う。
        /// まだ参加していないため、いつもの (オフラインの) 経路を通り、
        /// 遷移エフェクトもマネージャシーンの読み直しも面倒を見てもらえる。
        /// </summary>
        async UniTask RestartSceneAsync()
        {
            var transitionManager = TadaLib.Scene.TransitionManager.Instance;
            if (transitionManager == null)
            {
                return;
            }

            // この台だけで読み直す。
            // 既に部屋へ入っているため、そのままではホストへの遷移要求に化けてしまう。
            transitionManager.StartTransition(
                CharaSelectSceneName,
                RestartFadeDurationSec,
                RestartFadeDurationSec,
                isLocalOnly: true);

            // StartTransition は待てない作りのため、状態を見て待つ。
            // 遷移の途中でマネージャが入れ替わることがあるので、毎回引き直す。
            while (IsTransitioning(isExpected: true))
            {
                await UniTask.Yield();
            }

            while (IsTransitioning(isExpected: false))
            {
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// 遷移の開始待ち / 終了待ちを判定する
        /// </summary>
        /// <param name="isExpected">まだ始まっていないことを待つなら true</param>
        static bool IsTransitioning(bool isExpected)
        {
            var manager = TadaLib.Scene.TransitionManager.Instance;
            if (manager == null)
            {
                // マネージャが居ない間は待ち続けても進まない
                return false;
            }

            return isExpected ? !manager.IsTransitioning : manager.IsTransitioning;
        }

        void OnLeaveButton()
        {
            if (_isBusy)
            {
                return;
            }

            LeaveAsync(isRoomClosedByHost: false).Forget();
        }

        /// <summary>
        /// ホストからの解散の知らせを受けて抜ける
        /// </summary>
        public static void NotifyRoomClosedByHost()
        {
            if (_instance == null || _instance._isBusy)
            {
                return;
            }

            _instance.LeaveAsync(isRoomClosedByHost: true).Forget();
        }

        async UniTask LeaveAsync(bool isRoomClosedByHost)
        {
            _isBusy = true;

            // ホストは部屋を畳む側なので、先に全員へ知らせる。
            // 自分が抜けた後では伝えられない。
            var isHost = Network.NetworkSession.HasAuthority;
            if (isHost && !isRoomClosedByHost)
            {
                Network.NetworkFlowState.Instance?.NotifyRoomClosed();

                // 知らせが出ていくまで少し待つ
                await UniTask.Delay(System.TimeSpan.FromSeconds(RoomCloseNoticeWaitSec));
            }

            var launcher = Network.NetworkGameLauncher.Instance;
            if (launcher != null)
            {
                await launcher.LeaveAsync();
            }

            // 抜けた側はこの画面をやり直す。
            // 参加していた間の状態が残ったままになるため。
            //
            // ホストは部屋ごと畳んだだけで、この画面の主はそのまま自分なのでやり直さない。
            if (!isHost)
            {
                await RestartSceneAsync();
                SetWindowOpen(true);
            }

            _isBusy = false;
            RefreshView();
        }
        #endregion

        #region private メソッド (表示)
        /// <summary>
        /// 参加状況に応じて、入力フォームとメンバー一覧を切り替える
        /// </summary>
        void RefreshView()
        {
            var isJoined = Network.NetworkSession.IsOnline;

            _formGroup.SetActive(!isJoined);
            _joinedGroup.SetActive(isJoined);

            // 中身が無いときに場所を取らないようにする
            _statusText.gameObject.SetActive(_statusText.text.Length > 0);
            _errorText.gameObject.SetActive(_errorText.text.Length > 0);

            if (!isJoined)
            {
                return;
            }

            _roomNameText.text = $"ROOM: {_joinedPassphrase}";

            // ホストが抜けることは部屋を畳むことなので、言葉を分ける
            var isHost = Network.NetworkSession.HasAuthority;
            _roleText.text = isHost
                ? "YOU ARE THE HOST. START THE GAME FROM THIS PC."
                : "YOU ARE A GUEST. THE HOST LEADS THE GAME.";
            _leaveButtonLabel.text = isHost ? "CLOSE ROOM" : "LEAVE ROOM";

            RebuildMemberList();
        }

        /// <summary>
        /// メンバー一覧を席テーブルから作り直す
        ///
        /// 同じ台の 2 人目以降は「〇〇2」のように添字を付ける。
        /// </summary>
        void RebuildMemberList()
        {
            foreach (Transform child in _memberListRoot.transform)
            {
                Destroy(child.gameObject);
            }

            var seatTable = Network.NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                AddMemberRow("WAITING...", isSelf: false);
                return;
            }

            // 席ではなくメンバー表から作る。
            // 席は対戦に出る 4 枠だけで、席が空くのを待っている人は載らない。
            var members = seatTable.Members;
            var occupied = 0;

            for (int idx = 0; idx < members.Length; ++idx)
            {
                var member = members[idx];
                if (member.IsEmpty)
                {
                    continue;
                }

                occupied += member.LocalPlayerCount;

                var name = member.Nickname.ToString();
                if (name.Length == 0)
                {
                    name = DefaultNickname;
                }

                var isSelf = Network.NetworkSession.Runner != null
                    && member.Owner == Network.NetworkSession.Runner.LocalPlayer;

                // 同じ台に複数人いるときは、その人数ぶん並べる
                for (int slot = 0; slot < member.LocalPlayerCount; ++slot)
                {
                    var label = member.LocalPlayerCount >= 2 ? $"{name}{slot + 1}" : name;
                    AddMemberRow(label, isSelf);
                }
            }

            _memberCountText.text =
                $"MEMBERS  {occupied}/{Network.NetworkSeatTable.PlayerCountMax}";
        }

        void AddMemberRow(string label, bool isSelf)
        {
            var row = CreateUiObject("MemberRow", _memberListRoot.transform);
            AddLayoutElement(row, height: 50.0f);

            var rowImage = row.AddComponent<Image>();
            rowImage.sprite = _roundedSprite;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = isSelf ? SelfRowColor : RowColor;

            var text = CreateText(row.transform, label, 30.0f, TextColor);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            StretchWithPadding(text.rectTransform, 16.0f, 0.0f);

            if (isSelf)
            {
                var tag = CreateText(row.transform, "YOU", 23.0f, AccentColor);
                tag.alignment = TextAlignmentOptions.MidlineRight;
                StretchWithPadding(tag.rectTransform, 16.0f, 0.0f);
            }
        }
        #endregion

        #region private メソッド (UI 構築)
        void BuildUi()
        {
            _roundedSprite = CreateRoundedSprite();
            _fontAsset = LoadGameFontAsset();

            // Canvas (画面解像度に依存しないよう、基準解像度で設計する)
            _canvasRoot = CreateUiObject("NetworkRoomCanvas", transform);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRoot.AddComponent<GraphicRaycaster>();

            // 背後を暗くする幕。
            // 明るい背景との差をはっきりさせつつ、
            // 「今はゲームを始められない」ことを見た目でも伝える。
            //
            // 開くボタンより先に作って、ボタンが幕の上に残るようにする。
            BuildDimmer();
            BuildToggleButton();
            BuildWindow();

            SetWindowOpen(false);
            RefreshView();
        }

        void BuildDimmer()
        {
            _dimmer = CreateUiObject("Dimmer", _canvasRoot.transform);

            var image = _dimmer.AddComponent<Image>();
            image.color = DimmerColor;

            // ウィンドウの外を押したら閉じる。
            // ウィンドウ自身はこの上に載っているため、そちらの操作は奪わない。
            var button = _dimmer.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SetWindowOpen(false));

            StretchWithPadding(_dimmer.GetComponent<RectTransform>(), 0.0f, 0.0f);
        }

        /// <summary>
        /// 画面隅の「ネット対戦」ボタン
        /// </summary>
        void BuildToggleButton()
        {
            var button = CreateButton(
                _canvasRoot.transform,
                "NETWORK",
                () => SetWindowOpen(!_window.activeSelf));

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -24.0f);
            rect.sizeDelta = new Vector2(250.0f, 70.0f);

            _toggleButtonRect = rect;

            // マウスで触るものだと分かるように、文字の隣に絵を添える。
            //
            // ボタンの中に収める。
            // 外に置くと、絵とボタンが別のものに見えてしまう。
            //
            // 位置は数値で寄せずに並べて決める。
            // 文字の幅はフォントや表示倍率で変わるため、
            // 数値で寄せると環境によってずれる。
            var layout = button.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10.0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Midline;

            // 並べる対象になるため、引き伸ばしの設定は捨てる
            var labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var mouse = CreateUiObject("MouseIcon", button.transform);
            var mouseImage = mouse.AddComponent<Image>();
            mouseImage.sprite = CreateMouseSprite();
            mouseImage.color = TextColor;
            mouseImage.raycastTarget = false;
            mouseImage.preserveAspect = true;

            AddLayoutElement(mouse, height: MouseIconHeight, width: MouseIconWidth);
        }

        void BuildWindow()
        {
            _window = CreateUiObject("Window", _canvasRoot.transform);
            var windowImage = _window.AddComponent<Image>();
            windowImage.sprite = _roundedSprite;
            windowImage.type = Image.Type.Sliced;
            windowImage.color = PanelColor;

            // 暗い面がそのまま背景に沈まないよう、明るい縁で輪郭を出す
            var outline = _window.AddComponent<Outline>();
            outline.effectColor = PanelEdgeColor;
            outline.effectDistance = new Vector2(3.0f, 3.0f);
            outline.useGraphicAlpha = false;

            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -110.0f);
            rect.sizeDelta = new Vector2(WindowWidth, 0.0f);

            var layout = _window.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 24);
            layout.spacing = 10.0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 中身の量に合わせて枠を伸ばす。
            // 高さを決め打ちにすると、参加前と参加後で中身の量が違うため
            // どちらかで枠からはみ出す。
            var fitter = _window.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // タイトル行
            {
                var title = CreateText(_window.transform, "NETWORK", 39.0f, TextColor);
                title.fontStyle = FontStyles.Bold;
                AddLayoutElement(title.gameObject, height: 50.0f);
            }

            BuildFormGroup();
            BuildJoinedGroup();

            // 状態・エラー表示 (常に一番下)
            // 中身が無いときは場所を取らないよう隠す
            _statusText = CreateText(_window.transform, "", 27.0f, TextColor);
            _statusText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_statusText.gameObject, height: 36.0f);

            _errorText = CreateText(_window.transform, "", 27.0f, ErrorColor);
            _errorText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_errorText.gameObject, height: 72.0f);

            // 閉じ方の案内
            //
            // 閉じるボタンは置かない。
            // 「部屋を出る」と並ぶと押し間違えるうえ、
            // 遊ぶ手を止めてマウスへ持ち替えさせることになる。
            {
                var hint = CreateText(
                    _window.transform,
                    "PRESS A / B OR CLICK OUTSIDE TO CLOSE",
                    19.0f,
                    SubTextColor);
                hint.alignment = TextAlignmentOptions.Center;
                hint.textWrappingMode = TextWrappingModes.Normal;
                AddLayoutElement(hint.gameObject, height: 52.0f);
            }
        }

        /// <summary>
        /// 未参加のときの入力フォーム
        /// </summary>
        void BuildFormGroup()
        {
            _formGroup = CreateVerticalGroup(_window.transform, "FormGroup");

            CreateLabel(_formGroup.transform, "NAME");
            _nicknameField = CreateInputField(
                _formGroup.transform,
                PlayerPrefs.GetString(PrefKeyNickname, DefaultNickname),
                NicknameMaxLength);

            CreateLabel(_formGroup.transform, "PASSWORD");
            _passphraseField = CreateInputField(
                _formGroup.transform,
                PlayerPrefs.GetString(PrefKeyPassphrase, DefaultPassphrase),
                PassphraseMaxLength);

            CreateLabel(_formGroup.transform, "PLAYERS ON THIS PC");

            // 人数のステッパー (◀ 1 ▶)
            {
                var row = CreateUiObject("CountRow", _formGroup.transform);
                AddLayoutElement(row, height: 62.0f);

                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8.0f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;

                var minus = CreateButton(row.transform, "<", () => ChangeLocalPlayerCount(-1));
                AddLayoutElement(minus, width: 70.0f);

                _localCountText = CreateText(row.transform, "1", 34.0f, TextColor);
                _localCountText.alignment = TextAlignmentOptions.Center;
                AddLayoutElement(_localCountText.gameObject, width: 70.0f);

                var plus = CreateButton(row.transform, ">", () => ChangeLocalPlayerCount(1));
                AddLayoutElement(plus, width: 70.0f);
            }

            _localPlayerCount = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefKeyLocalCount, DefaultLocalPlayerCount),
                1,
                Network.NetworkSeatTable.SeatCountMax);
            _localCountText.text = _localPlayerCount.ToString();

            // 建てる / 入る
            {
                var row = CreateUiObject("JoinRow", _formGroup.transform);
                AddLayoutElement(row, height: 70.0f);

                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 12.0f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;

                CreateButton(row.transform, "CREATE",
                    () => OnJoinButton(Network.NetworkGameLauncher.JoinMode.Create),
                    isPrimary: true);
                CreateButton(row.transform, "JOIN",
                    () => OnJoinButton(Network.NetworkGameLauncher.JoinMode.JoinOnly),
                    isPrimary: true);
            }
        }

        /// <summary>
        /// 参加後のメンバー一覧
        /// </summary>
        void BuildJoinedGroup()
        {
            _joinedGroup = CreateVerticalGroup(_window.transform, "JoinedGroup");

            _roomNameText = CreateText(_joinedGroup.transform, "", 32.0f, TextColor);
            _roomNameText.fontStyle = FontStyles.Bold;
            AddLayoutElement(_roomNameText.gameObject, height: 41.0f);

            _roleText = CreateText(_joinedGroup.transform, "", 25.0f, SubTextColor);
            _roleText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_roleText.gameObject, height: 68.0f);

            _memberCountText = CreateText(_joinedGroup.transform, "MEMBERS", 27.0f, SubTextColor);
            AddLayoutElement(_memberCountText.gameObject, height: 36.0f);

            // 高さは人数で変わるため、決め打ちにしない
            _memberListRoot = CreateVerticalGroup(_joinedGroup.transform, "MemberList");

            // 部屋を出るのは戻しにくい操作のため、他と色を分けて間を空ける
            {
                var spacer = CreateUiObject("Spacer", _joinedGroup.transform);
                AddLayoutElement(spacer, height: 12.0f);
            }

            // 文字は参加のたびに変わるため、後から書き換える (RefreshView)
            _leaveButton = CreateButton(_joinedGroup.transform, "", OnLeaveButton);
            _leaveButton.GetComponent<Image>().color = LeaveButtonColor;
            _leaveButtonLabel = _leaveButton.GetComponentInChildren<TextMeshProUGUI>();
            AddLayoutElement(_leaveButton, height: 62.0f);
        }

        void ChangeLocalPlayerCount(int diff)
        {
            _localPlayerCount = Mathf.Clamp(
                _localPlayerCount + diff, 1, Network.NetworkSeatTable.SeatCountMax);
            _localCountText.text = _localPlayerCount.ToString();
        }
        #endregion

        #region private メソッド (UI 部品)
        static GameObject CreateUiObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        /// <summary>
        /// 縦に並べる入れ物
        ///
        /// 高さは中身から決まる。
        /// 親も childControlHeight で高さを見るため、
        /// ContentSizeFitter を重ねる必要はない (重ねると計算が競合する)。
        /// </summary>
        static GameObject CreateVerticalGroup(Transform parent, string name)
        {
            var group = CreateUiObject(name, parent);
            var layout = group.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8.0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return group;
        }

        TextMeshProUGUI CreateText(Transform parent, string content, float size, Color color)
        {
            var obj = CreateUiObject("Text", parent);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.font = _fontAsset;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }

        void CreateLabel(Transform parent, string content)
        {
            var label = CreateText(parent, content, 25.0f, SubTextColor);
            AddLayoutElement(label.gameObject, height: 32.0f);
        }

        /// <summary>
        /// ボタンを作る
        /// </summary>
        /// <param name="isPrimary">
        /// その画面で一番やってほしい操作かどうか。
        /// すべて同じ強さで並べると、どれを押せばよいか分からなくなる。
        /// </param>
        GameObject CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction onClick,
            bool isPrimary = false)
        {
            var obj = CreateUiObject($"Button_{label}", parent);
            var image = obj.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = isPrimary ? PrimaryButtonColor : ButtonColor;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            button.colors = colors;

            var text = CreateText(obj.transform, label, 30.0f, isPrimary ? DarkTextColor : TextColor);
            text.alignment = TextAlignmentOptions.Center;
            StretchWithPadding(text.rectTransform, 0.0f, 0.0f);

            return obj;
        }

        TMP_InputField CreateInputField(Transform parent, string initial, int maxLength)
        {
            var obj = CreateUiObject("InputField", parent);
            AddLayoutElement(obj, height: 62.0f);

            var image = obj.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldColor;

            var input = obj.AddComponent<TMP_InputField>();

            // 表示領域
            var viewport = CreateUiObject("TextArea", obj.transform);
            viewport.AddComponent<RectMask2D>();
            StretchWithPadding(viewport.GetComponent<RectTransform>(), 16.0f, 8.0f);

            var text = CreateText(viewport.transform, "", 30.0f, TextColor);
            StretchWithPadding(text.rectTransform, 0.0f, 0.0f);

            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.fontAsset = _fontAsset;
            input.pointSize = 30.0f;
            input.characterLimit = maxLength;

            // フォントに日本語が無く、打っても豆腐になる。
            // 合言葉は文字列の一致で部屋を決めるため、
            // 打てない文字を許すと入った先が食い違う。
            input.onValidateInput = (_, _, addedChar) => IsAllowedChar(addedChar) ? addedChar : '\0';

            input.text = initial;

            return input;
        }

        /// <summary>
        /// 入力を許す文字か
        /// </summary>
        static bool IsAllowedChar(char value)
        {
            return (value >= 'a' && value <= 'z')
                || (value >= 'A' && value <= 'Z')
                || (value >= '0' && value <= '9')
                || value == '-'
                || value == '_';
        }

        static void AddLayoutElement(GameObject obj, float height = -1.0f, float width = -1.0f)
        {
            var element = obj.AddComponent<LayoutElement>();
            if (height > 0.0f)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }
            if (width > 0.0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }
        }

        static void StretchWithPadding(RectTransform rect, float paddingX, float paddingY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingX, paddingY);
            rect.offsetMax = new Vector2(-paddingX, -paddingY);
        }

        /// <summary>
        /// マウスの絵を作る
        ///
        /// マウスで触るものだと分かるようにする。
        /// 画像アセットを持ち込まずに済ませたいので、輪郭を直接描く。
        /// </summary>
        static Sprite CreateMouseSprite()
        {
            const int width = 24;
            const int height = 34;
            const float radius = 10.0f;
            const float thickness = 2.5f;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);

                    // 角丸の枠からの距離を測る
                    var inner = new Vector2(
                        Mathf.Clamp(point.x, radius, width - radius),
                        Mathf.Clamp(point.y, radius, height - radius));
                    var distance = Vector2.Distance(point, inner);

                    var outer = Mathf.Clamp01(radius - distance + 0.5f);
                    var hollow = Mathf.Clamp01(radius - thickness - distance + 0.5f);
                    var alpha = Mathf.Clamp01(outer - hollow);

                    // ホイール (上寄りの中央に短い縦線)
                    var isWheel = Mathf.Abs(point.x - width * 0.5f) <= thickness * 0.5f
                        && point.y >= height * 0.62f
                        && point.y <= height * 0.84f;
                    if (isWheel)
                    {
                        alpha = 1.0f;
                    }

                    texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 角丸の 9 スライス用スプライトを作る
        /// (画像アセットを持ち込まずに、柔らかい見た目にするため)
        /// </summary>
        static Sprite CreateRoundedSprite()
        {
            const int size = 32;
            const float radius = 10.0f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    var inner = new Vector2(
                        Mathf.Clamp(x + 0.5f, radius, size - radius),
                        Mathf.Clamp(y + 0.5f, radius, size - radius));
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), inner);
                    var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
                }
            }
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100.0f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect,
                border: new Vector4(12.0f, 12.0f, 12.0f, 12.0f));
        }

        /// <summary>
        /// ゲームで使っているフォントを読む
        ///
        /// 日本語は入っていないため、表示は英数字だけにしてある。
        /// </summary>
        static TMP_FontAsset LoadGameFontAsset()
        {
            var asset = Resources.Load<TMP_FontAsset>(GameFontResourcePath);
            if (asset != null)
            {
                return asset;
            }

            Debug.LogWarning(
                $"[NetworkRoomWindow] フォントが見つからないため既定のものを使います: Resources/{GameFontResourcePath}");
            return TMP_Settings.defaultFontAsset;
        }
        #endregion

        #region private フィールド
        static NetworkRoomWindow _instance;

        /// <summary>
        /// マウス操作を割り当てた入力
        /// </summary>
        static InputActionAsset _uiActions;

        /// <summary>
        /// 設定済みか確かめた EventSystem の入力部品
        /// </summary>
        static InputSystemUIInputModule _preparedInputModule;

        const string PointActionName = "Point";
        const string ClickActionName = "Click";
        const string ScrollActionName = "Scroll";

        const string CharaSelectSceneName = "CharaSelect";
        const string SessionNamePrefix = "champan_";
        const string DefaultNickname = "PLAYER";
        const string DefaultPassphrase = "champan";
        const int DefaultLocalPlayerCount = 1;
        const int NicknameMaxLength = 6;
        const int PassphraseMaxLength = 16;
        const int CanvasSortingOrder = 500;
        const float WindowWidth = 500.0f;

        /// <summary>
        /// 開くボタンに添えるマウスの絵の大きさ
        /// </summary>
        const float MouseIconWidth = 26.0f;
        const float MouseIconHeight = 38.0f;
        const float MemberRefreshIntervalSec = 0.5f;

        /// <summary>
        /// 参加前にこの画面をやり直すときの遷移エフェクトの長さ
        /// </summary>
        const float RestartFadeDurationSec = 0.3f;

        /// <summary>
        /// 解散の知らせが出ていくのを待つ時間
        /// </summary>
        const float RoomCloseNoticeWaitSec = 0.3f;

        /// <summary>
        /// ゲーム本体と同じフォント
        /// </summary>
        const string GameFontResourcePath = "Fonts/Asap-ExtraBold SDF";

        const string PrefKeyNickname = "NetworkRoom.Nickname";
        const string PrefKeyPassphrase = "NetworkRoom.Passphrase";
        const string PrefKeyLocalCount = "NetworkRoom.LocalCount";


        // 配色は音量ウィンドウと共通のものを引く。
        // それぞれが持つと、片方だけ直して食い違うため。
        static readonly Color DimmerColor = Common.GameUiStyle.DimmerColor;
        static readonly Color PanelColor = Common.GameUiStyle.PanelColor;
        static readonly Color PanelEdgeColor = Common.GameUiStyle.PanelEdgeColor;
        static readonly Color FieldColor = Common.GameUiStyle.FieldColor;
        static readonly Color ButtonColor = Common.GameUiStyle.ButtonColor;
        static readonly Color PrimaryButtonColor = Common.GameUiStyle.PrimaryButtonColor;
        static readonly Color LeaveButtonColor = Common.GameUiStyle.LeaveButtonColor;
        static readonly Color RowColor = Common.GameUiStyle.RowColor;
        static readonly Color SelfRowColor = Common.GameUiStyle.SelfRowColor;
        static readonly Color TextColor = Common.GameUiStyle.TextColor;
        static readonly Color SubTextColor = Common.GameUiStyle.SubTextColor;
        static readonly Color DarkTextColor = Common.GameUiStyle.DarkTextColor;
        static readonly Color AccentColor = Common.GameUiStyle.AccentColor;
        static readonly Color ErrorColor = Common.GameUiStyle.ErrorColor;

        GameObject _canvasRoot;
        RectTransform _toggleButtonRect;
        GameObject _dimmer;
        GameObject _window;
        GameObject _formGroup;
        GameObject _joinedGroup;
        GameObject _memberListRoot;
        GameObject _leaveButton;
        TextMeshProUGUI _leaveButtonLabel;

        TMP_InputField _nicknameField;
        TMP_InputField _passphraseField;
        TextMeshProUGUI _localCountText;
        TextMeshProUGUI _statusText;
        TextMeshProUGUI _errorText;
        TextMeshProUGUI _roomNameText;
        TextMeshProUGUI _roleText;
        TextMeshProUGUI _memberCountText;

        Sprite _roundedSprite;
        TMP_FontAsset _fontAsset;

        int _localPlayerCount = DefaultLocalPlayerCount;
        bool _isBusy = false;
        string _joinedPassphrase = "";
        float _memberRefreshTimer = 0.0f;
        #endregion
    }
}
