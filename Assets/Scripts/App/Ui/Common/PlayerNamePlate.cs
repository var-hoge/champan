using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Ui.Common
{
    /// <summary>
    /// 頭上の名札を、ニックネームを出せる形に組み替える
    ///
    /// 元の画像は「吹き出し・矢印・文字」が一枚に焼かれていて、文字を差し替えられない。
    /// そこで、文字を消した吹き出しと矢印を別々に用意し、その上に文字を重ねる。
    ///
    /// 吹き出しだけを 9 スライスで伸ばす。
    /// 元の吹き出しは内側が 65px しかなく、6 文字だと 1 文字 11px で読めない。
    ///
    /// ローカル対戦では元の画像のまま何もしない。変える理由がなく、壊す範囲も狭くなる。
    /// </summary>
    public class PlayerNamePlate
        : MonoBehaviour
    {
        #region メソッド
        /// <summary>
        /// 表示する名前を設定する
        ///
        /// 空を渡すと元の画像に戻る (ローカル対戦や CPU 席)。
        /// </summary>
        public void SetName(string playerName, int playerIdx)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                SetPlateVisible(false);
                return;
            }

            EnsureBuilt(playerIdx);

            if (_bubbleImage == null)
            {
                // 差し替え用の画像が無い。元の画像のままにする
                return;
            }

            SetPlateVisible(true);

            if (_label.text != playerName)
            {
                _label.text = playerName;
                ResizeToFit();
            }
        }

        /// <summary>
        /// 頭上の表示ごと出し入れする
        ///
        /// キャラが居ないときや画面外で待っているときに隠すために使う。
        /// 元の絵の表示に追従させる。
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(isVisible && _isPlateVisible);
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 元の画像と、組み替えた名札を出し分ける
        /// </summary>
        void SetPlateVisible(bool isVisible)
        {
            if (_isPlateVisible == isVisible)
            {
                return;
            }

            _isPlateVisible = isVisible;

            var baseImage = GetComponent<Image>();
            if (baseImage != null)
            {
                // 元の画像は消すのではなく透明にする。
                // 表示の切り替えは PlayerIdUi が Image の有効・無効で行っており、
                // ここで消すとその判断とぶつかる。
                baseImage.color = isVisible ? Color.clear : Color.white;
            }

            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }

        void EnsureBuilt(int playerIdx)
        {
            if (_root != null)
            {
                return;
            }

            var bubbleSprite = Resources.Load<Sprite>($"Ui/Cursor_Player{playerIdx + 1}_Bubble");
            var tailSprite = Resources.Load<Sprite>($"Ui/Cursor_Player{playerIdx + 1}_Tail");
            if (bubbleSprite == null || tailSprite == null)
            {
                Debug.LogWarning(
                    $"[PlayerNamePlate] 名札の画像が見つかりません: playerIdx={playerIdx}");
                return;
            }

            _root = new GameObject("NamePlate", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            // 元の絵の下端に合わせる。
            //
            // 中心で合わせるのは避ける。
            // 枠の高さは席によって違い (人間 80 / CPU 53)、
            // 中心を揃えると矢印の先がずれてキャラに被る。
            // 矢印が指す先は下端なので、そこを基準にする。
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.0f);
            rootRect.pivot = new Vector2(0.5f, 0.0f);
            rootRect.anchoredPosition = Vector2.zero;

            // 吹き出し (伸びる)
            var bubble = new GameObject("Bubble", typeof(RectTransform));
            bubble.transform.SetParent(_root.transform, false);
            _bubbleImage = bubble.AddComponent<Image>();
            _bubbleImage.sprite = bubbleSprite;
            _bubbleImage.type = Image.Type.Sliced;
            _bubbleImage.raycastTarget = false;

            // 上端を基準に置く。
            // 元の絵は「吹き出し 50 + 矢印 36 - 重なり 6 = 80」で組まれており、
            // 上端から積み上げると、その並びをそのまま再現できる。
            _bubbleRect = bubble.GetComponent<RectTransform>();
            _bubbleRect.anchorMin = _bubbleRect.anchorMax = new Vector2(0.5f, 1.0f);
            _bubbleRect.pivot = new Vector2(0.5f, 1.0f);
            _bubbleRect.anchoredPosition = Vector2.zero;

            // 文字
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(bubble.transform, false);
            _label = label.AddComponent<TextMeshProUGUI>();
            _label.font = Resources.Load<TMP_FontAsset>(FontResourcePath);
            _label.color = GetSeatColor(playerIdx);
            _label.alignment = TextAlignmentOptions.Center;

            // 文字を縮めるのではなく、吹き出しを広げて収める。
            // 縮める設定では測った幅と実際の描画が食い違い、
            // 吹き出しからはみ出したまま止まる。
            _label.enableAutoSizing = false;
            _label.fontSize = LabelFontSize;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.raycastTarget = false;

            var labelRect = _label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(LabelPaddingX, LabelPaddingBottom);
            labelRect.offsetMax = new Vector2(-LabelPaddingX, -LabelPaddingTop);

            // 矢印 (伸ばさない。吹き出しの縁に重ねて継ぎ目を隠す)
            var tail = new GameObject("Tail", typeof(RectTransform));
            tail.transform.SetParent(_root.transform, false);
            var tailImage = tail.AddComponent<Image>();
            tailImage.sprite = tailSprite;
            tailImage.raycastTarget = false;

            var tailRect = tail.GetComponent<RectTransform>();
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 1.0f);
            tailRect.pivot = new Vector2(0.5f, 1.0f);
            tailRect.sizeDelta = new Vector2(tailSprite.rect.width, tailSprite.rect.height);
            tailRect.anchoredPosition = new Vector2(0.0f, -(BubbleHeight - TailOverlap));

            ResizeToFit();
        }

        /// <summary>
        /// 名前の長さに合わせて吹き出しを伸ばす
        /// </summary>
        void ResizeToFit()
        {
            if (_bubbleRect == null)
            {
                return;
            }

            // 折り返さずに測る。
            // 測る前に反映させないと、前の文字のままの幅が返る。
            _label.ForceMeshUpdate();

            var preferred = _label.GetPreferredValues(_label.text, 0.0f, 0.0f).x;
            var width = Mathf.Max(BubbleWidthMin, preferred + LabelPaddingX * 2.0f);

            _bubbleRect.sizeDelta = new Vector2(width, BubbleHeight);

            // 元の絵と同じ高さにしておく。
            // 中心が揃うので、頭上に置く処理を変えずに済む。
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(width, PlateHeight);
        }

        /// <summary>
        /// 席の色
        ///
        /// 元の名札の絵から抜き出した値。
        /// 消した文字と同じ色で描くことで、元の見た目に揃う。
        /// </summary>
        static Color GetSeatColor(int playerIdx)
        {
            return playerIdx switch
            {
                0 => new Color32(239, 71, 111, 255),
                1 => new Color32(97, 170, 254, 255),
                2 => new Color32(37, 224, 104, 255),
                _ => new Color32(255, 151, 85, 255),
            };
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 吹き出しの元の大きさ (切り出した画像に合わせる)
        /// </summary>
        const float BubbleWidthMin = 115.0f;
        const float BubbleHeight = 50.0f;

        /// <summary>
        /// 矢印を吹き出しにどれだけ食い込ませるか
        /// 切り出す範囲を重ねてあるため、その分だけ上へ寄せる
        /// </summary>
        const float TailOverlap = 6.0f;

        /// <summary>
        /// 名札全体の高さ (元の絵と同じ)
        /// 吹き出し + 矢印 - 重なり
        /// </summary>
        const float PlateHeight = BubbleHeight + TailHeight - TailOverlap;

        const float TailHeight = 36.0f;

        const float LabelFontSize = 38.0f;

        /// <summary>
        /// 文字を白い部分に収めるための左右の余白
        ///
        /// 画像の端がそのまま白の端ではない。
        /// 吹き出しの外側には透明な余白があり (左 21px / 右 20〜30px)、
        /// そこまで文字を寄せると白からはみ出して見える。
        /// 透明な余白ぶんに、内側の詰め物を足した値にする。
        /// </summary>
        const float LabelPaddingX = SpriteMarginX + 8.0f;

        /// <summary>
        /// 吹き出しの絵の左右にある透明な余白
        /// </summary>
        const float SpriteMarginX = 30.0f;

        const float LabelPaddingTop = 2.0f;
        const float LabelPaddingBottom = 6.0f;

        const string FontResourcePath = "Fonts/Asap-ExtraBold SDF";

        GameObject _root;
        RectTransform _bubbleRect;
        Image _bubbleImage;
        TextMeshProUGUI _label;
        bool _isPlateVisible = false;
        #endregion
    }
}
