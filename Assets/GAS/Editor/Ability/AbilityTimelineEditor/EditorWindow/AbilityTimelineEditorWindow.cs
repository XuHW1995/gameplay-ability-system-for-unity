using System;
using System.Reflection;
using GAS.Runtime.Ability;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GAS.Editor.Ability.AbilityTimelineEditor
{
    public class AbilityTimelineEditorWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;

        private VisualElement _root;


        public static AbilityTimelineEditorWindow Instance { get; private set; }
        public TimelineTrackView TrackView { get; private set; }

        public TimelineInspector TimelineInspector { get; private set; }

        public void CreateGUI()
        {
            Instance = this;
            _root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUxml = m_VisualTreeAsset.Instantiate();
            _root.Add(labelFromUxml);

            InitAbilityAssetBar();
            InitTopBar();

            InitController();
            TimerShaftView = new TimerShaftView(_root);
            TrackView = new TimelineTrackView(_root);
            TimelineInspector = new TimelineInspector(_root);
        }

        public static void ShowWindow(TimelineAbilityAsset asset)
        {
            var wnd = GetWindow<AbilityTimelineEditorWindow>();
            wnd.titleContent = new GUIContent("AbilityTimelineEditorWindow");
            wnd.InitAbility(asset);
        }

        public void Save()
        {
            AbilityAsset.Save();
        }

        private void InitAbility(TimelineAbilityAsset asset)
        {
            _abilityAsset.value = asset;
            MaxFrame.value = AbilityAsset.MaxFrameCount;
            CurrentSelectFrameIndex = 0;
            TimerShaftView.RefreshTimerDraw();
            TrackView.RefreshTrackDraw();
        }

        private void SaveAsset()
        {
            EditorUtility.SetDirty(AbilityAsset);
            AssetDatabase.SaveAssetIfDirty(AbilityAsset);
        }

        #region Config

        public AbilityTimelineEditorConfig Config { get; } = new();

        private ObjectField _abilityAsset;
        private Button _btnShowAbilityAssetDetail;
        public TimelineAbilityAsset AbilityAsset => _abilityAsset.value as TimelineAbilityAsset;

        private TimelineAbilityEditorWindow AbilityAssetEditor => AbilityAsset != null
            ? UnityEditor.Editor.CreateEditor(AbilityAsset) as TimelineAbilityEditorWindow
            : null;

        private void InitAbilityAssetBar()
        {
            _abilityAsset = _root.Q<ObjectField>("SequentialAbilityAsset");
            _abilityAsset.RegisterValueChangedCallback(OnSequentialAbilityAssetChanged);

            _btnShowAbilityAssetDetail = _root.Q<Button>("BtnShowAbilityAssetDetail");
            _btnShowAbilityAssetDetail.clickable.clicked += ShowAbilityAssetDetail;
        }

        private void OnSequentialAbilityAssetChanged(ChangeEvent<Object> evt)
        {
            var asset = evt.newValue as TimelineAbilityAsset;
            if (AbilityAsset != null)
            {
                MaxFrame.value = AbilityAsset.MaxFrameCount;
            }

            CurrentSelectFrameIndex = 0;
            TimerShaftView.RefreshTimerDraw();
            TrackView.RefreshTrackDraw();
        }

        private void ShowAbilityAssetDetail()
        {
            if (AbilityAsset == null) return;
            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var inspectorInstance = CreateInstance(inspectorType) as UnityEditor.EditorWindow;
            var prevSelection = Selection.activeObject;
            Selection.activeObject = AbilityAsset;
            var isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            if (isLocked != null) isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
            Selection.activeObject = prevSelection;
            if (inspectorInstance != null) inspectorInstance.Show();
        }

        #endregion

        #region TopBar

        private string _previousScenePath;
        private Button BtnLoadPreviewScene;
        private Button BtnBackToScene;
        private ObjectField _previewObjectField;

        private void InitTopBar()
        {
            BtnLoadPreviewScene = _root.Q<Button>(nameof(BtnLoadPreviewScene));
            BtnLoadPreviewScene.clickable.clicked += LoadPreviewScene;
            BtnBackToScene = _root.Q<Button>(nameof(BtnBackToScene));
            BtnBackToScene.clickable.clicked += BackToScene;
            _previewObjectField = _root.Q<ObjectField>("PreviewInstance");
            _previewObjectField.RegisterValueChangedCallback(OnPreviewObjectChanged);
        }

        private void OnPreviewObjectChanged(ChangeEvent<Object> evt)
        {
            // TODO : 在这里处理预览对象的变化
        }

        private void BackToScene()
        {
            // 判断是否有记录前一个Scene
            if (!string.IsNullOrEmpty(_previousScenePath))
                // 激活前一个Scene
                EditorSceneManager.OpenScene(_previousScenePath);
            else
                Debug.LogWarning("No previous scene available.");
        }

        private void LoadPreviewScene()
        {
            // 记录当前Scene
            _previousScenePath = SceneManager.GetActiveScene().path;
            // 创建一个新的Scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 在这里添加临时预览的内容，例如放置一些对象
            // 这里只是演示，具体可以根据需求添加你的内容
            // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // SceneManager.MoveGameObjectToScene(cube, newScene);
            // 激活新创建的Scene
            SceneManager.SetActiveScene(newScene);
        }

        #endregion

        #region TimerShaft

        public TimerShaftView TimerShaftView { get; private set; }

        private int _currentMaxFrame;

        public int CurrentMaxFrame
        {
            get => _currentMaxFrame;
            private set
            {
                if (_currentMaxFrame == value) return;
                _currentMaxFrame = value;
                AbilityAsset.MaxFrameCount = _currentMaxFrame;
                SaveAsset();
                MaxFrame.value = _currentMaxFrame;
                TrackView.UpdateContentSize();
                TimerShaftView.RefreshTimerDraw();
            }
        }

        private int _currentSelectFrameIndex;

        public int CurrentSelectFrameIndex
        {
            get => _currentSelectFrameIndex;
            set
            {
                if (_currentSelectFrameIndex == value) return;
                _currentSelectFrameIndex = Mathf.Clamp(value, 0, MaxFrame.value);
                CurrentFrame.value = _currentSelectFrameIndex;
                TimerShaftView.RefreshTimerDraw();

                EvaluateFrame(_currentSelectFrameIndex);
            }
        }

        public float CurrentFramePos => Mathf.Abs(TimerShaftView.TimeLineContainer.transform.position.x);
        public float CurrentSelectFramePos => _currentSelectFrameIndex * Config.FrameUnitWidth;
        public float CurrentEndFramePos => CurrentMaxFrame * Config.FrameUnitWidth;

        public int GetFrameIndexByPosition(float x)
        {
            return TimerShaftView.GetFrameIndexByPosition(x);
        }
    
        public int GetFrameIndexByMouse(float x)
        {
            return TimerShaftView.GetFrameIndexByMouse(x);
        }

        #endregion

        #region Controller

        private Button BtnPlay;
        private Button BtnLeftFrame;
        private Button BtnRightFrame;
        private IntegerField CurrentFrame;
        private IntegerField MaxFrame;

        private void InitController()
        {
            BtnPlay = _root.Q<Button>(nameof(BtnPlay));
            BtnPlay.clickable.clicked += OnPlay;

            BtnLeftFrame = _root.Q<Button>(nameof(BtnLeftFrame));
            BtnLeftFrame.clickable.clicked += OnLeftFrame;

            BtnRightFrame = _root.Q<Button>(nameof(BtnRightFrame));
            BtnRightFrame.clickable.clicked += OnRightFrame;

            CurrentFrame = _root.Q<IntegerField>(nameof(CurrentFrame));
            CurrentFrame.RegisterValueChangedCallback(OnCurrentFrameChanged);
            MaxFrame = _root.Q<IntegerField>(nameof(MaxFrame));
            MaxFrame.RegisterValueChangedCallback(OnMaxFrameChanged);
        }

        private void OnMaxFrameChanged(ChangeEvent<int> evt)
        {
            if(AbilityAsset==null) return;
            CurrentMaxFrame = evt.newValue;
        }

        private void OnCurrentFrameChanged(ChangeEvent<int> evt)
        {
            if(AbilityAsset==null) return;
            CurrentSelectFrameIndex = evt.newValue;
        }

        private void RefreshPlayButton()
        {
            BtnPlay.text = !IsPlaying ? "▶" : "⏹";
            BtnPlay.style.backgroundColor =
                !IsPlaying ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(0.1f, 0.8f, 0.1f, 0.5f);
        }

        private void OnPlay()
        {
            if(AbilityAsset==null) return;
            IsPlaying = !IsPlaying;
        }

        private void OnLeftFrame()
        {
            if(AbilityAsset==null) return;
            IsPlaying = false;
            CurrentSelectFrameIndex -= 1;
        }

        private void OnRightFrame()
        {
            if(AbilityAsset==null) return;
            IsPlaying = false;
            CurrentSelectFrameIndex += 1;
        }

        #endregion

        #region Clip Inspector

        public object CurrentInspectorObject => TimelineInspector.CurrentInspectorObject;

        public void SetInspector(object target = null)
        {
            if(AbilityAsset==null) return;
            TimelineInspector.SetInspector(target);
        }

        #endregion

        #region TimelinePreview

        private DateTime _startTime;
        private int _startPlayFrameIndex;
        private bool _isPlaying;

        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                _isPlaying = CanPlay() && value;

                if (_isPlaying)
                {
                    _startTime = DateTime.Now;
                    _startPlayFrameIndex = CurrentSelectFrameIndex;
                }

                RefreshPlayButton();
            }
        }

        private void Update()
        {
            if (IsPlaying)
            {
                var deltaTime = (DateTime.Now - _startTime).TotalSeconds;
                var frameIndex = (int)(deltaTime * Config.DefaultFrameRate) + _startPlayFrameIndex;
                if (frameIndex >= CurrentMaxFrame)
                {
                    frameIndex = CurrentMaxFrame;
                    IsPlaying = false;
                }

                CurrentSelectFrameIndex = frameIndex;
            }
        }

        private void EvaluateFrame(int frameIndex)
        {
            if (AbilityAsset != null && _previewObjectField.value != null)
                // TODO : 在这里处理预览对象的动画,特效等等
                TrackView.animationTrack.TickView(frameIndex, _previewObjectField.value as GameObject);
        }

        private bool CanPlay()
        {
            var canPlay = AbilityAsset != null && _previewObjectField.value != null;
            return canPlay;
        }

        #endregion
    }
}