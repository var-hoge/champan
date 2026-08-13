using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Ui.Common
{
    /// <summary>
    /// ネットワーク対戦の様子を画面の隅に出す
    ///
    /// 部屋の出入りは、今まで画面のどこにも出ていなかった。
    /// 誰が来て誰が抜けたのか分からず、
    /// ホストの操作を待っている場面では止まっているようにしか見えなかった。
    ///
    /// 出すのは 2 種類。
    /// - 知らせ: 出入りのような、起きた瞬間だけ伝わればよいもの。少し置いて消える
    /// - 状態: 「ホストの操作待ち」のような、続いている間ずっと伝えたいもの
    ///
    /// 表示は英数字だけにする。ゲーム本体のフォントに日本語が入っていないため。
    /// </summary>
    public class NetworkNoticeUi
        : MonoBehaviour
    {
        #region 定数
        /// <summary>
        /// ホストの操作を待っている間に出す文言
        ///
        /// ルール選択・スコア表・最終結果で同じものを出す。
        /// 場面ごとに書き分けると、待たされている側から見れば同じことなのに
        /// 文言だけ変わって落ち着かない。
        ///
        /// 文字列を各所に散らすと必ず食い違うため、ここに置く。
        /// </summary>
        public const string StatusWaitingForHost = "WAITING FOR HOST...";
        #endregion

        #region メソッド
        /// <summary>
        /// 知らせを出す (少し置いて消える)
        /// </summary>
        public static void Post(string message)
        {
            if (_instance == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            _instance.AddNotice(message);
        }

        /// <summary>
        /// 続いている状態を出す
        /// </summary>
        /// <param name="status">空にすると消える</param>
        public static void SetStatus(string status)
        {
            if (_instance == null)
            {
                return;
            }

            _instance._status = status ?? "";
        }

        public static void ClearStatus()
        {
            SetStatus("");
        }

        /// <summary>
        /// シーンに置かずに自分で現れる
        /// 出したい場面が複数のシーンにまたがるため
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateSelf()
        {
            if (_instance != null)
            {
                return;
            }

            var obj = new GameObject(nameof(NetworkNoticeUi));
            DontDestroyOnLoad(obj);
            obj.AddComponent<NetworkNoticeUi>();
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
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 画面が変わったら状態を消す
        ///
        /// 状態は出しっぱなしにできるぶん、消し忘れると次の画面まで居座る。
        /// 出したい側が毎回消さなくて済むよう、ここでまとめて落とす。
        /// </summary>
        void OnActiveSceneChanged(
            UnityEngine.SceneManagement.Scene prev,
            UnityEngine.SceneManagement.Scene next)
        {
            _status = "";
        }

        void Update()
        {
            // オフラインでは出すものが無い
            var isOnline = Network.NetworkSession.IsOnline;
            if (_canvasRoot.activeSelf != isOnline)
            {
                _canvasRoot.SetActive(isOnline);
            }

            if (!isOnline)
            {
                Reset();
                return;
            }

            WatchMembers();
            RefreshPlacement();
            RefreshStatus();
            RefreshNotices();
        }
        #endregion

        #region private メソッド (置き場所)
        /// <summary>
        /// 出す位置と、出すかどうかを決める
        ///
        /// 同じ隅にはウィンドウの開くボタンが出る画面と、出ない画面がある。
        /// 位置を決め打ちにすると、ボタンが無い画面では上が不自然に空き、
        /// ウィンドウを開いたときには重なってしまう。
        /// </summary>
        void RefreshPlacement()
        {
            // 同じ隅を使っている表示の、一番下に合わせる。
            // ボタンだけのときはその下、ウィンドウを開いたらウィンドウの下。
            //
            // ウィンドウの高さは中身で変わるため、決め打ちにはできない。
            // 出している側に下端を教えてもらう。
            var occupiedBottom = Mathf.Min(
                CharaSelect.NetworkRoomWindow.OccupiedBottomY,
                Title.VolumeSettingsWindow.OccupiedBottomY);

            var posY = occupiedBottom < 0.0f
                ? occupiedBottom - GapY
                : PosYTop;

            var rect = _noticeRoot.GetComponent<RectTransform>();
            if (!Mathf.Approximately(rect.anchoredPosition.y, posY))
            {
                rect.anchoredPosition = new Vector2(PosX, posY);
            }
        }
        #endregion

        #region private メソッド (状態)
        void Reset()
        {
            _notices.Clear();
            _knownMembers.Clear();
            _isMemberBaselineReady = false;
            _status = "";
        }

        /// <summary>
        /// 部屋の出入りを見張る
        ///
        /// 出入りそのものを知らせる仕組みは無いので、名簿の変化から拾う。
        /// </summary>
        void WatchMembers()
        {
            var seatTable = Network.NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                // 部屋を出たら次に入ったときに出し直す
                _knownMembers.Clear();
                _isMemberBaselineReady = false;
                return;
            }

            _currentMembers.Clear();

            // 自分の出入りは知らせない。
            //
            // 自分で部屋を建てた / 入ったことは操作した本人が分かっている。
            // また、名簿に自分が載るのは部屋に入った少し後になるため、
            // 最初の一覧を控えるだけでは間に合わず、自分の分だけ流れてしまう。
            var runner = Network.NetworkSession.Runner;
            var localPlayerId = runner != null ? runner.LocalPlayer.PlayerId : -1;

            var members = seatTable.Members;
            for (int idx = 0; idx < members.Length; ++idx)
            {
                var member = members[idx];
                if (member.IsEmpty || member.Owner.PlayerId == localPlayerId)
                {
                    continue;
                }

                _currentMembers[member.Owner.PlayerId] = member.Nickname.ToString();
            }

            // 入った時点で既に居た人まで知らせると、入室のたびに一斉に流れてしまう。
            // 最初の一回は控えるだけにする。
            if (!_isMemberBaselineReady)
            {
                _isMemberBaselineReady = true;
                CopyMembers();
                return;
            }

            foreach (var entry in _currentMembers)
            {
                if (!_knownMembers.ContainsKey(entry.Key))
                {
                    AddNotice($"{ToDisplayName(entry.Value)} JOINED");
                }
            }

            foreach (var entry in _knownMembers)
            {
                if (!_currentMembers.ContainsKey(entry.Key))
                {
                    AddNotice($"{ToDisplayName(entry.Value)} LEFT");
                }
            }

            CopyMembers();
        }

        void CopyMembers()
        {
            _knownMembers.Clear();
            foreach (var entry in _currentMembers)
            {
                _knownMembers[entry.Key] = entry.Value;
            }
        }

        static string ToDisplayName(string nickname)
        {
            return string.IsNullOrEmpty(nickname) ? "PLAYER" : nickname;
        }

        void AddNotice(string message)
        {
            _notices.Insert(0, new Notice
            {
                Message = message,
                RemainSec = NoticeLifeSec,
                AgeSec = 0.0f,
            });

            while (_notices.Count > NoticeCountMax)
            {
                _notices.RemoveAt(_notices.Count - 1);
            }
        }

        void RefreshStatus()
        {
            var isVisible = _status.Length > 0;
            if (_statusRoot.activeSelf != isVisible)
            {
                _statusRoot.SetActive(isVisible);
            }

            if (isVisible)
            {
                ApplyRowWidth(_statusRow, _status);
            }
        }

        void RefreshNotices()
        {
            for (int idx = _notices.Count - 1; idx >= 0; --idx)
            {
                var notice = _notices[idx];
                notice.RemainSec -= Time.unscaledDeltaTime;
                notice.AgeSec += Time.unscaledDeltaTime;
                _notices[idx] = notice;

                if (notice.RemainSec <= 0.0f)
                {
                    _notices.RemoveAt(idx);
                }
            }

            for (int idx = 0; idx < _noticeRows.Count; ++idx)
            {
                var row = _noticeRows[idx];

                if (idx >= _notices.Count)
                {
                    if (row.Root.activeSelf)
                    {
                        row.Root.SetActive(false);
                    }

                    continue;
                }

                var notice = _notices[idx];

                if (!row.Root.activeSelf)
                {
                    row.Root.SetActive(true);
                }

                ApplyRowWidth(row, notice.Message);

                // 消える間際だけ薄くする。
                // ずっと薄いと読みにくく、急に消えると気づけない。
                var fadeRate = Mathf.Clamp01(notice.RemainSec / NoticeFadeSec);

                // 出るときは左から滑り込ませる。通知でよく使われる出方。
                var appearRate = Mathf.Clamp01(notice.AgeSec / NoticeAppearSec);
                var easedRate = 1.0f - Mathf.Pow(1.0f - appearRate, 3.0f);

                row.Group.alpha = fadeRate * appearRate;
                row.Body.anchoredPosition = new Vector2(
                    Mathf.Lerp(-SlideDistance, 0.0f, easedRate), 0.0f);
            }
        }
        #endregion

        #region private メソッド (UI 構築)
        void BuildUi()
        {
            _roundedSprite = GameUiStyle.CreateRoundedSprite();
            _fontAsset = GameUiStyle.LoadGameFontAsset();

            _canvasRoot = GameUiStyle.CreateCanvas("NetworkNoticeCanvas", transform, CanvasSortingOrder);

            _noticeRoot = GameUiStyle.CreateUiObject("Notices", _canvasRoot.transform);

            var rect = _noticeRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(PosX, PosYTop);

            var layout = _noticeRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6.0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // 横に引き伸ばさない。
            // 引き伸ばすと、短い文でも枠だけ長くなって間延びして見える
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = _noticeRoot.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildStatusRow(_noticeRoot.transform);

            for (int idx = 0; idx < NoticeCountMax; ++idx)
            {
                _noticeRows.Add(BuildNoticeRow(_noticeRoot.transform));
            }

            _canvasRoot.SetActive(false);
        }

        /// <summary>
        /// 続いている状態の行
        ///
        /// 流れていく知らせと見分けが付くよう、明るい面で目立たせる。
        /// </summary>
        void BuildStatusRow(Transform parent)
        {
            _statusRow = BuildRow(
                parent, "Status", GameUiStyle.PrimaryButtonColor, GameUiStyle.DarkTextColor);

            _statusRoot = _statusRow.Root;
            _statusRow.Text.fontStyle = FontStyles.Bold;

            _statusRoot.SetActive(false);
        }

        NoticeRow BuildNoticeRow(Transform parent)
        {
            var row = BuildRow(parent, "Notice", GameUiStyle.PanelColor, GameUiStyle.TextColor);
            row.Root.SetActive(false);

            return row;
        }

        /// <summary>
        /// 一行を作る
        ///
        /// 幅は文字に合わせて縮める。
        /// 決め打ちにすると、短い文でも枠だけ長くなって間延びして見える。
        /// </summary>
        /// <summary>
        /// 一行を作る
        ///
        /// 並べる枠 (Root) と、見た目 (Body) を分けてある。
        /// Root の位置は並べ方が決めるため、そこを動かしても戻されてしまう。
        /// 滑り込ませる動きは、中の Body をずらして出す。
        /// </summary>
        NoticeRow BuildRow(Transform parent, string name, Color backColor, Color textColor)
        {
            var obj = GameUiStyle.CreateUiObject(name, parent);

            var layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = RowHeight;
            layoutElement.minHeight = RowHeight;

            var body = GameUiStyle.CreateUiObject("Body", obj.transform);
            var bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.0f, 0.5f);
            bodyRect.pivot = new Vector2(0.0f, 0.5f);

            var image = body.AddComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = backColor;
            image.raycastTarget = false;

            // 左端に色の帯を入れる。
            // 流れてくるものだと分かる目印で、通知でよく使われる形。
            var bar = GameUiStyle.CreateUiObject("Bar", body.transform);
            var barImage = bar.AddComponent<Image>();
            barImage.color = GameUiStyle.AccentColor;
            barImage.raycastTarget = false;

            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.0f, 0.0f);
            barRect.anchorMax = new Vector2(0.0f, 1.0f);
            barRect.pivot = new Vector2(0.0f, 0.5f);
            barRect.offsetMin = new Vector2(0.0f, BarInsetY);
            barRect.offsetMax = new Vector2(BarWidth, -BarInsetY);

            var text = GameUiStyle.CreateText(
                body.transform, _fontAsset, "", FontSize, textColor);
            text.raycastTarget = false;

            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.0f, 0.0f);
            textRect.anchorMax = new Vector2(1.0f, 1.0f);
            textRect.offsetMin = new Vector2(TextLeft, 0.0f);
            textRect.offsetMax = new Vector2(-PaddingX, 0.0f);

            var group = obj.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            return new NoticeRow
            {
                Root = obj,
                Body = bodyRect,
                LayoutElement = layoutElement,
                Text = text,
                Group = group,
            };
        }

        /// <summary>
        /// 文字に合わせて枠の幅を決める
        ///
        /// 決め打ちにすると、短い文でも枠だけ長くなって間延びして見える。
        /// </summary>
        void ApplyRowWidth(NoticeRow row, string message)
        {
            row.Text.text = message;

            var width = TextLeft + row.Text.preferredWidth + PaddingX;

            row.LayoutElement.preferredWidth = width;
            row.LayoutElement.minWidth = width;
            row.Body.sizeDelta = new Vector2(width, RowHeight);
        }
        #endregion

        #region private フィールド
        static NetworkNoticeUi _instance;

        struct Notice
        {
            public string Message;
            public float RemainSec;

            /// <summary>
            /// 出てからの経過。出るときの動きに使う
            /// </summary>
            public float AgeSec;
        }

        class NoticeRow
        {
            /// <summary>
            /// 並べる枠。位置は並べ方が決める
            /// </summary>
            public GameObject Root;

            /// <summary>
            /// 見た目。滑り込ませるときはこちらをずらす
            /// </summary>
            public RectTransform Body;

            public LayoutElement LayoutElement;
            public TextMeshProUGUI Text;
            public CanvasGroup Group;
        }

        const int CanvasSortingOrder = 480;

        const float PosX = 24.0f;

        /// <summary>
        /// 隅にボタンが無い画面での高さ
        /// </summary>
        const float PosYTop = -24.0f;

        /// <summary>
        /// 上にある表示との隙間
        /// </summary>
        const float GapY = 12.0f;

        const float RowHeight = 48.0f;
        const float FontSize = 27.0f;
        const float PaddingX = 16.0f;

        /// <summary>
        /// 左端の色帯の太さ
        /// </summary>
        const float BarWidth = 5.0f;

        /// <summary>
        /// 色帯を上下から少し詰める量 (角丸からはみ出さないように)
        /// </summary>
        const float BarInsetY = 6.0f;

        /// <summary>
        /// 文字の左端 (色帯のぶん空ける)
        /// </summary>
        const float TextLeft = BarWidth + PaddingX;

        /// <summary>
        /// 出るときの動きの長さ
        /// </summary>
        const float NoticeAppearSec = 0.25f;

        /// <summary>
        /// 出るときに左から滑り込ませる距離
        /// </summary>
        const float SlideDistance = 40.0f;

        /// <summary>
        /// 知らせを出しておく長さ
        /// </summary>
        const float NoticeLifeSec = 4.5f;

        /// <summary>
        /// 消える間際に薄くしはじめる長さ
        /// </summary>
        const float NoticeFadeSec = 0.6f;

        const int NoticeCountMax = 4;

        GameObject _canvasRoot;
        GameObject _noticeRoot;
        GameObject _statusRoot;
        NoticeRow _statusRow;

        readonly List<NoticeRow> _noticeRows = new();
        readonly List<Notice> _notices = new();

        string _status = "";

        /// <summary>
        /// 分かっている部屋のメンバー (台の番号 → ニックネーム)
        /// </summary>
        readonly Dictionary<int, string> _knownMembers = new();
        readonly Dictionary<int, string> _currentMembers = new();

        /// <summary>
        /// 最初の一覧を控えたか
        /// </summary>
        bool _isMemberBaselineReady = false;

        Sprite _roundedSprite;
        TMP_FontAsset _fontAsset;
        #endregion
    }
}
