using App.Ui.Common;
using KanKikuchi.AudioManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Ui.Title
{
    /// <summary>
    /// タイトル画面の音量調整ウィンドウ
    ///
    /// タイトルのメニューはコントローラで選ぶ作りのため、
    /// そこに項目を足すと選択の流れを変えることになる。
    /// 音量は遊ぶ前に一度決めるものなので、
    /// 画面隅のボタンから開く別のウィンドウに分けた。
    ///
    /// 見た目は部屋ウィンドウと同じ GameUiStyle から引く。
    /// </summary>
    public class VolumeSettingsWindow
        : MonoBehaviour
    {
        #region メソッド
        /// <summary>
        /// シーンに置かずに自分で現れる
        ///
        /// タイトルのシーンに部品を足すと、
        /// シーンを触るたびに壊れる余地が増えるため。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateSelf()
        {
            if (_instance != null)
            {
                return;
            }

            var obj = new GameObject(nameof(VolumeSettingsWindow));
            DontDestroyOnLoad(obj);
            obj.AddComponent<VolumeSettingsWindow>();
        }
        #endregion

        #region MonoBehaviour の実装
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            BuildUi();

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ApplySceneVisibility(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        void Update()
        {
            // 開くボタンを押せる状態にしておく
            GameUiStyle.EnsureUiInputReady();

            if (_window == null || !_window.activeSelf)
            {
                return;
            }

            // ゲームを遊ぶ手のまま閉じられるようにする。
            // 閉じるためにマウスへ持ち替えるのが煩わしいため。
            if (GameUiStyle.IsGamepadCloseButtonPressed())
            {
                SetWindowOpen(false);
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
        /// </summary>
        void ApplySceneVisibility(string sceneName)
        {
            var isTitle = sceneName == TitleSceneName;

            _canvasRoot.SetActive(isTitle);

            if (!isTitle)
            {
                SetWindowOpen(false);
                return;
            }

            GameUiStyle.EnsureUiInputReady();
        }

        void SetWindowOpen(bool isOpen)
        {
            if (isOpen)
            {
                GameUiStyle.EnsureUiInputReady();

                // 開いた時点の値に合わせる
                // (他の場所で変わっていても食い違わないように)
                _slider.SetValueWithoutNotify(Sound.VolumeSettings.Step);
                RefreshValueText();
            }

            var wasOpen = _window.activeSelf;

            _window.SetActive(isOpen);
            _dimmer.SetActive(isOpen);

            // 開いている間は裏でタイトルの選択が進まないようにする
            TadaLib.Input.PlayerInputProxy.IsSuppressed = isOpen;

            // つまみを動かし終えたので、覚えた値を書き出す。
            // 閉じたときだけでよい (組み立て時にも通るため)
            if (wasOpen && !isOpen)
            {
                Sound.VolumeSettings.Save();
            }
        }

        void OnSliderChanged(float value)
        {
            var step = Mathf.RoundToInt(value);
            if (step == Sound.VolumeSettings.Step)
            {
                return;
            }

            Sound.VolumeSettings.SetStep(step);
            RefreshValueText();

            // 今どれくらいの大きさかは、鳴らさないと分からない。
            // 段階が変わったときだけ鳴らすので、重なって濁ることはない。
            SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
        }

        void RefreshValueText()
        {
            _valueText.text = Sound.VolumeSettings.Step.ToString();
        }
        #endregion

        #region private メソッド (UI 構築)
        void BuildUi()
        {
            _roundedSprite = GameUiStyle.CreateRoundedSprite();
            _circleSprite = GameUiStyle.CreateCircleSprite();
            _fontAsset = GameUiStyle.LoadGameFontAsset();

            _canvasRoot = GameUiStyle.CreateCanvas("VolumeSettingsCanvas", transform, CanvasSortingOrder);

            // 幕は開くボタンより先に作って、ボタンが幕の上に残るようにする
            BuildDimmer();
            BuildToggleButton();
            BuildWindow();

            SetWindowOpen(false);
        }

        void BuildDimmer()
        {
            _dimmer = GameUiStyle.CreateUiObject("Dimmer", _canvasRoot.transform);

            var image = _dimmer.AddComponent<Image>();
            image.color = GameUiStyle.DimmerColor;

            // ウィンドウの外を押したら閉じる。
            // ウィンドウ自身はこの上に載っているため、そちらの操作は奪わない。
            var button = _dimmer.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SetWindowOpen(false));

            GameUiStyle.StretchWithPadding(_dimmer.GetComponent<RectTransform>(), 0.0f, 0.0f);
        }

        /// <summary>
        /// 画面隅の「音量」ボタン
        /// </summary>
        void BuildToggleButton()
        {
            var button = GameUiStyle.CreateButton(
                _canvasRoot.transform,
                _fontAsset,
                _roundedSprite,
                "SOUND",
                () => SetWindowOpen(!_window.activeSelf));

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -24.0f);
            rect.sizeDelta = new Vector2(230.0f, 70.0f);

            GameUiStyle.AddMouseIcon(button);
        }

        void BuildWindow()
        {
            _window = GameUiStyle.CreateUiObject("Window", _canvasRoot.transform);

            var windowImage = _window.AddComponent<Image>();
            windowImage.sprite = _roundedSprite;
            windowImage.type = Image.Type.Sliced;
            windowImage.color = GameUiStyle.PanelColor;

            // 暗い面がそのまま背景に沈まないよう、明るい縁で輪郭を出す
            var outline = _window.AddComponent<Outline>();
            outline.effectColor = GameUiStyle.PanelEdgeColor;
            outline.effectDistance = new Vector2(3.0f, 3.0f);
            outline.useGraphicAlpha = false;

            var rect = _window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(24.0f, -110.0f);
            rect.sizeDelta = new Vector2(WindowWidth, 0.0f);

            var layout = _window.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 24);
            layout.spacing = 14.0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 中身の量に合わせて枠を伸ばす
            var fitter = _window.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = GameUiStyle.CreateText(
                _window.transform, _fontAsset, "SOUND", 39.0f, GameUiStyle.TextColor);
            title.fontStyle = FontStyles.Bold;
            GameUiStyle.AddLayoutElement(title.gameObject, height: 48.0f);

            // BGM と SE は分けずに、まとめて一つのつまみで扱う
            (_slider, _valueText) = BuildSliderRow("VOLUME", OnSliderChanged);

            var closeButton = GameUiStyle.CreateButton(
                _window.transform,
                _fontAsset,
                _roundedSprite,
                "CLOSE",
                () => SetWindowOpen(false),
                isPrimary: true);

            GameUiStyle.AddLayoutElement(closeButton, height: 64.0f);

            RefreshValueText();
        }

        /// <summary>
        /// 「名前 + つまみ + 数字」の一行を作る
        /// </summary>
        (Slider slider, TextMeshProUGUI valueText) BuildSliderRow(
            string label,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            var row = GameUiStyle.CreateUiObject($"Row_{label}", _window.transform);
            GameUiStyle.AddLayoutElement(row, height: RowHeight);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14.0f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var labelText = GameUiStyle.CreateText(
                row.transform, _fontAsset, label, 30.0f, GameUiStyle.TextColor);
            GameUiStyle.AddLayoutElement(labelText.gameObject, width: LabelWidth);

            var slider = BuildSlider(row.transform);

            // つまみの幅は残りいっぱいに広げる。
            // 数値で決めると、文字の幅が変わったときにはみ出す。
            var sliderElement = slider.GetComponent<LayoutElement>();
            sliderElement.flexibleWidth = 1.0f;

            var valueText = GameUiStyle.CreateText(
                row.transform, _fontAsset, "", 28.0f, GameUiStyle.AccentColor);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            GameUiStyle.AddLayoutElement(valueText.gameObject, width: ValueWidth);

            slider.onValueChanged.AddListener(onChanged);

            return (slider, valueText);
        }

        /// <summary>
        /// つまみを作る
        ///
        /// 溝・たまった色・つまみの 3 枚で組む。
        /// </summary>
        Slider BuildSlider(Transform parent)
        {
            var obj = GameUiStyle.CreateUiObject("Slider", parent);
            GameUiStyle.AddLayoutElement(obj, height: SliderHeight);

            var slider = obj.AddComponent<Slider>();
            slider.minValue = 0.0f;
            slider.maxValue = Sound.VolumeSettings.StepMax;

            // 細かく刻んでも聞き分けられないため、段階で動かす
            slider.wholeNumbers = true;

            // 溝
            var background = GameUiStyle.CreateUiObject("Background", obj.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = _roundedSprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = GameUiStyle.FieldColor;
            SetBarRect(background.GetComponent<RectTransform>());

            // たまった色
            var fillArea = GameUiStyle.CreateUiObject("FillArea", obj.transform);
            SetBarRect(fillArea.GetComponent<RectTransform>());

            var fill = GameUiStyle.CreateUiObject("Fill", fillArea.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = _roundedSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = GameUiStyle.PrimaryButtonColor;

            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            // つまみ
            //
            // Slider はつまみを縦いっぱいに引き伸ばす作りで、
            // 高さは「入れ物の高さ + sizeDelta.y」で決まる。
            // そのため、つまみ自身ではなく入れ物の高さで大きさを決める。
            // (入れ物を行いっぱいに広げると、丸が縦に伸びて巨大になる)
            var handleArea = GameUiStyle.CreateUiObject("HandleArea", obj.transform);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0.0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1.0f, 0.5f);
            handleAreaRect.pivot = new Vector2(0.5f, 0.5f);

            // 左右はつまみの半分だけ内側に寄せて、端でもはみ出さないようにする
            handleAreaRect.sizeDelta = new Vector2(-HandleSize, HandleSize);
            handleAreaRect.anchoredPosition = Vector2.zero;

            var handle = GameUiStyle.CreateUiObject("Handle", handleArea.transform);
            var handleImage = handle.AddComponent<Image>();
            handleImage.sprite = _circleSprite;
            handleImage.color = GameUiStyle.TextColor;

            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(HandleSize, 0.0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.SetValueWithoutNotify(Sound.VolumeSettings.Step);

            return slider;
        }

        /// <summary>
        /// 溝の見た目を、行の中で細い帯にする
        /// </summary>
        static void SetBarRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.0f, 0.5f);
            rect.anchorMax = new Vector2(1.0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0.0f, BarHeight);
            rect.anchoredPosition = Vector2.zero;
        }
        #endregion

        #region private フィールド
        static VolumeSettingsWindow _instance;

        const string TitleSceneName = "Title";

        const int CanvasSortingOrder = 500;
        const float WindowWidth = 500.0f;
        const float RowHeight = 52.0f;
        const float SliderHeight = 32.0f;
        const float BarHeight = 10.0f;

        /// <summary>
        /// つまみの直径
        /// </summary>
        const float HandleSize = 22.0f;

        const float LabelWidth = 128.0f;
        const float ValueWidth = 36.0f;

        GameObject _canvasRoot;
        GameObject _dimmer;
        GameObject _window;

        Slider _slider;
        TextMeshProUGUI _valueText;

        Sprite _roundedSprite;
        Sprite _circleSprite;
        TMP_FontAsset _fontAsset;
        #endregion
    }
}
