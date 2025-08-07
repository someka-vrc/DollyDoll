using UnityEngine;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using UnityEditor;
using UniRx;
using System.Linq;
using UnityEngine.UIElements;

using System.Text.RegularExpressions;

namespace Somekasu.DollyDoll
{

    /// <summary>
    /// 国際化対応クラス
    /// </summary>
    internal static class I18n
    {
        ///<summary> カタログ。最初の要素がデフォルト(フォールバック)ロケール </summary>
#pragma warning disable IDE1006 // Naming Styles
        internal static readonly List<I18nCatalogEntry> Catalog = new();
        ///<summary> ローカライズされた文字列を格納する辞書 </summary>
        internal static readonly Dictionary<string, Dictionary<string, string>> Words = new();
        internal static ReactiveProperty<string> CurrentLocale = new(); // デフォルトロケール
        private static CompositeDisposable _disposables;

        ///<summary> I18Nアセット変更イベント </summary>
        private static Subject<Unit> _assetsChanged;
        ///<summary> I18Nコンテンツ更新イベント用 </summary>
        private static Subject<Unit> _contentsUpdated;
        ///<summary> I18Nコンテンツ更新イベント </summary>
        internal static IObservable<Unit> ContentsUpdated => _contentsUpdated;
        internal static bool IsWatching => _disposables != null && !_disposables.IsDisposed;
#pragma warning restore IDE1006 // Naming Styles

        internal static void SetLocaleByDisplayName(string displayName)
        {
            // カタログから指定された表示名のロケールを検索
            var entry = Catalog.FirstOrDefault(e => e.DisplayName == displayName);
            if (entry.IsValid)
            {
                CurrentLocale.Value = entry.Locale;
            }
            else
            {
                MyLog.LogWarning($"Locale with display name '{displayName}' not found or invalid.");
            }
        }

        internal static void Load()
        {
            // リソースからI18nCatalogアセットをロード
            AssetDatabase.Refresh(); // アセットデータベースを更新(自動の処理が非同期なため)
            I18nCatalog catalogAsset = Resources.Load<I18nCatalog>("DollyDoll/i18n/i18nCatalog");
            if (catalogAsset == null)
            {
                MyLog.LogError("I18nCatalog asset not found in Resources/DollyDoll/i18n/i18nCatalog");
                return;
            }
            Catalog.Clear();
            Catalog.AddRange(catalogAsset.Entries ?? new List<I18nCatalogEntry>());
            Words.Clear();
            foreach (var entry in Catalog)
            {
                if (!entry.IsValid)
                {
                    MyLog.LogError($"Invalid I18nCatalogEntry: {entry.Locale}, {entry.DisplayName}, {entry.Resource}");
                    continue;
                }
                if (AssetDatabase.GetAssetPath(entry.Resource) == null)
                {
                    MyLog.LogError($"Resource not found for I18nCatalogEntry: {entry.Locale}, {entry.DisplayName}");
                    continue;
                }

                if (!Words.ContainsKey(entry.Locale))
                {
                    Words[entry.Locale] = new Dictionary<string, string>();
                }
                // ローカライズされた文字列をロード
                var localizedText = entry.Resource.text;

                if (localizedText != null)
                {
                    var lines = localizedText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Trim().StartsWith("#"))
                            continue; // コメント行をスキップ

                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            // バリューの改行エスケープ処理
                            // \\n → \n（リテラル文字列、改行しない）
                            // \n → 改行文字
                            value = value.Replace("\\\\n", "\uE000")  // 一時的にプライベート使用領域に置換
                                        .Replace("\\n", "\n")         // \n を改行文字に変換
                                        .Replace("\uE000", "\\n");    // \\n を \n に復元

                            Words[entry.Locale][key] = value;
                        }
                    }
                }
            }

            // CURRENT_LOCALEがWORDSに存在しない場合、最初のロケールをデフォルトに設定
            if (string.IsNullOrEmpty(CurrentLocale.Value) || !Words.ContainsKey(CurrentLocale.Value) && Catalog.Count > 0)
            {
                CurrentLocale.Value = Catalog[0].Locale;
            }
        }

        internal static string G(string key, params object[] args) => GetString(key, args);
        internal static string GetString(string key, params object[] args)
        {
            // 現在のロケールでのローカライズされた文字列を取得
            if (Words.TryGetValue(CurrentLocale.Value, out var localizedWords))
            {
                if (localizedWords.TryGetValue(key, out var localizedValue))
                {
                    return string.Format(localizedValue, args);
                }
            }
            // カタログの最初の要素のロケールでフォールバック
            if (Catalog.Count > 0 && Words.TryGetValue(Catalog[0].Locale, out var fallbackWords))
            {
                if (fallbackWords.TryGetValue(key, out var fallbackValue))
                {
                    return string.Format(fallbackValue, args);
                }
            }
            return key;
        }

        /// <summary>
        /// "ue/{tag}"で始まるキーについて、ルートVisualElement配下の対応する要素にローカライズされた文字列を適用
        /// </summary>
        /// <param name="root"></param>
        internal static void Apply(string tag, VisualElement root)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));
            }
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root), "Root VisualElement cannot be null.");
            }

            // 現在のロケールの辞書を取得
            if (!Words.TryGetValue(CurrentLocale.Value, out var currentWords))
            {
                // フォールバックロケールを使用
                if (Catalog.Count > 0 && Words.TryGetValue(Catalog[0].Locale, out var fallbackWords))
                {
                    currentWords = fallbackWords;
                }
                else
                {
                    return; // 辞書が見つからない場合は何もしない
                }
            }

            // "ue/"で始まるキーのみを処理
            var uiKeys = currentWords.Where(kvp => kvp.Key.StartsWith($"ue/{tag}")).ToList();

            foreach (var kvp in uiKeys)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // "ue/"を除去
                var keyWithoutPrefix = key.Substring(4 + tag.Length);
                var parts = keyWithoutPrefix.Split('/');

                if (parts.Length != 2)
                    continue;

                var selector = parts[0];
                var property = parts[1];

                // サポートされているプロパティのみ処理
                if (property != "text" && property != "label" && property != "tooltip" && property != "choices")
                    continue;

                // セレクタを解析して要素を検索
                var targetElements = QueryElements(root, selector);

                foreach (var targetElement in targetElements)
                {
                    // プロパティに応じて値を設定
                    switch (property)
                    {
                        case "text":
                            SetTextProperty(targetElement, value);
                            break;
                        case "label":
                            SetLabelProperty(targetElement, value);
                            break;
                        case "tooltip":
                            targetElement.tooltip = value;
                            break;
                        case "choices":
                            SetChoicesProperty(targetElement, value);
                            break;
                        default:
                            MyLog.LogError($"Property '{property}' for key '{key}' is not supported.");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// CSSセレクタ風の文字列を解析して要素を検索
        /// </summary>
        private static List<VisualElement> QueryElements(VisualElement root, string selector)
        {
            var results = new List<VisualElement>();

            // 正規表現でセレクタを解析
            // パターン: オプション#名前 + 0個以上の.クラス名
            // 例: "#button", ".class1.class2", "#button.class1.class2", "button"
            var pattern = @"^(?:#([a-zA-Z_][a-zA-Z0-9_-]*))?((?:\.[a-zA-Z_][a-zA-Z0-9_-]*)*)$";
            var match = Regex.Match(selector, pattern);

            string elementName = null;
            var classNames = new List<string>();

            if (match.Success)
            {
                // 名前をキャプチャ（グループ1）
                if (match.Groups[1].Success)
                {
                    elementName = match.Groups[1].Value;
                }

                // クラス名をキャプチャ（グループ2）
                if (match.Groups[2].Success)
                {
                    var classesString = match.Groups[2].Value;
                    // ".class1.class2" → ["class1", "class2"]
                    classNames.AddRange(classesString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else
            {
                // 正規表現に一致しない場合は単純な名前として扱う
                elementName = selector;
            }

            // UIElements の Query を使用して効率的に検索
            if (!string.IsNullOrEmpty(elementName) || classNames.Count > 0)
            {
                var query = root.Query<VisualElement>(elementName, classNames.ToArray());
                results.AddRange(query.ToList());
            }

            return results;
        }

        /// <summary>
        /// 要素のtextプロパティを設定
        /// </summary>
        private static void SetTextProperty(VisualElement element, string value)
        {
            switch (element)
            {
                case Label label:
                    label.text = value;
                    break;
                case Button button:
                    button.text = value;
                    break;
                case Toggle toggle:
                    toggle.text = value;
                    break;
                case Foldout foldout:
                    foldout.text = value;
                    break;
                case TextElement textElement:
                    textElement.text = value;
                    break;
                default:
                    MyLog.LogError($"{element.GetType().Name} does not have a text property or switch/case is not implemented.");
                    break;
            }
        }

        /// <summary>
        /// 要素のlabelプロパティを設定
        /// </summary>
        private static void SetLabelProperty(VisualElement element, string value)
        {
            // BaseFieldの派生クラスを個別にチェック
            switch (element)
            {
                case FloatField floatField:
                    floatField.label = value;
                    break;
                case IntegerField integerField:
                    integerField.label = value;
                    break;
                case Slider slider:
                    slider.label = value;
                    break;
                case SliderInt sliderInt:
                    sliderInt.label = value;
                    break;
                case TextField textField:
                    textField.label = value;
                    break;
                case EnumField enumField:
                    enumField.label = value;
                    break;
                case DropdownField dropdownField:
                    dropdownField.label = value;
                    break;
                case Toggle toggle:
                    toggle.label = value;
                    break;
                case Vector2Field vector2Field:
                    vector2Field.label = value;
                    break;
                case Vector3Field vector3Field:
                    vector3Field.label = value;
                    break;
                case BoundsField boundsField:
                    boundsField.label = value;
                    break;
                case RectField rectField:
                    rectField.label = value;
                    break;
                case Vector4Field vector4Field:
                    vector4Field.label = value;
                    break;
                default:
                    MyLog.LogError($"{element.GetType().Name} does not have a label property or switch/case is not implemented.");
                    break;
            }
        }

        /// <summary>
        /// 要素のchoicesプロパティを設定（DropdownField専用）
        /// </summary>
        private static void SetChoicesProperty(VisualElement element, string value)
        {
            switch (element)
            {
                case DropdownField dropdownField:
                    int index = dropdownField.index;
                    // カンマ区切りの文字列を分割してリストに変換
                    var choices = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(item => item.Trim())
                                    .ToList();
                    dropdownField.choices = choices;
                    dropdownField.index = index;
                    break;
                default:
                    MyLog.LogError($"{element.GetType().Name} does not have a choices property. It is only supported for DropdownField.");
                    break;
            }
        }

        /// <summary>
        /// 監視開始
        /// カタログエントリー変更、カタログ内容変更、ロケール変更を検知し、必要に応じて再読み込みを行う。
        /// カタログ内容変更は<see cref="I18nAssetContentWatcher"/>によって監視される。
        /// </summary>
        internal static IDisposable Subscribe()
        {
            _disposables = new CompositeDisposable();
            _assetsChanged = new Subject<Unit>().AddTo(_disposables);
            _contentsUpdated = new Subject<Unit>().AddTo(_disposables);
            // カタログエントリー変更検知
            Observable.Interval(TimeSpan.FromSeconds(0.05))
                .Select(_ => Catalog.Select(c => "" + c.GetHashCode()).Aggregate("", (a, b) => a + b))
                .DistinctUntilChanged()
                .Subscribe(_ => _assetsChanged?.OnNext(Unit.Default))
                .AddTo(_disposables);
            // カタログ変更イベント
            _assetsChanged.Throttle(TimeSpan.FromSeconds(0.1))
                .Subscribe(_ =>
                {
                    MyLog.Log("I18n assets changed, reloading catalog.");
                    Load(); // カタログの内容が変更されたら再読み込み
                    _contentsUpdated?.OnNext(Unit.Default);
                })
                .AddTo(_disposables);
            // ロケール変更イベント
            CurrentLocale.Subscribe(locale =>
            {
                Load(); // 現在のロケールが変更されたら再読み込み
                _contentsUpdated?.OnNext(Unit.Default);
            })
            .AddTo(_disposables);

            return _disposables;
        }

        ///<summary> <see cref="I18nAssetContentWatcher"/>からの通知用 </summary>
        internal static void NotifyAssetsChanged() => _assetsChanged?.OnNext(Unit.Default);
    }

    /// <summary>
    /// I18nアセットの内容変更を監視するクラス
    /// </summary>
    internal class I18nAssetContentWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!I18n.IsWatching)
            {
                return;
            }

            var i18nPaths = I18n.Catalog
                .Where(entry => entry.IsValid)
                .Select(entry => AssetDatabase.GetAssetPath(entry.Resource))
                .ToHashSet();

            // ファイル追加や内容変更
            foreach (var path in importedAssets)
            {
                if (i18nPaths.Contains(path))
                {
                    MyLog.Log($"I18n file is added or changed: {path}");
                    I18n.NotifyAssetsChanged();
                }
            }
            // ファイル削除
            foreach (var path in deletedAssets)
            {
                if (i18nPaths.Contains(path))
                {
                    MyLog.Log($"I18n file is deleted: {path}");
                    I18n.NotifyAssetsChanged();
                }
            }
        }
    }
}