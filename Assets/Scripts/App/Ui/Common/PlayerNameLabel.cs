using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Ui.Common
{
    /// <summary>
    /// 「1P」の絵を、ニックネームの文字に差し替える
    ///
    /// この絵は枠ではなく文字そのもの (色付きの文字に濃い縁) で、
    /// 頭上の名札のように文字だけを抜くことができない。
    /// そこで絵を隠し、同じ配色の文字を代わりに描く。
    /// </summary>
    public class PlayerNameLabel
        : MonoBehaviour
    {
        #region メソッド
        /// <summary>
        /// 表示する名前を設定する
        ///
        /// 空を渡すと元の絵に戻る (ローカル対戦や CPU 席)。
        /// </summary>
        public void SetName(string playerName, int playerIdx)
        {
            var image = GetComponent<Image>();
            var hasName = !string.IsNullOrEmpty(playerName);

            if (image != null)
            {
                image.enabled = !hasName;
            }

            if (!hasName)
            {
                if (_backgroundImage != null)
                {
                    _backgroundImage.gameObject.SetActive(false);
                }

                return;
            }

            EnsureBuilt(playerIdx);

            _backgroundImage.gameObject.SetActive(true);

            if (_label.text != playerName)
            {
                _label.text = playerName;
                ResizeToFit();
            }
        }
        #endregion

        #region private メソッド
        void EnsureBuilt(int playerIdx)
        {
            if (_label != null)
            {
                return;
            }

            // 白い下地を敷く。
            // 背景に溶け込んで読めないため、頭上の名札と同じ絵を使う。
            // (あの絵の吹き出しは色を持たない白なので、席によらず流用できる)
            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(transform, false);

            _backgroundImage = background.AddComponent<Image>();
            _backgroundImage.sprite = Resources.Load<Sprite>(BackgroundResourcePath);
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.raycastTarget = false;

            _backgroundRect = background.GetComponent<RectTransform>();
            _backgroundRect.anchorMin = _backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            _backgroundRect.anchoredPosition = Vector2.zero;

            var obj = new GameObject("NameLabel", typeof(RectTransform));
            obj.transform.SetParent(background.transform, false);

            _label = obj.AddComponent<TextMeshProUGUI>();
            _label.font = Resources.Load<TMP_FontAsset>(FontResourcePath);
            _label.color = GetTextColor(playerIdx);
            _label.alignment = TextAlignmentOptions.Center;

            // 文字を縮めるのではなく、下地を広げて収める。
            // 縮める設定では測った幅と実際の描画が食い違う。
            _label.enableAutoSizing = false;
            _label.fontSize = FontSize;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.raycastTarget = false;

            // 元の絵と同じく、濃い縁を付ける
            _label.fontMaterial.EnableKeyword("OUTLINE_ON");
            _label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
            _label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);

            var rect = _label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(LabelPaddingX, 0.0f);
            rect.offsetMax = new Vector2(-LabelPaddingX, 0.0f);
        }

        /// <summary>
        /// 名前の長さに合わせて下地を広げる
        /// </summary>
        void ResizeToFit()
        {
            if (_backgroundRect == null)
            {
                return;
            }

            // 測る前に反映させないと、前の文字のままの幅が返る
            _label.ForceMeshUpdate();

            var preferred = _label.GetPreferredValues(_label.text, 0.0f, 0.0f).x;
            var width = Mathf.Max(BackgroundWidthMin, preferred + LabelPaddingX * 2.0f);

            _backgroundRect.sizeDelta = new Vector2(width, BackgroundHeight);
        }

        /// <summary>
        /// 文字の色
        ///
        /// 元の絵から抜き出した値。
        /// 頭上の名札とは別の配色になっている。
        /// </summary>
        static Color GetTextColor(int playerIdx)
        {
            return playerIdx switch
            {
                0 => new Color32(97, 170, 254, 255),
                1 => new Color32(239, 71, 111, 255),
                2 => new Color32(6, 214, 160, 255),
                _ => new Color32(255, 209, 102, 255),
            };
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 元の絵の縁の色
        /// </summary>
        static readonly Color OutlineColor = new Color32(92, 85, 77, 255);

        const float OutlineWidth = 0.2f;
        const float FontSize = 30.0f;

        /// <summary>
        /// 文字を白い部分に収めるための左右の余白
        ///
        /// 下地の絵の端がそのまま白の端ではない。
        /// 外側には透明な余白があるため、そのぶんを含める。
        /// </summary>
        const float LabelPaddingX = SpriteMarginX + 8.0f;
        const float SpriteMarginX = 30.0f;

        const float BackgroundWidthMin = 115.0f;
        const float BackgroundHeight = 50.0f;

        const string BackgroundResourcePath = "Ui/Cursor_Player1_Bubble";
        const string FontResourcePath = "Fonts/Asap-ExtraBold SDF";

        Image _backgroundImage;
        RectTransform _backgroundRect;
        TextMeshProUGUI _label;
        #endregion
    }
}
