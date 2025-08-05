using System;
using System.IO;
using System.Linq;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Somekasu.DollyDoll
{
    [CustomEditor(typeof(DollyDoll))]
    public class DollyDollEditor : Editor
    {
        private CompositeDisposable _disposables;
        private DollyDoll _dollyDoll => (DollyDoll)target;
        private bool _uiCreated = false;
        private bool _enabled = false;

        #region UI Elements
        private VisualElement _root;
        // NodesCountValidationHelpBox
        private VisualElement _nodesCountValidationHelpBox;
        private Label _nodesCountValidationHelpBoxLabel;
        // FileSection
        private Foldout _fileSection;
        private Button _loadButton;
        private Button _reloadButton;
        private Button _saveAsButton;
        private Button _saveButton;
        private Label _jsonNameLabel;
        // OscSection
        private Foldout _oscSection;
        private Toggle _importOnSaveToggle;
        private Toggle _forceImportToggle;
        private Toggle _loadOnExportToggle;
        private Button _oscButton;
        // PlaybackSection
        private Foldout _playbackSection;
        private Label _pathIndexLabel;
        private Button _prevButton;
        private Label _pathIndexText;
        private Button _nextButton;
        private Slider _progressSlider;
        private Button _playButton;
        // DollySettingSection
        private Foldout _dollySettingSection;
        private DropdownField _motionControlField;
        private DropdownField _easingField;
        private DropdownField _pathTypeField;
        private DropdownField _loopingField;
        // NodeOperationSection
        private Foldout _nodeOperationSection;
        private Button _addNodeButton;
        // CircleGenSection
        private Foldout _circleGenSection;
        private FloatField _radiusField;
        private FloatField _radiusDeltaField;
        private FloatField _yDeltaField;
        private FloatField _thetaDeltaField;
        private IntegerField _countField;
        private ObjectField _lookAtField;
        private VisualElement _circleGenHelpBox;
        private Label _circleGenHelpBoxLabel;
        private Button _circleGenButton;
        // SettingSection
        private Foldout _settingSection;
        private DropdownField _localeField;
        private IntegerField _oscVrcPortField;
        private IntegerField _oscUnityPortField;
        private TextField _vrcHostField;
        #endregion

        private void OnEnable()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            Observable.EveryUpdate()
                .Where(_ => _dollyDoll.enabled && _uiCreated)
                .Take(1)
                .Subscribe(_ =>
                {
                    // UI要素の参照を取得
                    InitializeUIElements();
                    // UI要素の初期表示値セット
                    SetValuesToUIElements();

                    // イベント登録
                    RegisterFileSectionEvents();
                    RegisterOscSectionEvents();
                    RegisterNodeOperationSectionEvents();
                    RegisterPlaybackSectionEvents();
                    RegisterCircleGenSectionEvents();
                    RegisterSettingSectionEvents();
                    RegisterDollySettingSectionEvents();

                    // 定期的にバリデーション
                    Observable.Interval(TimeSpan.FromSeconds(0.1))
                        .Subscribe(_ => UpdateUI())
                        .AddTo(_disposables);

                    Undo.undoRedoPerformed += SetValuesToUIElements;
                    _enabled = true;
                });
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed += SetValuesToUIElements;
            _disposables?.Dispose();
            _disposables = null;
            _enabled = false;
        }

        public override VisualElement CreateInspectorGUI()
        {
            // UXMLファイルを読み込み
            var visualTree = Resources.Load<VisualTreeAsset>("DollyDoll/DollyDollUIDocument");
            _root = visualTree.CloneTree();
            if (_root.styleSheets.count == 0)
            {
                // スタイルシートの読み込み
                var styleSheet = Resources.Load<StyleSheet>("DollyDoll/DollyDollStyleSheet");
                if (styleSheet != null)
                {
                    _root.styleSheets.Add(styleSheet);
                }
            }
            _uiCreated = true;

            return _root;
        }


        private void SetValuesToUIElements()
        {
            if (!_uiCreated || _dollyDoll == null)
            {
                return;
            }
            // OscSection
            _vrcHostField.SetValueWithoutNotify(_dollyDoll.Osc.VrcHost);
            _importOnSaveToggle.SetValueWithoutNotify(_dollyDoll.Osc.ImportOnSave.Value);
            _forceImportToggle.SetValueWithoutNotify(_dollyDoll.Osc.ForceImport.Value);
            _loadOnExportToggle.SetValueWithoutNotify(_dollyDoll.Osc.LoadOnExport.Value);
            // DollySettingSection
            SetupDropdownFieldValue(_motionControlField, _dollyDoll.PlayBackSetting.MotionControl);
            SetupDropdownFieldValue(_easingField, _dollyDoll.PlayBackSetting.Easing);
            SetupDropdownFieldValue(_pathTypeField, _dollyDoll.PlayBackSetting.PathType);
            SetupDropdownFieldValue(_loopingField, _dollyDoll.PlayBackSetting.Looping);
            // CircleGenSection
            _radiusField.SetValueWithoutNotify(_dollyDoll.CircleGen.Radius);
            _radiusDeltaField.SetValueWithoutNotify(_dollyDoll.CircleGen.RadiusDelta);
            _yDeltaField.SetValueWithoutNotify(_dollyDoll.CircleGen.YDelta);
            _thetaDeltaField.SetValueWithoutNotify(_dollyDoll.CircleGen.ThetaDelta);
            _countField.SetValueWithoutNotify(_dollyDoll.CircleGen.Count);
            _lookAtField.SetValueWithoutNotify(_dollyDoll.CircleGen.LookAt);
            // SettingSection
            _oscVrcPortField.SetValueWithoutNotify(_dollyDoll.Osc.VrcPort);
            _oscUnityPortField.SetValueWithoutNotify(_dollyDoll.Osc.UnityPort);
        }

        private void InitializeUIElements()
        {
            // NodesCountValidationHelpBox
            _nodesCountValidationHelpBox = _root.Q<VisualElement>("NodesCountValidationHelpBox");
            _nodesCountValidationHelpBoxLabel = _root.Q<Label>("NodesCountValidationHelpBoxLabel");
            // FileSection
            _fileSection = _root.Q<Foldout>("FileSection");
            _loadButton = _root.Q<Button>("LoadButton");
            _reloadButton = _root.Q<Button>("ReloadButton");
            _saveAsButton = _root.Q<Button>("SaveAsButton");
            _saveButton = _root.Q<Button>("SaveButton");
            _jsonNameLabel = _root.Q<Label>("JSONName");
            // OscSection
            _oscSection = _root.Q<Foldout>("OscSection");
            _importOnSaveToggle = _root.Q<Toggle>("ImportOnSave");
            _forceImportToggle = _root.Q<Toggle>("ForceImport");
            _loadOnExportToggle = _root.Q<Toggle>("LoadOnExport");
            _oscButton = _root.Q<Button>("OscButton");
            // PlaybackSection
            _playbackSection = _root.Q<Foldout>("PlaybackSection");
            _pathIndexLabel = _root.Q<Label>("PathIndexLabel");
            _prevButton = _root.Q<Button>("PrevButton");
            _pathIndexText = _root.Q<Label>("PathIndexText");
            _nextButton = _root.Q<Button>("NextButton");
            _progressSlider = _root.Q<Slider>("Progress");
            _playButton = _root.Q<Button>("PlayButton");
            // DollySettingSection
            _dollySettingSection = _root.Q<Foldout>("DollySettingSection");
            _motionControlField = _root.Q<DropdownField>("MotionControl");
            _easingField = _root.Q<DropdownField>("Easing");
            _pathTypeField = _root.Q<DropdownField>("PathType");
            _loopingField = _root.Q<DropdownField>("Looping");
            // NodeOperationSection
            _nodeOperationSection = _root.Q<Foldout>("NodeOperationSection");
            _addNodeButton = _root.Q<Button>("AddNodeButton");
            // CircleGenSection
            _circleGenSection = _root.Q<Foldout>("CircleGenSection");
            _radiusField = _root.Q<FloatField>("Radius");
            _radiusDeltaField = _root.Q<FloatField>("RadiusDelta");
            _yDeltaField = _root.Q<FloatField>("YDelta");
            _thetaDeltaField = _root.Q<FloatField>("ThetaDelta");
            _countField = _root.Q<IntegerField>("Count");
            _lookAtField = _root.Q<ObjectField>("LookAt");
            _circleGenHelpBox = _root.Q<VisualElement>("CircleGenHelpBox");
            _circleGenHelpBoxLabel = _root.Q<Label>("CircleGenHelpBoxLabel");
            _circleGenButton = _root.Q<Button>("CircleGenButton");
            // SettingSection
            _settingSection = _root.Q<Foldout>("SettingSection");
            _localeField = _root.Q<DropdownField>("Locale");
            _vrcHostField = _root.Q<TextField>("VrcHost");
            _oscVrcPortField = _root.Q<IntegerField>("OscVrcPort");
            _oscUnityPortField = _root.Q<IntegerField>("OscUnityPort");

            I18n.Apply("DollyDoll", _root);
            SetupLanguageChoices();
        }

        private void RegisterFileSectionEvents()
        {
            _loadButton.clicked += _dollyDoll.Load;
            _reloadButton.clicked += _dollyDoll.Reload;
            _saveAsButton.clicked += _dollyDoll.SaveAsNew;
            _saveButton.clicked += _dollyDoll.Save;

            _jsonNameLabel.text = I18n.G("validation/fileNotSpecified");
            _dollyDoll.ObserveEveryValueChanged(x => x.JSONPath)
                .Subscribe(path => _jsonNameLabel.text = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : I18n.G("validation/fileNotSpecified"))
                .AddTo(_disposables);
            I18n.ContentsUpdated
                .Subscribe(_ =>
                {
                    if (_root != null)
                    {
                        I18n.Apply("DollyDoll", _root);
                        _jsonNameLabel.text = !string.IsNullOrEmpty(_dollyDoll.JSONPath) ? Path.GetFileName(_dollyDoll.JSONPath) : I18n.G("validation/fileNotSpecified");
                        SetupLanguageChoices();
                    }
                })
                .AddTo(_disposables);
        }

        private void RegisterOscSectionEvents()
        {

            _importOnSaveToggle.CopyToReactiveOnValueChanged(_dollyDoll.Osc.ImportOnSave, _dollyDoll, "ImportOnSave").AddTo(_disposables);
            _forceImportToggle.CopyToReactiveOnValueChanged(_dollyDoll.Osc.ForceImport, _dollyDoll, "ForceImport").AddTo(_disposables);
            _loadOnExportToggle.CopyToReactiveOnValueChanged(_dollyDoll.Osc.LoadOnExport, _dollyDoll, "LoadOnExport").AddTo(_disposables);

            _oscButton.clicked += () => _dollyDoll.Osc.IsActive.Value = !_dollyDoll.Osc.IsActive.Value;
            _dollyDoll.Osc.IsActive
                .Subscribe(isActive => _oscButton.text = isActive ? I18n.G("osc/disconnect") : I18n.G("osc/connect"))
                .AddTo(_disposables);
        }

        private void RegisterPlaybackSectionEvents()
        {
            _prevButton.clicked += OnPrevPathButton;
            _nextButton.clicked += OnNextButton;
            _playButton.clicked += OnPlaybackToggle;
            SyncBaseFieldWithReactive(_progressSlider, _dollyDoll.Service.Playback.CurrentProgress, _disposables);

            _dollyDoll.Service.Playback.IsPlaying.Subscribe(isPlaying =>
            {
                if (_playButton != null)
                {
                    _playButton.text = isPlaying ? I18n.G("playback/stop") : I18n.G("playback/play");
                }
            }).AddTo(_disposables);

            // パス変更の監視
            _dollyDoll.NodesChanged.Subscribe(_ =>
            {
                if (_pathIndexText != null)
                {
                    var pathIndices = PathIndices();
                    if (pathIndices.Count > 0 && !_pathIndexText.text.Equals(pathIndices[0].ToString()))
                    {
                        _pathIndexText.text = pathIndices[0].ToString();
                    }
                }
            }).AddTo(_disposables);
        }

        private void RegisterDollySettingSectionEvents()
        {
            _motionControlField.CopyToEnumOnDropdownChanged<PBMotionControl>(value => _dollyDoll.PlayBackSetting.MotionControl = value, _dollyDoll, "MotionControl");
            _easingField.CopyToEnumOnDropdownChanged<PBEasing>(value => _dollyDoll.PlayBackSetting.Easing = value, _dollyDoll, "Easing");
            _pathTypeField.CopyToEnumOnDropdownChanged<PBPathType>(value => _dollyDoll.PlayBackSetting.PathType = value, _dollyDoll, "PathType");
            _loopingField.CopyToEnumOnDropdownChanged<PBLooping>(value => _dollyDoll.PlayBackSetting.Looping = value, _dollyDoll, "Looping");
        }

        private void RegisterNodeOperationSectionEvents()
        {
            _addNodeButton.clicked += _dollyDoll.AddNode;
        }

        private void RegisterCircleGenSectionEvents()
        {
            _radiusField.BindProperty(serializedObject.FindProperty("CircleGen.Radius"));
            _radiusDeltaField.BindProperty(serializedObject.FindProperty("CircleGen.RadiusDelta"));
            _yDeltaField.BindProperty(serializedObject.FindProperty("CircleGen.YDelta"));
            _thetaDeltaField.BindProperty(serializedObject.FindProperty("CircleGen.ThetaDelta"));
            _countField.BindProperty(serializedObject.FindProperty("CircleGen.Count"));
            _lookAtField.BindProperty(serializedObject.FindProperty("CircleGen.LookAt"));
            _circleGenButton.clicked += _dollyDoll.GenerateCircleNodes;

            _radiusField.RegisterValueChangedCallback(evt => ValidateCircleGen());
            _radiusDeltaField.RegisterValueChangedCallback(evt => ValidateCircleGen());
            _yDeltaField.RegisterValueChangedCallback(evt => ValidateCircleGen());
            _thetaDeltaField.RegisterValueChangedCallback(evt => ValidateCircleGen());
            _countField.RegisterValueChangedCallback(evt => ValidateCircleGen());
            _lookAtField.RegisterValueChangedCallback(evt => ValidateCircleGen());
        }

        private void RegisterSettingSectionEvents()
        {
            _localeField.RegisterValueChangedCallback(evt =>
            {
                if (_dollyDoll.LocaleName != evt.newValue)
                {
                    _dollyDoll.LocaleName = _localeField.index >= 0 && _localeField.index < _localeField.choices.Count
                        ? evt.newValue
                        : I18n.Catalog[0].DisplayName;
                    EditorUtility.SetDirty(_dollyDoll);
                }
            });
            _vrcHostField.BindProperty(serializedObject.FindProperty("Osc.VrcHost"));
            _oscVrcPortField.BindProperty(serializedObject.FindProperty("Osc.VrcPort"));
            _oscUnityPortField.BindProperty(serializedObject.FindProperty("Osc.UnityPort"));
        }

        private void SetupLanguageChoices()
        {
            _localeField.choices = I18n.Catalog.Select(c => c.DisplayName).ToList();
            // 初期値をDollyDoll.Languageに合わせる
            if (!string.IsNullOrEmpty(_dollyDoll.LocaleName) && _localeField.choices.Contains(_dollyDoll.LocaleName))
                _localeField.value = _dollyDoll.LocaleName;
            else
                _localeField.value = _localeField.choices[0];
        }

        /// <summary>
        /// ドロップダウンフィールドの初期値設定
        /// </summary>
        private void SetupDropdownFieldValue<T>(DropdownField dropdownField, T currentValue) where T : System.Enum
        {
            if (dropdownField.choices != null && dropdownField.choices.Count > 0)
            {
                int enumIndex = System.Convert.ToInt32(currentValue);
                if (enumIndex >= 0 && enumIndex < dropdownField.choices.Count)
                {
                    dropdownField.SetValueWithoutNotify(dropdownField.choices[enumIndex]);
                }
            }
        }

        /// <summary>
        /// ドロップダウンフィールドのイベント登録
        /// </summary>
        // private void RegisterDropdownFieldEvent<T>(DropdownField dropdownField, System.Action<T> onValueChanged) where T : System.Enum
        // {
        //     dropdownField.RegisterValueChangedCallback(evt =>
        //     {
        //         int selectedIndex = dropdownField.index;
        //         if (selectedIndex >= 0)
        //         {
        //             T enumValue = (T)System.Enum.ToObject(typeof(T), selectedIndex);
        //             Undo.SetCurrentGroupName($"Edit {dropdownField.name}");
        //             Undo.RecordObject(_dollyDoll, $"Edit {dropdownField.name} record");
        //             onValueChanged(enumValue);
        //             EditorUtility.SetDirty(_dollyDoll);
        //         }
        //     });
        // }

        private void UpdateUI()
        {
            if (_root == null || !_enabled)
            {
                return;
            }
            ValidateNodesCount();
            ValidateCircleGen();
            UpdatePlaybackUI();
        }

        private void UpdatePlaybackUI()
        {
            if (_pathIndexText != null && _playButton != null)
            {
                // パス番号の表示を更新
                var pathIndices = PathIndices();
                if (pathIndices.Count > 0)
                {
                    var selectedPathIndex = _dollyDoll.Service.Playback.SelectedPathIndex.Value;
                    if (pathIndices.Contains(selectedPathIndex))
                    {
                        _pathIndexText.text = selectedPathIndex.ToString();
                    }
                    else
                    {
                        _pathIndexText.text = pathIndices[0].ToString();
                        _dollyDoll.Service.Playback.SelectedPathIndex.Value = pathIndices[0];
                    }
                }
                else
                {
                    _pathIndexText.text = "0";
                }

                // 再生ボタンのテキストを更新
                _playButton.text = _dollyDoll.Service.Playback.IsPlaying.Value ? I18n.G("playback/stop") : I18n.G("playback/play");
            }
        }

        ///<summary> カメラノード数の警告チェック </summary>
        private void ValidateNodesCount()
        {
            string errorMsg = "";
            var allNodes = _dollyDoll.Nodes.ToList();

            int totalNodesCount = allNodes.Count;
            if (totalNodesCount > 100)
            {
                errorMsg += I18n.G("validation/totalNodesOver", totalNodesCount, 100) + "\n";
            }
            else if (totalNodesCount > 50)
            {
                errorMsg += I18n.G("validation/totalNodesOverPico", totalNodesCount, 50) + "\n";
            }

            var pathNodeCounts = allNodes
                .GroupBy(n => n.PathIndex)
                .ToDictionary(n => n.Key, n => n.Count());

            foreach (var kvp in pathNodeCounts)
            {
                if (kvp.Value > 50)
                {
                    errorMsg += I18n.G("validation/pathNodesOver", kvp.Key, kvp.Value, 50) + "\n";
                }
                else if (kvp.Value > 25)
                {
                    errorMsg += I18n.G("validation/pathNodesOverPico", kvp.Key, kvp.Value, 25) + "\n";
                }
            }

            _nodesCountValidationHelpBoxLabel.text = errorMsg.Trim();
            _nodesCountValidationHelpBox.style.display = !string.IsNullOrEmpty(errorMsg) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ValidateCircleGen()
        {
            // バリデーションチェック
            bool isValid = true;
            string errorMsg = "";
            if (_dollyDoll.CircleGen.Radius <= 0)
            {
                errorMsg += I18n.G("validation/radiusMustBePositive") + "\n";
                isValid = false;
            }
            if (_dollyDoll.CircleGen.Count < 1)
            {
                errorMsg += I18n.G("validation/countMustBePositive") + "\n";
                isValid = false;
            }
            if (Mathf.Abs(_dollyDoll.CircleGen.ThetaDelta) < 1e-3f)
            {
                errorMsg += I18n.G("validation/thetaDeltaMustBeNonZero") + "\n";
                isValid = false;
            }

            _circleGenButton.SetEnabled(isValid);
            _circleGenHelpBox.style.display = !isValid ? DisplayStyle.Flex : DisplayStyle.None;
            _circleGenHelpBoxLabel.text = errorMsg.Trim();
        }

        #region Playback Methods (from DollyDollOverlay)

        private System.Collections.Generic.List<int> PathIndices() => _dollyDoll.Nodes.Select(n => n.PathIndex).Distinct().OrderBy(i => i).ToList();

        private void OnNextButton()
        {
            var pathIndices = PathIndices();
            var currentValue = int.Parse(_pathIndexText.text);
            var index = pathIndices.IndexOf(currentValue);
            if (index > -1 && index < pathIndices.Count - 1)
            {
                _pathIndexText.text = pathIndices[index + 1].ToString();
                _dollyDoll.Service.Playback.SelectedPathIndex.Value = pathIndices[index + 1];
            }
        }

        private void OnPrevPathButton()
        {
            var pathIndices = PathIndices();
            var currentValue = int.Parse(_pathIndexText.text);
            var index = pathIndices.IndexOf(currentValue);
            if (index > 0)
            {
                _pathIndexText.text = pathIndices[index - 1].ToString();
                _dollyDoll.Service.Playback.SelectedPathIndex.Value = pathIndices[index - 1];
            }
        }

        private void OnPlaybackToggle()
        {
            if (!_dollyDoll.Service.Playback.IsPlaying.Value)
            {
                _dollyDoll.Service.Playback.PlaySplineT();
            }
            else
            {
                _dollyDoll.Service.Playback.StopSplineT();
            }
        }

        /// <summary>
        /// BaseField<TValueType> と双方向同期する ReactiveProperty<TValueType> を作成します。
        /// </summary>
        /// <typeparam name="TValueType">BaseField の値の型</typeparam>
        /// <param name="baseField">同期したい BaseField インスタンス</param>
        /// <param name="rp">ReactiveProperty</param>
        /// <param name="disposables">購読を管理する CompositeDisposable</param>
        /// <param name="initialValue">ReactiveProperty の初期値 (省略時は BaseField の現在の値)</param>
        public static void SyncBaseFieldWithReactive<TValueType>(
            BaseField<TValueType> baseField,
            ReactiveProperty<TValueType> rp,
            CompositeDisposable disposables,
            TValueType initialValue = default(TValueType))
        {
            // --- BaseField の値の変化を ReactiveProperty に同期 (UI -> Model) ---
            // BaseField の value が変更されたときに発火するコールバックを Observable に変換
            Observable.FromEvent<EventCallback<ChangeEvent<TValueType>>, ChangeEvent<TValueType>>(
                    handler => evt => handler(evt),
                    handler => baseField.RegisterValueChangedCallback(handler),
                    handler => baseField.UnregisterValueChangedCallback(handler)
                )
                .Select(evt => evt.newValue) // 新しい値だけを選択
                .Subscribe(newValue =>
                {
                    // ReactiveProperty の値を更新
                    // 無限ループ防止のため、値が異なる場合のみ更新
                    if (!System.Collections.Generic.EqualityComparer<TValueType>.Default.Equals(rp.Value, newValue))
                    {
                        rp.Value = newValue;
                    }
                })
                .AddTo(disposables);

            // --- ReactiveProperty の値の変化を BaseField に同期 (Model -> UI) ---
            rp.Subscribe(rpValue =>
                {
                    // BaseField の値を更新
                    // 無限ループ防止のため、値が異なる場合のみ更新
                    if (!System.Collections.Generic.EqualityComparer<TValueType>.Default.Equals(baseField.value, rpValue))
                    {
                        baseField.value = rpValue;
                    }
                })
                .AddTo(disposables);
        }

        #endregion
    }
}