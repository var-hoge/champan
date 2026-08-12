using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace App.Ui.Common
{
    /// <summary>
    /// 自前で組み立てる UI の見た目と土台
    ///
    /// 部屋ウィンドウと音量ウィンドウで同じ見た目にしたい。
    /// 色や角丸をそれぞれが持つと、片方だけ直して食い違うため、
    /// ここに集めて一箇所から引く。
    ///
    /// 画像アセットを持ち込まずに済むよう、角丸やマウスの絵は直接描く。
    /// </summary>
    public static class GameUiStyle
    {
        #region 配色
        // タイトルの背景は明るい一枚絵のため、
        // 明るいウィンドウだと背景に溶けて読みにくい。
        // 暗い面に明るい文字を載せて、確実に浮かせる。
        public static readonly Color DimmerColor = new(0.05f, 0.03f, 0.02f, 0.55f);
        public static readonly Color PanelColor = new(0.16f, 0.12f, 0.09f, 0.97f);
        public static readonly Color PanelEdgeColor = new(0.87f, 0.72f, 0.48f, 1.00f);
        public static readonly Color FieldColor = new(0.25f, 0.20f, 0.15f, 1.00f);
        public static readonly Color ButtonColor = new(0.31f, 0.25f, 0.19f, 1.00f);
        public static readonly Color PrimaryButtonColor = new(0.91f, 0.66f, 0.25f, 1.00f);
        public static readonly Color LeaveButtonColor = new(0.44f, 0.22f, 0.18f, 1.00f);
        public static readonly Color RowColor = new(0.23f, 0.18f, 0.14f, 1.00f);
        public static readonly Color SelfRowColor = new(0.36f, 0.27f, 0.15f, 1.00f);
        public static readonly Color TextColor = new(0.96f, 0.93f, 0.87f, 1.00f);
        public static readonly Color SubTextColor = new(0.71f, 0.65f, 0.57f, 1.00f);
        public static readonly Color DarkTextColor = new(0.20f, 0.14f, 0.08f, 1.00f);
        public static readonly Color AccentColor = new(0.95f, 0.75f, 0.36f, 1.00f);
        public static readonly Color ErrorColor = new(1.00f, 0.56f, 0.48f, 1.00f);
        #endregion

        #region 寸法
        /// <summary>
        /// 画面の解像度に依存しないための基準
        /// </summary>
        public static readonly Vector2 ReferenceResolution = new(1920.0f, 1080.0f);

        /// <summary>
        /// 文字の隣に添えるマウスの絵の大きさ
        /// </summary>
        public const float MouseIconWidth = 26.0f;
        public const float MouseIconHeight = 38.0f;
        #endregion

        #region メソッド (部品)
        public static GameObject CreateUiObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        /// <summary>
        /// 画面いっぱいに広がる Canvas を作る
        /// </summary>
        public static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var root = CreateUiObject(name, parent);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            return root;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            TMP_FontAsset fontAsset,
            string content,
            float size,
            Color color)
        {
            var obj = CreateUiObject("Text", parent);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }

        /// <summary>
        /// ボタンを作る
        /// </summary>
        /// <param name="isPrimary">
        /// その画面で一番やってほしい操作かどうか。
        /// すべて同じ強さで並べると、どれを押せばよいか分からなくなる。
        /// </param>
        public static GameObject CreateButton(
            Transform parent,
            TMP_FontAsset fontAsset,
            Sprite roundedSprite,
            string label,
            UnityEngine.Events.UnityAction onClick,
            bool isPrimary = false)
        {
            var obj = CreateUiObject($"Button_{label}", parent);
            var image = obj.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = isPrimary ? PrimaryButtonColor : ButtonColor;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            button.colors = colors;

            var text = CreateText(
                obj.transform, fontAsset, label, 30.0f, isPrimary ? DarkTextColor : TextColor);
            text.alignment = TextAlignmentOptions.Center;
            StretchWithPadding(text.rectTransform, 0.0f, 0.0f);

            return obj;
        }

        /// <summary>
        /// ボタンの文字の隣にマウスの絵を添える
        ///
        /// マウスで触るものだと分かるようにする。
        ///
        /// 位置は数値で寄せずに並べて決める。
        /// 文字の幅はフォントや表示倍率で変わるため、
        /// 数値で寄せると環境によってずれる。
        /// </summary>
        public static void AddMouseIcon(GameObject button)
        {
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

        public static void AddLayoutElement(GameObject obj, float height = -1.0f, float width = -1.0f)
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

        public static void StretchWithPadding(RectTransform rect, float paddingX, float paddingY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingX, paddingY);
            rect.offsetMax = new Vector2(-paddingX, -paddingY);
        }
        #endregion

        #region メソッド (絵とフォント)
        /// <summary>
        /// 角丸の 9 スライス用スプライトを作る
        /// (画像アセットを持ち込まずに、柔らかい見た目にするため)
        /// </summary>
        public static Sprite CreateRoundedSprite()
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
        /// 丸いスプライトを作る (つまみなどに使う)
        /// </summary>
        public static Sprite CreateCircleSprite()
        {
            const int size = 48;
            var radius = size * 0.5f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(radius, radius);

            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// マウスの絵を作る
        /// </summary>
        public static Sprite CreateMouseSprite()
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
        /// ゲームで使っているフォントを読む
        ///
        /// 日本語は入っていないため、表示は英数字だけにしてある。
        /// </summary>
        public static TMP_FontAsset LoadGameFontAsset()
        {
            var asset = Resources.Load<TMP_FontAsset>(GameFontResourcePath);
            if (asset != null)
            {
                return asset;
            }

            Debug.LogWarning(
                $"[GameUiStyle] フォントが見つからないため既定のものを使います: Resources/{GameFontResourcePath}");
            return TMP_Settings.defaultFontAsset;
        }
        #endregion

        #region メソッド (入力)
        /// <summary>
        /// マウスで UI を操作できる状態にする
        ///
        /// この作品の EventSystem は、入力を読む設定が空のまま置かれている。
        /// そのままではボタンを押しても何も起きない。
        /// 画面ごとに EventSystem が違うため、開くたびに確かめる。
        /// </summary>
        public static void EnsureUiInputReady()
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

            Debug.Log("[GameUiStyle] UI の入力設定が空だったため、マウス操作を割り当てました");
        }

        /// <summary>
        /// コントローラの決定 / キャンセルが押されたか
        ///
        /// どのボタンでも閉じると、トリガーや肩ボタンに触れただけで閉じてしまう。
        /// 閉じる意思が読み取れる 2 つに絞る。
        /// </summary>
        public static bool IsGamepadCloseButtonPressed()
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
        /// マウスで UI を触るのに要る最小限の入力を組み立てる
        ///
        /// 既存の入力アセットに頼らないのは、
        /// 画面ごとに設定の有無がまちまちで、当てにできないため。
        /// </summary>
        static InputActionAsset BuildUiActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "GameUiStyleUI";

            var map = asset.AddActionMap("UI");
            map.AddAction(PointActionName, InputActionType.PassThrough, "<Mouse>/position");
            map.AddAction(ClickActionName, InputActionType.PassThrough, "<Mouse>/leftButton");
            map.AddAction(ScrollActionName, InputActionType.PassThrough, "<Mouse>/scroll");

            asset.Enable();

            return asset;
        }
        #endregion

        #region private フィールド
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

        /// <summary>
        /// ゲーム本体と同じフォント
        /// </summary>
        const string GameFontResourcePath = "Fonts/Asap-ExtraBold SDF";
        #endregion
    }
}
