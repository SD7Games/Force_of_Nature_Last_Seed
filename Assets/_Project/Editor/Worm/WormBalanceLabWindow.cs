using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class WormBalanceLabWindow : EditorWindow
{
    private const float MinResultViewHeight = 220f;
    private const float MaxResultViewHeight = 520f;
    private const float EstimatedControlsHeight = 560f;
    private const int DefaultSectionCount = 9;
    private const float DefaultWormSpeed = 1f;
    private const float DefaultSegmentSpacing = 0.5f;
    private const float DefaultRollbackSpeed = 8f;
    private const float PreviousSectionRollbackForwardSpeedMultiplier = 2f;
    private const float DefaultSectionRollbackForwardSpeedMultiplier = 4f;
    private const float DefaultReviveRollbackProgress = 0.12f;
    private const float LegacyTakeAllMinTotalDpsGainRatio = 0.7f;
    private const float PreviousTakeAllMinTotalDpsGainRatio = 1.15f;
    private const float DefaultTakeAllMinTotalDpsGainRatio = 0.9f;
    private const float PreviousTakeAllMinHeadPathProgress = 0.7f;
    private const string RewardDatabasePath = "Assets/_Project/App/Gameplay/Rewards/RewardDatabase_Main.asset";
    private const string HpConfigPath = "Assets/_Project/App/Gameplay/Enemy/Worm/Balance/WormHpScalingConfig_Default.asset";
    private const string PressureConfigPath = "Assets/_Project/App/Gameplay/Enemy/Worm/Balance/WormPressureConfig_Default.asset";
    private const string MainWeaponConfigPath = "Assets/_Project/App/Gameplay/Combat/Weapons/ProjectileWeapon/Configs/MainWeaponConfig_Default.asset";
    private const string AcaciaThornConfigPath = "Assets/_Project/App/Gameplay/Combat/Weapons/AcaciaThornWeapon/Configs/AcaciaThornWeaponConfig_Default.asset";

    [SerializeField] private RewardDatabase _rewardDatabase;
    [SerializeField] private WormHpScalingConfig _hpConfig;
    [SerializeField] private WormPressureConfig _pressureConfig;
    [SerializeField] private WeaponConfig _mainWeaponConfig;
    [SerializeField] private AcaciaThornWeaponConfig _acaciaThornConfig;
    [SerializeField] private RailPath _railPath;

    [SerializeField] private int _simulationCount = 1000;
    [SerializeField] private int _seed = 12345;
    [SerializeField] private int _levelNumber = 1;
    [SerializeField] private int _sectionCount = DefaultSectionCount;
    [SerializeField] private float _pathTimeLimitSeconds = 75f;
    [SerializeField] private bool _derivePathTimeFromRail = true;
    [SerializeField] private float _wormSpeed = DefaultWormSpeed;
    [SerializeField] private float _segmentSpacing = DefaultSegmentSpacing;
    [SerializeField] private float _rollbackSpeed = DefaultRollbackSpeed;
    [SerializeField] private float _sectionRollbackForwardSpeedMultiplier = DefaultSectionRollbackForwardSpeedMultiplier;
    [SerializeField] private float _hitEfficiency = 0.9f;
    [SerializeField] private int _progressBucketCount = 5;
    [SerializeField] private bool _simulatePlayerXFollow = true;
    [SerializeField] private bool _useRuntimePressure = true;
    [SerializeField] private bool _applySectionRollback = true;
    [SerializeField] private bool _logEachRunToConsole;
    [SerializeField] private WormBalanceRewardPickStrategy _rewardPickStrategy = WormBalanceRewardPickStrategy.HighestEstimatedDpsGain;
    [SerializeField] private WormBalanceAdSimulationMode _adSimulationMode = WormBalanceAdSimulationMode.BalanceMatrix;
    [SerializeField] private int _freeRerollAttemptsPerSession = 2;
    [SerializeField] private int _adRerollAttemptsPerSession = 1;
    [SerializeField] private int _takeAllAttemptsPerSession = 1;
    [SerializeField] private int _reviveAttemptsPerSession = 1;
    [SerializeField] private float _reviveRollbackProgress = DefaultReviveRollbackProgress;
    [SerializeField] private float _freeRerollMinDpsGainRatio = 0.12f;
    [SerializeField] private float _adRerollMinDpsGainRatio = 0.25f;
    [SerializeField] private float _takeAllMinTotalDpsGainRatio = DefaultTakeAllMinTotalDpsGainRatio;
    [SerializeField] private float _takeAllMinHeadPathProgress = RewardAdRerollPolicy.TakeAllMinHeadPathProgress;

    private Vector2 _windowScroll;
    private Vector2 _resultScroll;
    private string _lastSummary = "Run a simulation to see balance data.";

    [MenuItem("Tools/Game/Worm Balance Lab")]
    public static void Open()
    {
        GetWindow<WormBalanceLabWindow>("Worm Balance Lab");
    }

    private void OnEnable()
    {
        MigrateSimulationDefaults();
        LoadDefaultAssets();
        LoadOpenSceneValues();
    }

    private void MigrateSimulationDefaults()
    {
        if (Mathf.Approximately(_takeAllMinTotalDpsGainRatio, LegacyTakeAllMinTotalDpsGainRatio))
            _takeAllMinTotalDpsGainRatio = DefaultTakeAllMinTotalDpsGainRatio;

        if (Mathf.Approximately(_takeAllMinTotalDpsGainRatio, PreviousTakeAllMinTotalDpsGainRatio))
            _takeAllMinTotalDpsGainRatio = DefaultTakeAllMinTotalDpsGainRatio;

        if (Mathf.Approximately(_sectionRollbackForwardSpeedMultiplier, PreviousSectionRollbackForwardSpeedMultiplier))
            _sectionRollbackForwardSpeedMultiplier = DefaultSectionRollbackForwardSpeedMultiplier;

        if (_takeAllMinHeadPathProgress <= 0f
            || Mathf.Approximately(_takeAllMinHeadPathProgress, PreviousTakeAllMinHeadPathProgress))
        {
            _takeAllMinHeadPathProgress = RewardAdRerollPolicy.TakeAllMinHeadPathProgress;
        }
    }

    private void OnGUI()
    {
        _windowScroll = EditorGUILayout.BeginScrollView(
            _windowScroll,
            false,
            true);

        try
        {
            EditorGUILayout.LabelField("Worm Balance Lab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs deterministic editor-only simulations using the real reward database, reward effects and HP resolver. Locked weapon unlocks are picked first. HighestEstimatedDpsGain then previews every offered reward on cloned runtime states and picks the largest estimated DPS increase.",
                MessageType.Info);

            DrawAssetFields();
            EditorGUILayout.Space(8f);
            DrawSimulationFields();
            EditorGUILayout.Space(8f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawSummary();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAssetFields()
    {
        EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

        _rewardDatabase = (RewardDatabase)EditorGUILayout.ObjectField(
            "Reward Database",
            _rewardDatabase,
            typeof(RewardDatabase),
            false);
        _hpConfig = (WormHpScalingConfig)EditorGUILayout.ObjectField(
            "HP Config",
            _hpConfig,
            typeof(WormHpScalingConfig),
            false);
        _pressureConfig = (WormPressureConfig)EditorGUILayout.ObjectField(
            "Pressure Config",
            _pressureConfig,
            typeof(WormPressureConfig),
            false);
        _mainWeaponConfig = (WeaponConfig)EditorGUILayout.ObjectField(
            "Main Weapon",
            _mainWeaponConfig,
            typeof(WeaponConfig),
            false);
        _acaciaThornConfig = (AcaciaThornWeaponConfig)EditorGUILayout.ObjectField(
            "Acacia Thorn",
            _acaciaThornConfig,
            typeof(AcaciaThornWeaponConfig),
            false);
        _railPath = (RailPath)EditorGUILayout.ObjectField(
            "Rail Path",
            _railPath,
            typeof(RailPath),
            true);
    }

    private void DrawSimulationFields()
    {
        EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);

        _simulationCount = Mathf.Max(1, EditorGUILayout.IntField("Auto Test Runs", _simulationCount));
        _seed = EditorGUILayout.IntField("Seed", _seed);
        _levelNumber = Mathf.Max(1, EditorGUILayout.IntField("Level Number", _levelNumber));
        _sectionCount = Mathf.Max(1, EditorGUILayout.IntField("Worm Sections", _sectionCount));
        _wormSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("Worm Speed", _wormSpeed));
        _derivePathTimeFromRail = EditorGUILayout.Toggle("Use Rail Length", _derivePathTimeFromRail);
        using (new EditorGUI.DisabledScope(_derivePathTimeFromRail && _railPath != null))
        {
            _pathTimeLimitSeconds = Mathf.Max(1f, EditorGUILayout.FloatField("Path Time Limit", _pathTimeLimitSeconds));
        }

        if (_derivePathTimeFromRail && _railPath != null)
        {
            WormBalancePathMetrics metrics = WormBalancePathMetrics.FromRailPath(
                _railPath,
                _pathTimeLimitSeconds,
                _derivePathTimeFromRail,
                _wormSpeed,
                _progressBucketCount);
            EditorGUILayout.LabelField(
                "Derived Path Time",
                $"{metrics.PathTimeLimitSeconds:0.0}s ({metrics.PathLength:0.00} units / {_wormSpeed:0.00} speed)");
        }

        _segmentSpacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Segment Spacing", _segmentSpacing));
        _rollbackSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("Rollback Speed", _rollbackSpeed));
        _sectionRollbackForwardSpeedMultiplier = Mathf.Max(
            0f,
            EditorGUILayout.FloatField("Rollback Forward Multiplier", _sectionRollbackForwardSpeedMultiplier));
        _hitEfficiency = Mathf.Clamp(
            EditorGUILayout.Slider("Hit Efficiency", _hitEfficiency, 0.1f, 1.5f),
            0.1f,
            1.5f);
        _progressBucketCount = Mathf.Clamp(
            EditorGUILayout.IntField("Progress Buckets", _progressBucketCount),
            2,
            20);
        _simulatePlayerXFollow = EditorGUILayout.Toggle("Player Follows Head X", _simulatePlayerXFollow);
        _rewardPickStrategy = (WormBalanceRewardPickStrategy)EditorGUILayout.EnumPopup(
            "Reward Pick",
            _rewardPickStrategy);
        _adSimulationMode = (WormBalanceAdSimulationMode)EditorGUILayout.EnumPopup(
            "Ad Simulation",
            _adSimulationMode);
        _freeRerollAttemptsPerSession = Mathf.Max(
            0,
            EditorGUILayout.IntField("Free Rerolls / Session", _freeRerollAttemptsPerSession));
        _adRerollAttemptsPerSession = Mathf.Max(
            0,
            EditorGUILayout.IntField("Ad Rerolls / Session", _adRerollAttemptsPerSession));
        _takeAllAttemptsPerSession = Mathf.Max(
            0,
            EditorGUILayout.IntField("Take All Ads / Session", _takeAllAttemptsPerSession));
        _reviveAttemptsPerSession = Mathf.Max(
            0,
            EditorGUILayout.IntField("Revive Ads / Session", _reviveAttemptsPerSession));
        _reviveRollbackProgress = Mathf.Clamp01(
            EditorGUILayout.Slider("Revive Rollback Progress", _reviveRollbackProgress, 0f, 0.95f));
        _freeRerollMinDpsGainRatio = Mathf.Clamp(
            EditorGUILayout.Slider("Free Reroll Min DPS Gain", _freeRerollMinDpsGainRatio, 0f, 1f),
            0f,
            1f);
        _adRerollMinDpsGainRatio = Mathf.Clamp(
            EditorGUILayout.Slider("Ad Reroll Min DPS Gain", _adRerollMinDpsGainRatio, 0f, 2f),
            0f,
            2f);
        _takeAllMinTotalDpsGainRatio = Mathf.Clamp(
            EditorGUILayout.Slider("Take All Min Total DPS Gain", _takeAllMinTotalDpsGainRatio, 0f, 3f),
            0f,
            3f);
        _takeAllMinHeadPathProgress = Mathf.Clamp01(
            EditorGUILayout.Slider("Take All Min Path Progress", _takeAllMinHeadPathProgress, 0f, 1f));
        _useRuntimePressure = EditorGUILayout.Toggle("Runtime Pressure", _useRuntimePressure);
        _applySectionRollback = EditorGUILayout.Toggle("Section Rollback", _applySectionRollback);
        _logEachRunToConsole = EditorGUILayout.Toggle("Log Each Run", _logEachRunToConsole);
    }

    private void DrawActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Preview"))
                RunSimulation(1);

            if (GUILayout.Button($"Run {_simulationCount} Auto Tests"))
                RunSimulation(_simulationCount);

            if (GUILayout.Button("Load Open Scene Values"))
                LoadOpenSceneValues(force: true);

            if (GUILayout.Button("Reload Defaults"))
                LoadDefaultAssets(force: true);
        }
    }

    private void DrawSummary()
    {
        EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);

        GUIStyle resultStyle = new(EditorStyles.textArea)
        {
            wordWrap = false
        };
        float resultViewHeight = Mathf.Clamp(
            position.height - EstimatedControlsHeight,
            MinResultViewHeight,
            MaxResultViewHeight);
        float textHeight = Mathf.Max(
            MinResultViewHeight,
            resultStyle.CalcHeight(
                new GUIContent(_lastSummary),
                Mathf.Max(240f, position.width - 64f)));

        _resultScroll = EditorGUILayout.BeginScrollView(
            _resultScroll,
            true,
            true,
            GUILayout.Height(resultViewHeight),
            GUILayout.MinHeight(MinResultViewHeight));
        EditorGUILayout.TextArea(
            _lastSummary,
            resultStyle,
            GUILayout.MinHeight(textHeight),
            GUILayout.ExpandWidth(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunSimulation(int runCount)
    {
        WormBalanceSimulationSettings settings = BuildSettings(runCount);

        if (!settings.IsValid(out string error))
        {
            EditorUtility.DisplayDialog("Worm Balance Lab", error, "OK");
            return;
        }

        WormBalanceSimulationReport report = WormBalanceSimulator.Run(settings);
        _lastSummary = report.BuildSummary();

        Debug.Log(_lastSummary);

        if (_logEachRunToConsole)
        {
            IReadOnlyList<WormBalanceRunResult> runs = report.Runs;

            for (int i = 0; i < runs.Count; i++)
                Debug.Log(runs[i].BuildDebugLine());
        }

    }

    private WormBalanceSimulationSettings BuildSettings(int runCount)
    {
        return new WormBalanceSimulationSettings(
            _rewardDatabase,
            _hpConfig,
            _pressureConfig,
            _mainWeaponConfig,
            _acaciaThornConfig,
            Mathf.Max(1, runCount),
            _seed,
            _levelNumber,
            _sectionCount,
            _pathTimeLimitSeconds,
            _derivePathTimeFromRail,
            _wormSpeed,
            _segmentSpacing,
            _rollbackSpeed,
            _sectionRollbackForwardSpeedMultiplier,
            _hitEfficiency,
            _progressBucketCount,
            _simulatePlayerXFollow,
            _useRuntimePressure,
            _applySectionRollback,
            _rewardPickStrategy,
            _adSimulationMode,
            _freeRerollAttemptsPerSession,
            _adRerollAttemptsPerSession,
            _takeAllAttemptsPerSession,
            _reviveAttemptsPerSession,
            _reviveRollbackProgress,
            _freeRerollMinDpsGainRatio,
            _adRerollMinDpsGainRatio,
            _takeAllMinTotalDpsGainRatio,
            _takeAllMinHeadPathProgress,
            WormBalancePathMetrics.FromRailPath(
                _railPath,
                _pathTimeLimitSeconds,
                _derivePathTimeFromRail,
                _wormSpeed,
                _progressBucketCount));
    }

    private void LoadDefaultAssets(bool force = false)
    {
        if (force || _rewardDatabase == null)
            _rewardDatabase = AssetDatabase.LoadAssetAtPath<RewardDatabase>(RewardDatabasePath);

        if (force || _hpConfig == null)
            _hpConfig = AssetDatabase.LoadAssetAtPath<WormHpScalingConfig>(HpConfigPath);

        if (force || _pressureConfig == null)
            _pressureConfig = AssetDatabase.LoadAssetAtPath<WormPressureConfig>(PressureConfigPath);

        if (force || _mainWeaponConfig == null)
            _mainWeaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(MainWeaponConfigPath);

        if (force || _acaciaThornConfig == null)
            _acaciaThornConfig = AssetDatabase.LoadAssetAtPath<AcaciaThornWeaponConfig>(AcaciaThornConfigPath);
    }

    private void LoadOpenSceneValues(bool force = false)
    {
        LastSeed.Bootstrap.Installers.WormInstaller wormInstaller =
            FindOpenSceneObject<LastSeed.Bootstrap.Installers.WormInstaller>();

        if (wormInstaller != null)
        {
            if (force || _hpConfig == null)
                _hpConfig = wormInstaller.EditorHpScalingConfig;

            if (force || _levelNumber <= 1)
                _levelNumber = wormInstaller.EditorLevelNumber;

            if (force || _sectionCount == DefaultSectionCount)
                _sectionCount = wormInstaller.EditorSectionCount;
        }

        WormController controller = FindOpenSceneObject<WormController>();
        if (controller != null)
        {
            if (force || _railPath == null)
                _railPath = controller.EditorRail;

            if (force || Mathf.Approximately(_wormSpeed, DefaultWormSpeed))
                _wormSpeed = controller.EditorSpeed;

            if (force || Mathf.Approximately(_segmentSpacing, DefaultSegmentSpacing))
                _segmentSpacing = controller.EditorSegmentSpacing;

            if (force || Mathf.Approximately(_rollbackSpeed, DefaultRollbackSpeed))
                _rollbackSpeed = controller.EditorRollbackSpeed;

            if (force || Mathf.Approximately(_sectionRollbackForwardSpeedMultiplier, DefaultSectionRollbackForwardSpeedMultiplier))
                _sectionRollbackForwardSpeedMultiplier = controller.EditorSectionRollbackForwardSpeedMultiplier;

            if (force || Mathf.Approximately(_reviveRollbackProgress, DefaultReviveRollbackProgress))
                _reviveRollbackProgress = controller.EditorReviveRollbackProgressNormalized;
        }

        WormPressureDirector pressureDirector = FindOpenSceneObject<WormPressureDirector>();
        if (pressureDirector != null && (force || _pressureConfig == null))
            _pressureConfig = pressureDirector.EditorConfig;

        RewardInstaller rewardInstaller = FindOpenSceneObject<RewardInstaller>();
        if (rewardInstaller != null)
        {
            if (force || _freeRerollAttemptsPerSession <= 0)
                _freeRerollAttemptsPerSession = rewardInstaller.EditorFreeRerollAttemptsPerSession;

            if (force || _adRerollAttemptsPerSession <= 0)
                _adRerollAttemptsPerSession = rewardInstaller.EditorAdRerollAttemptsPerSession;

            if (force || _takeAllAttemptsPerSession <= 0)
                _takeAllAttemptsPerSession = rewardInstaller.EditorTakeAllAttemptsPerSession;
        }

        WormReviveFlowController reviveFlow = FindOpenSceneObject<WormReviveFlowController>();
        if (reviveFlow != null && (force || _reviveAttemptsPerSession <= 0))
            _reviveAttemptsPerSession = reviveFlow.EditorMaxReviveAttempts;

        LastSeed.Bootstrap.Installers.PlayerInstaller playerInstaller =
            FindOpenSceneObject<LastSeed.Bootstrap.Installers.PlayerInstaller>();

        if (playerInstaller != null && (force || _mainWeaponConfig == null))
            _mainWeaponConfig = playerInstaller.EditorStartWeaponConfig;

        AcaciaThornWeapon acaciaThornWeapon = FindOpenSceneObject<AcaciaThornWeapon>();
        if (acaciaThornWeapon != null && (force || _acaciaThornConfig == null))
            _acaciaThornConfig = acaciaThornWeapon.Config;
    }

    private static T FindOpenSceneObject<T>()
        where T : UnityEngine.Object
    {
        T[] objects = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return objects != null && objects.Length > 0
            ? objects[0]
            : null;
    }
}
