using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace App.Ui.Title
{
    /// <summary>
    /// ネットワーク対戦の部屋建てウィンドウ
    ///
    /// タイトル画面でのみ表示される。
    /// 通信の中身は持たず、NetworkGameLauncher を呼ぶだけ。
    ///
    /// UI はコードから組み立てる。
    /// プロジェクトに日本語の TMP フォントが無いため、
    /// OS のフォントから実行時に動的フォントを作って使う。
    /// 大きさは CanvasScaler の基準解像度で決まり、画面解像度に依存しない。
    /// </summary>
    public class NetworkRoomWindow
        : MonoBehaviour
    {
        #region プロパティ
        /// <summary>
        /// タイトルの操作を止めるべきか
        ///
        /// ウィンドウが出ている間はゲームを開始できない。
        /// 文字入力中のキー操作でメニューが動くのも防ぐ。
        /// </summary>
        public static bool IsInputBlocked
            => _instance != null && _instance._window != null && _instance._window.activeSelf;
        #endregion

        #region MonoBehaviour の実装
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateSelf()
        {
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
            }
        }

        void Update()
        {
            if (_window == null || !_window.activeSelf)
            {
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
        /// タイトル画面でのみ表示する
        /// (部屋のセッション自体は NetworkGameLauncher が持ち続けるため切れない)
        /// </summary>
        void ApplySceneVisibility(string sceneName)
        {
            var isTitle = sceneName == TitleSceneName;

            _canvasRoot.SetActive(isTitle);

            if (!isTitle && _window != null)
            {
                _window.SetActive(false);
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
                _errorText.text = "合言葉を入力してください";
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
                ? "部屋を建てています..."
                : "部屋を探しています...";
            RefreshView();

            var launcher = Network.NetworkGameLauncher.GetOrCreate();

            // 合言葉そのものを部屋名にせず、ゲーム固有の前置きを付ける
            await launcher.JoinAsync(
                SessionNamePrefix + passphrase,
                _localPlayerCount,
                nickname,
                joinMode);

            _isBusy = false;
            _statusText.text = "";

            if (!launcher.IsSessionReady)
            {
                _errorText.text = launcher.LastJoinErrorMessage;
            }

            _joinedPassphrase = passphrase;
            RefreshView();
        }

        void OnLeaveButton()
        {
            if (_isBusy)
            {
                return;
            }

            LeaveAsync().Forget();
        }

        async UniTask LeaveAsync()
        {
            _isBusy = true;

            var launcher = Network.NetworkGameLauncher.Instance;
            if (launcher != null)
            {
                await launcher.LeaveAsync();
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

            _roomNameText.text = $"部屋: {_joinedPassphrase}";
            _roleText.text = Network.NetworkSession.HasAuthority
                ? "あなたはホストです。ゲームの開始はこの台の操作で進みます"
                : "あなたはゲストです。ホストの操作に追従します";

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
                AddMemberRow("(席の情報を待っています...)", isSelf: false);
                return;
            }

            // 同じ台から何人参加しているかを数える (添字を付けるかの判断に使う)
            var ownerCounts = new Dictionary<Fusion.PlayerRef, int>();
            for (int idx = 0; idx < seatTable.Seats.Length; ++idx)
            {
                var seat = seatTable.Seats[idx];
                if (seat.IsEmpty)
                {
                    continue;
                }

                ownerCounts[seat.Owner] = ownerCounts.TryGetValue(seat.Owner, out var count) ? count + 1 : 1;
            }

            var occupied = 0;
            for (int idx = 0; idx < seatTable.Seats.Length; ++idx)
            {
                var seat = seatTable.Seats[idx];
                if (seat.IsEmpty)
                {
                    continue;
                }

                ++occupied;

                var name = seat.Nickname.ToString();
                if (name.Length == 0)
                {
                    name = DefaultNickname;
                }

                if (ownerCounts[seat.Owner] >= 2)
                {
                    name = $"{name}{seat.LocalSlot + 1}";
                }

                AddMemberRow(name, isSelf: seatTable.IsLocalSeat(idx));
            }

            _memberCountText.text = $"メンバー ({occupied}/{Network.NetworkSeatTable.SeatCountMax})";
        }

        void AddMemberRow(string label, bool isSelf)
        {
            var row = CreateUiObject("MemberRow", _memberListRoot.transform);
            AddLayoutElement(row, height: 44.0f);

            var rowImage = row.AddComponent<Image>();
            rowImage.sprite = _roundedSprite;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = isSelf ? SelfRowColor : RowColor;

            var text = CreateText(row.transform, label, 26.0f, TextColor);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            StretchWithPadding(text.rectTransform, 16.0f, 0.0f);

            if (isSelf)
            {
                var tag = CreateText(row.transform, "自分", 20.0f, AccentColor);
                tag.alignment = TextAlignmentOptions.MidlineRight;
                StretchWithPadding(tag.rectTransform, 16.0f, 0.0f);
            }
        }
        #endregion

        #region private メソッド (UI 構築)
        void BuildUi()
        {
            _roundedSprite = CreateRoundedSprite();
            _fontAsset = CreateJapaneseFontAsset();

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

            BuildToggleButton();
            BuildWindow();

            _window.SetActive(false);
            RefreshView();
        }

        /// <summary>
        /// 画面隅の「ネット対戦」ボタン
        /// </summary>
        void BuildToggleButton()
        {
            var button = CreateButton(
                _canvasRoot.transform,
                "ネット対戦",
                () =>
                {
                    _window.SetActive(!_window.activeSelf);
                    if (_window.activeSelf)
                    {
                        RefreshView();
                    }
                });

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -24.0f);
            rect.sizeDelta = new Vector2(220.0f, 64.0f);
        }

        void BuildWindow()
        {
            _window = CreateUiObject("Window", _canvasRoot.transform);
            var windowImage = _window.AddComponent<Image>();
            windowImage.sprite = _roundedSprite;
            windowImage.type = Image.Type.Sliced;
            windowImage.color = PanelColor;

            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -104.0f);
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
                var title = CreateText(_window.transform, "ネット対戦", 34.0f, TextColor);
                title.fontStyle = FontStyles.Bold;
                AddLayoutElement(title.gameObject, height: 44.0f);
            }

            BuildFormGroup();
            BuildJoinedGroup();

            // 状態・エラー表示 (常に一番下)
            // 中身が無いときは場所を取らないよう隠す
            _statusText = CreateText(_window.transform, "", 24.0f, TextColor);
            _statusText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_statusText.gameObject, height: 32.0f);

            _errorText = CreateText(_window.transform, "", 24.0f, ErrorColor);
            _errorText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_errorText.gameObject, height: 64.0f);

            // 閉じるボタン
            {
                var close = CreateButton(_window.transform, "閉じる", () => _window.SetActive(false));
                AddLayoutElement(close, height: 56.0f);
            }
        }

        /// <summary>
        /// 未参加のときの入力フォーム
        /// </summary>
        void BuildFormGroup()
        {
            _formGroup = CreateVerticalGroup(_window.transform, "FormGroup");

            CreateLabel(_formGroup.transform, "ニックネーム");
            _nicknameField = CreateInputField(
                _formGroup.transform,
                PlayerPrefs.GetString(PrefKeyNickname, DefaultNickname),
                NicknameMaxLength);

            CreateLabel(_formGroup.transform, "合言葉 (英数字がおすすめ)");
            _passphraseField = CreateInputField(
                _formGroup.transform,
                PlayerPrefs.GetString(PrefKeyPassphrase, DefaultPassphrase),
                PassphraseMaxLength);

            CreateLabel(_formGroup.transform, "この台で遊ぶ人数");

            // 人数のステッパー (◀ 1 ▶)
            {
                var row = CreateUiObject("CountRow", _formGroup.transform);
                AddLayoutElement(row, height: 56.0f);

                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8.0f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;

                var minus = CreateButton(row.transform, "◀", () => ChangeLocalPlayerCount(-1));
                AddLayoutElement(minus, width: 64.0f);

                _localCountText = CreateText(row.transform, "1", 30.0f, TextColor);
                _localCountText.alignment = TextAlignmentOptions.Center;
                AddLayoutElement(_localCountText.gameObject, width: 64.0f);

                var plus = CreateButton(row.transform, "▶", () => ChangeLocalPlayerCount(1));
                AddLayoutElement(plus, width: 64.0f);
            }

            _localPlayerCount = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefKeyLocalCount, DefaultLocalPlayerCount),
                1,
                Network.NetworkSeatTable.SeatCountMax);
            _localCountText.text = _localPlayerCount.ToString();

            // 建てる / 入る
            {
                var row = CreateUiObject("JoinRow", _formGroup.transform);
                AddLayoutElement(row, height: 64.0f);

                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 12.0f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;

                CreateButton(row.transform, "部屋を建てる",
                    () => OnJoinButton(Network.NetworkGameLauncher.JoinMode.Create));
                CreateButton(row.transform, "部屋に入る",
                    () => OnJoinButton(Network.NetworkGameLauncher.JoinMode.JoinOnly));
            }
        }

        /// <summary>
        /// 参加後のメンバー一覧
        /// </summary>
        void BuildJoinedGroup()
        {
            _joinedGroup = CreateVerticalGroup(_window.transform, "JoinedGroup");

            _roomNameText = CreateText(_joinedGroup.transform, "", 28.0f, TextColor);
            _roomNameText.fontStyle = FontStyles.Bold;
            AddLayoutElement(_roomNameText.gameObject, height: 36.0f);

            _roleText = CreateText(_joinedGroup.transform, "", 22.0f, SubTextColor);
            _roleText.textWrappingMode = TextWrappingModes.Normal;
            AddLayoutElement(_roleText.gameObject, height: 60.0f);

            _memberCountText = CreateText(_joinedGroup.transform, "メンバー", 24.0f, SubTextColor);
            AddLayoutElement(_memberCountText.gameObject, height: 32.0f);

            // 高さは人数で変わるため、決め打ちにしない
            _memberListRoot = CreateVerticalGroup(_joinedGroup.transform, "MemberList");

            var leave = CreateButton(_joinedGroup.transform, "部屋を抜ける", OnLeaveButton);
            AddLayoutElement(leave, height: 56.0f);
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
            var label = CreateText(parent, content, 22.0f, SubTextColor);
            AddLayoutElement(label.gameObject, height: 28.0f);
        }

        GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var obj = CreateUiObject($"Button_{label}", parent);
            var image = obj.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = ButtonColor;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.highlightedColor = new Color(0.96f, 0.90f, 0.80f);
            colors.pressedColor = new Color(0.88f, 0.80f, 0.68f);
            button.colors = colors;

            var text = CreateText(obj.transform, label, 26.0f, TextColor);
            text.alignment = TextAlignmentOptions.Center;
            StretchWithPadding(text.rectTransform, 0.0f, 0.0f);

            return obj;
        }

        TMP_InputField CreateInputField(Transform parent, string initial, int maxLength)
        {
            var obj = CreateUiObject("InputField", parent);
            AddLayoutElement(obj, height: 56.0f);

            var image = obj.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = FieldColor;

            var input = obj.AddComponent<TMP_InputField>();

            // 表示領域
            var viewport = CreateUiObject("TextArea", obj.transform);
            viewport.AddComponent<RectMask2D>();
            StretchWithPadding(viewport.GetComponent<RectTransform>(), 16.0f, 8.0f);

            var text = CreateText(viewport.transform, "", 26.0f, TextColor);
            StretchWithPadding(text.rectTransform, 0.0f, 0.0f);

            input.textViewport = viewport.GetComponent<RectTransform>();
            input.textComponent = text;
            input.fontAsset = _fontAsset;
            input.pointSize = 26.0f;
            input.characterLimit = maxLength;
            input.text = initial;

            return input;
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
        /// OS のフォントから日本語を表示できる動的フォントを作る
        /// (プロジェクトに日本語の TMP フォントが無いため)
        /// </summary>
        static TMP_FontAsset CreateJapaneseFontAsset()
        {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());

            foreach (var candidate in JapaneseFontCandidates)
            {
                if (!installed.Contains(candidate))
                {
                    continue;
                }

                // Font 経由では顔データを読めず失敗する。
                // OS フォントはファミリー名から直接作る必要がある。
                var asset = TMP_FontAsset.CreateFontAsset(candidate, "Regular", 64);
                if (asset != null)
                {
                    return asset;
                }
            }

            Debug.LogWarning("[NetworkRoomWindow] 日本語フォントが見つからないため、既定のフォントを使います");
            return TMP_Settings.defaultFontAsset;
        }
        #endregion

        #region private フィールド
        static NetworkRoomWindow _instance;

        const string TitleSceneName = "Title";
        const string SessionNamePrefix = "champan_";
        const string DefaultNickname = "PLAYER";
        const string DefaultPassphrase = "champan";
        const int DefaultLocalPlayerCount = 1;
        const int NicknameMaxLength = 8;
        const int PassphraseMaxLength = 16;
        const int CanvasSortingOrder = 500;
        const float WindowWidth = 460.0f;
        const float MemberRefreshIntervalSec = 0.5f;

        const string PrefKeyNickname = "NetworkRoom.Nickname";
        const string PrefKeyPassphrase = "NetworkRoom.Passphrase";
        const string PrefKeyLocalCount = "NetworkRoom.LocalCount";

        static readonly string[] JapaneseFontCandidates =
        {
            "Meiryo UI",
            "Meiryo",
            "Yu Gothic UI",
            "Yu Gothic",
            "MS UI Gothic",
            "MS Gothic",
        };

        static readonly Color PanelColor = new(1.00f, 0.97f, 0.90f, 0.97f);
        static readonly Color ButtonColor = new(1.00f, 1.00f, 1.00f, 1.00f);
        static readonly Color FieldColor = new(1.00f, 1.00f, 1.00f, 1.00f);
        static readonly Color RowColor = new(1.00f, 1.00f, 1.00f, 0.6f);
        static readonly Color SelfRowColor = new(1.00f, 0.92f, 0.75f, 1.0f);
        static readonly Color TextColor = new(0.36f, 0.27f, 0.20f, 1.0f);
        static readonly Color SubTextColor = new(0.55f, 0.47f, 0.40f, 1.0f);
        static readonly Color AccentColor = new(0.85f, 0.55f, 0.15f, 1.0f);
        static readonly Color ErrorColor = new(0.80f, 0.25f, 0.20f, 1.0f);

        GameObject _canvasRoot;
        GameObject _window;
        GameObject _formGroup;
        GameObject _joinedGroup;
        GameObject _memberListRoot;

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
