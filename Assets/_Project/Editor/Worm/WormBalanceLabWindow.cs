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
        WormSpawner spawner = FindOpenSceneObject<WormSpawner>();
        if (spawner != null)
        {
            if (force || _sectionCount == DefaultSectionCount)
                _sectionCount = spawner.EditorSectionCount;
        }

        LastSeed.Bootstrap.Installers.WormInstaller wormInstaller =
            FindOpenSceneObject<LastSeed.Bootstrap.Installers.WormInstaller>();

        if (wormInstaller != null)
        {
            if (force || _hpConfig == null)
                _hpConfig = wormInstaller.EditorHpScalingConfig;

            if (force || _levelNumber <= 1)
                _levelNumber = wormInstaller.EditorLevelNumber;
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

public enum WormBalanceRewardPickStrategy
{
    RandomChoice = 0,
    HighestRarityThenRandom = 1,
    HighestEstimatedDpsGain = 2
}

public enum WormBalanceAdSimulationMode
{
    NoAdsOnly = 0,
    AdsAssistOnly = 1,
    CompareNoAdsAndAdsAssist = 2,
    BalanceMatrix = 3
}

internal enum WormBalanceScenario
{
    NoAds = 0,
    ReviveOnly = 1,
    AdsAssistNoRevive = 2,
    AdsAssist = 3
}

internal sealed class WormBalanceSimulationSettings
{
    public readonly RewardDatabase RewardDatabase;
    public readonly WormHpScalingConfig HpConfig;
    public readonly WormPressureConfig PressureConfig;
    public readonly WeaponConfig MainWeaponConfig;
    public readonly AcaciaThornWeaponConfig AcaciaThornConfig;
    public readonly int RunCount;
    public readonly int Seed;
    public readonly int LevelNumber;
    public readonly int SectionCount;
    public readonly float PathTimeLimitSeconds;
    public readonly bool DerivePathTimeFromRail;
    public readonly float WormSpeed;
    public readonly float SegmentSpacing;
    public readonly float RollbackSpeed;
    public readonly float SectionRollbackForwardSpeedMultiplier;
    public readonly float HitEfficiency;
    public readonly int ProgressBucketCount;
    public readonly bool SimulatePlayerXFollow;
    public readonly bool UseRuntimePressure;
    public readonly bool ApplySectionRollback;
    public readonly WormBalanceRewardPickStrategy RewardPickStrategy;
    public readonly WormBalanceAdSimulationMode AdSimulationMode;
    public readonly int FreeRerollAttemptsPerSession;
    public readonly int AdRerollAttemptsPerSession;
    public readonly int TakeAllAttemptsPerSession;
    public readonly int ReviveAttemptsPerSession;
    public readonly float ReviveRollbackProgress;
    public readonly float FreeRerollMinDpsGainRatio;
    public readonly float AdRerollMinDpsGainRatio;
    public readonly float TakeAllMinTotalDpsGainRatio;
    public readonly float TakeAllMinHeadPathProgress;
    public readonly WormBalancePathMetrics PathMetrics;

    public WormBalanceSimulationSettings(
        RewardDatabase rewardDatabase,
        WormHpScalingConfig hpConfig,
        WormPressureConfig pressureConfig,
        WeaponConfig mainWeaponConfig,
        AcaciaThornWeaponConfig acaciaThornConfig,
        int runCount,
        int seed,
        int levelNumber,
        int sectionCount,
        float pathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        float segmentSpacing,
        float rollbackSpeed,
        float sectionRollbackForwardSpeedMultiplier,
        float hitEfficiency,
        int progressBucketCount,
        bool simulatePlayerXFollow,
        bool useRuntimePressure,
        bool applySectionRollback,
        WormBalanceRewardPickStrategy rewardPickStrategy,
        WormBalanceAdSimulationMode adSimulationMode,
        int freeRerollAttemptsPerSession,
        int adRerollAttemptsPerSession,
        int takeAllAttemptsPerSession,
        int reviveAttemptsPerSession,
        float reviveRollbackProgress,
        float freeRerollMinDpsGainRatio,
        float adRerollMinDpsGainRatio,
        float takeAllMinTotalDpsGainRatio,
        float takeAllMinHeadPathProgress,
        WormBalancePathMetrics pathMetrics)
    {
        RewardDatabase = rewardDatabase;
        HpConfig = hpConfig;
        PressureConfig = pressureConfig;
        MainWeaponConfig = mainWeaponConfig;
        AcaciaThornConfig = acaciaThornConfig;
        RunCount = Mathf.Max(1, runCount);
        Seed = seed;
        LevelNumber = Mathf.Max(1, levelNumber);
        SectionCount = Mathf.Max(1, sectionCount);
        DerivePathTimeFromRail = derivePathTimeFromRail;
        WormSpeed = Mathf.Max(0.01f, wormSpeed);
        PathMetrics = pathMetrics ?? WormBalancePathMetrics.CreateFallback(
            pathTimeLimitSeconds,
            WormSpeed,
            progressBucketCount);
        PathTimeLimitSeconds = Mathf.Max(1f, PathMetrics.PathTimeLimitSeconds);
        SegmentSpacing = Mathf.Max(0.01f, segmentSpacing);
        RollbackSpeed = Mathf.Max(0.01f, rollbackSpeed);
        SectionRollbackForwardSpeedMultiplier = Mathf.Max(0f, sectionRollbackForwardSpeedMultiplier);
        HitEfficiency = Mathf.Max(0.01f, hitEfficiency);
        ProgressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
        SimulatePlayerXFollow = simulatePlayerXFollow;
        UseRuntimePressure = useRuntimePressure;
        ApplySectionRollback = applySectionRollback;
        RewardPickStrategy = rewardPickStrategy;
        AdSimulationMode = adSimulationMode;
        FreeRerollAttemptsPerSession = Mathf.Max(0, freeRerollAttemptsPerSession);
        AdRerollAttemptsPerSession = Mathf.Max(0, adRerollAttemptsPerSession);
        TakeAllAttemptsPerSession = Mathf.Max(0, takeAllAttemptsPerSession);
        ReviveAttemptsPerSession = Mathf.Max(0, reviveAttemptsPerSession);
        ReviveRollbackProgress = Mathf.Clamp01(reviveRollbackProgress);
        FreeRerollMinDpsGainRatio = Mathf.Max(0f, freeRerollMinDpsGainRatio);
        AdRerollMinDpsGainRatio = Mathf.Max(0f, adRerollMinDpsGainRatio);
        TakeAllMinTotalDpsGainRatio = Mathf.Max(0f, takeAllMinTotalDpsGainRatio);
        TakeAllMinHeadPathProgress = Mathf.Clamp01(takeAllMinHeadPathProgress);
    }

    public bool IncludesScenario(WormBalanceScenario scenario)
    {
        return AdSimulationMode switch
        {
            WormBalanceAdSimulationMode.NoAdsOnly => scenario == WormBalanceScenario.NoAds,
            WormBalanceAdSimulationMode.AdsAssistOnly => scenario == WormBalanceScenario.AdsAssist,
            WormBalanceAdSimulationMode.CompareNoAdsAndAdsAssist =>
                scenario == WormBalanceScenario.NoAds || scenario == WormBalanceScenario.AdsAssist,
            WormBalanceAdSimulationMode.BalanceMatrix => true,
            _ => false
        };
    }

    public int ScenarioCount
    {
        get
        {
            return AdSimulationMode switch
            {
                WormBalanceAdSimulationMode.CompareNoAdsAndAdsAssist => 2,
                WormBalanceAdSimulationMode.BalanceMatrix => 4,
                _ => 1
            };
        }
    }

    public bool IsValid(out string error)
    {
        if (RewardDatabase == null)
        {
            error = "Reward database is missing.";
            return false;
        }

        if (HpConfig == null)
        {
            error = "Worm HP config is missing.";
            return false;
        }

        if (MainWeaponConfig == null || MainWeaponConfig.Projectile == null)
        {
            error = "Main weapon config or projectile config is missing.";
            return false;
        }

        error = null;
        return true;
    }
}

internal sealed class WormBalancePathMetrics
{
    private readonly float[] _controlPointProgresses;
    private readonly int _progressBucketCount;
    private readonly RailPath _railPath;

    private WormBalancePathMetrics(
        float pathLength,
        float pathTimeLimitSeconds,
        float[] controlPointProgresses,
        int progressBucketCount,
        RailPath railPath)
    {
        PathLength = Mathf.Max(0f, pathLength);
        PathTimeLimitSeconds = Mathf.Max(1f, pathTimeLimitSeconds);
        _controlPointProgresses = controlPointProgresses ?? Array.Empty<float>();
        _progressBucketCount = Mathf.Clamp(progressBucketCount, 2, 20);
        _railPath = railPath;
    }

    public float PathLength { get; }
    public float PathTimeLimitSeconds { get; }
    public int ControlPointCount => _controlPointProgresses.Length;

    public static WormBalancePathMetrics FromRailPath(
        RailPath railPath,
        float fallbackPathTimeLimitSeconds,
        bool derivePathTimeFromRail,
        float wormSpeed,
        int progressBucketCount)
    {
        if (railPath == null)
            return CreateFallback(fallbackPathTimeLimitSeconds, wormSpeed, progressBucketCount);

        int pointCount = Mathf.Max(0, railPath.PointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.LegacyWaypointCount);

        if (pointCount <= 0)
            pointCount = Mathf.Max(0, railPath.transform.childCount);

        float[] controlPointProgresses = pointCount > 0
            ? new float[pointCount]
            : Array.Empty<float>();

        float totalLength = Mathf.Max(0f, railPath.TotalLength);

        for (int i = 0; i < pointCount; i++)
        {
            if (!TryGetControlPointDistance(
                    railPath,
                    i,
                    out float distance))
            {
                controlPointProgresses[i] = pointCount > 1
                    ? i / (float)(pointCount - 1)
                    : 0f;
                continue;
            }

            totalLength = Mathf.Max(totalLength, railPath.TotalLength);
            controlPointProgresses[i] = totalLength > 0f
                ? Mathf.Clamp01(distance / totalLength)
                : (pointCount > 1 ? i / (float)(pointCount - 1) : 0f);
        }

        float pathTime = derivePathTimeFromRail && totalLength > 0f
            ? totalLength / Mathf.Max(0.01f, wormSpeed)
            : fallbackPathTimeLimitSeconds;

        return new WormBalancePathMetrics(
            totalLength,
            pathTime,
            controlPointProgresses,
            progressBucketCount,
            railPath);
    }

    private static bool TryGetControlPointDistance(
        RailPath railPath,
        int pointIndex,
        out float distance)
    {
        distance = 0f;

        if (railPath == null)
            return false;

        if (railPath.TryGetControlPointDistance(pointIndex, out distance))
            return true;

        if (pointIndex < 0 || pointIndex >= railPath.transform.childCount)
            return false;

        Transform child = railPath.transform.GetChild(pointIndex);

        if (child == null)
            return false;

        distance = railPath.GetClosestDistance(child.position);
        return true;
    }

    public static WormBalancePathMetrics CreateFallback(
        float pathTimeLimitSeconds,
        float wormSpeed,
        int progressBucketCount)
    {
        float safeTime = Mathf.Max(1f, pathTimeLimitSeconds);

        return new WormBalancePathMetrics(
            safeTime * Mathf.Max(0.01f, wormSpeed),
            safeTime,
            Array.Empty<float>(),
            progressBucketCount,
            null);
    }

    public float GetHeadX(float headProgress)
    {
        if (_railPath == null || PathLength <= 0f)
            return 0f;

        Vector3 headPosition = _railPath.GetPoint(Mathf.Clamp01(headProgress) * PathLength);
        return headPosition.x;
    }

    public WormBalancePathLocation GetLocation(float headProgress)
    {
        float progress = Mathf.Clamp01(headProgress);
        int bucketIndex = Mathf.Clamp(
            Mathf.FloorToInt(progress * _progressBucketCount),
            0,
            _progressBucketCount - 1);

        int controlPointIndex = GetReachedControlPointIndex(progress);

        return new WormBalancePathLocation(
            progress,
            bucketIndex,
            _progressBucketCount,
            controlPointIndex,
            GetControlPointProgress(controlPointIndex));
    }

    private int GetReachedControlPointIndex(float progress)
    {
        if (_controlPointProgresses.Length == 0)
            return -1;

        int index = 0;

        for (int i = 0; i < _controlPointProgresses.Length; i++)
        {
            if (progress + 0.0001f < _controlPointProgresses[i])
                break;

            index = i;
        }

        return index;
    }

    private float GetControlPointProgress(int index)
    {
        if (index < 0 || index >= _controlPointProgresses.Length)
            return -1f;

        return _controlPointProgresses[index];
    }
}

internal readonly struct WormBalancePathLocation
{
    public readonly float HeadProgress;
    public readonly int BucketIndex;
    public readonly int BucketCount;
    public readonly int ControlPointIndex;
    public readonly float ControlPointProgress;

    public WormBalancePathLocation(
        float headProgress,
        int bucketIndex,
        int bucketCount,
        int controlPointIndex,
        float controlPointProgress)
    {
        HeadProgress = Mathf.Clamp01(headProgress);
        BucketIndex = Mathf.Max(0, bucketIndex);
        BucketCount = Mathf.Max(1, bucketCount);
        ControlPointIndex = controlPointIndex;
        ControlPointProgress = controlPointProgress;
    }

    public string BucketLabel
    {
        get
        {
            float start = BucketIndex / (float)BucketCount * 100f;
            float end = (BucketIndex + 1) / (float)BucketCount * 100f;
            return $"{start:0}-{end:0}%";
        }
    }

    public string ControlPointLabel =>
        ControlPointIndex >= 0
            ? $"CP {ControlPointIndex} ({ControlPointProgress * 100f:0.0}%)"
            : "No rail";
}

internal static class WormBalanceSimulator
{
    private const int ThousandHp = 1000;
    private const int TenThousandHp = 10000;
    private const int MillionHp = 1000000;
    private const int TenMillionHp = 10000000;

    public static WormBalanceSimulationReport Run(WormBalanceSimulationSettings settings)
    {
        Random.State previousRandomState = Random.state;
        var runs = new List<WormBalanceRunResult>(settings.RunCount * settings.ScenarioCount);

        try
        {
            for (int i = 0; i < settings.RunCount; i++)
            {
                if (settings.IncludesScenario(WormBalanceScenario.NoAds))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.NoAds));
                }

                if (settings.IncludesScenario(WormBalanceScenario.ReviveOnly))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.ReviveOnly));
                }

                if (settings.IncludesScenario(WormBalanceScenario.AdsAssistNoRevive))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.AdsAssistNoRevive));
                }

                if (settings.IncludesScenario(WormBalanceScenario.AdsAssist))
                {
                    Random.InitState(settings.Seed + i * 7919);
                    runs.Add(SimulateRun(settings, i, WormBalanceScenario.AdsAssist));
                }
            }
        }
        finally
        {
            Random.state = previousRandomState;
        }

        return new WormBalanceSimulationReport(settings, runs);
    }

    private static WormBalanceRunResult SimulateRun(
        WormBalanceSimulationSettings settings,
        int runIndex,
        WormBalanceScenario scenario)
    {
        WeaponRuntimeState mainState = CreateMainWeaponState(settings.MainWeaponConfig);
        AcaciaThornRuntimeState acaciaState = CreateAcaciaThornState(settings.AcaciaThornConfig);
        RewardRuntimeContext rewardContext = new(
            mainState,
            acaciaState,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainState),
            settings.MainWeaponConfig,
            settings.AcaciaThornConfig);
        RewardRollService rewardRollService = new(settings.RewardDatabase);
        WormSectionHpResolver hpResolver = new(settings.HpConfig);
        WormBalanceSectionState[] sections = BuildSections(settings);
        WormBalanceAdSessionState adSession = WormBalanceAdSessionState.Create(settings, scenario);

        float time = 0f;
        float headProgress = 0f;
        float playerX = settings.PathMetrics.GetHeadX(headProgress);
        float maxPlayerXError = 0f;
        float pressureElapsedTime = 0f;
        float pressureSampleTimer = 0f;
        float runtimePressureMultiplier = 1f;
        bool pressureChanged = false;
        int destroyedSegments = 0;
        int rewardsTaken = 0;
        int lastSectionIndex = -1;
        float firstRewardTime = -1f;
        bool hasRevivedThisRun = false;
        StringBuilder rewardLog = new();

        RebuildSectionHp(
            settings,
            hpResolver,
            sections,
            0,
            mainState,
            acaciaState,
            runtimePressureMultiplier,
            headProgress,
            hasRevivedThisRun,
            allowHpDecrease: true);

        int totalProgressSegments = CountProgressSegments(sections);

        for (int i = 0; i < sections.Length; i++)
        {
            WormBalanceSectionState section = sections[i];
            WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);

            if (!power.IsValid || power.EstimatedDps <= 0f)
            {
                return WormBalanceRunResult.Loss(
                    scenario,
                    runIndex,
                    "No DPS",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    0f,
                    playerX,
                    settings.PathMetrics.GetHeadX(headProgress),
                    maxPlayerXError,
                    settings.PathMetrics.GetLocation(headProgress),
                    adSession.ToStats(),
                    rewardLog.ToString());
            }

            float dps = Mathf.Max(0.01f, power.EstimatedDps * settings.HitEfficiency);
            float killTime = section.Hp / dps;
            float timeBeforeSectionDamage = time;

            if (!AdvanceTime(
                    settings,
                    killTime,
                    ref time,
                    ref pressureElapsedTime,
                    ref headProgress,
                    ref pressureSampleTimer,
                    ref runtimePressureMultiplier,
                    ref pressureChanged,
                    ref playerX,
                    ref maxPlayerXError))
            {
                int remainingSectionHp = CalculateRemainingSectionHp(
                    section.Hp,
                    dps,
                    time - timeBeforeSectionDamage);
                float endpointSectionDamageProgress = CalculateSectionDamageProgress(
                    section.Hp,
                    remainingSectionHp);
                float endpointDestructionProgress = GetDestructionProgress(
                    destroyedSegments + (section.SegmentCount * endpointSectionDamageProgress),
                    totalProgressSegments);

                if (TryUseRevive(
                        settings,
                        adSession,
                        ref hasRevivedThisRun,
                        ref headProgress,
                        ref pressureElapsedTime,
                        ref pressureSampleTimer,
                        ref runtimePressureMultiplier,
                        ref pressureChanged,
                        ref playerX,
                        ref maxPlayerXError))
                {
                    section.Hp = remainingSectionHp;
                    RebuildSectionHp(
                        settings,
                        hpResolver,
                        sections,
                        i + 1,
                        mainState,
                        acaciaState,
                        runtimePressureMultiplier,
                        headProgress,
                        hasRevivedThisRun,
                        allowHpDecrease: true);
                    pressureChanged = false;
                    i--;
                    continue;
                }

                return WormBalanceRunResult.Loss(
                    scenario,
                    runIndex,
                    "Path completed",
                    time,
                    GetDestructionProgress(destroyedSegments, totalProgressSegments),
                    headProgress,
                    i,
                    lastSectionIndex,
                    rewardsTaken,
                    firstRewardTime,
                    dps,
                    playerX,
                    settings.PathMetrics.GetHeadX(headProgress),
                    maxPlayerXError,
                    settings.PathMetrics.GetLocation(headProgress),
                    adSession.ToStats(),
                    rewardLog.ToString(),
                    endpointDestructionProgress,
                    endpointSectionDamageProgress);
            }

            destroyedSegments += section.SegmentCount;
            lastSectionIndex = i;

            if (settings.ApplySectionRollback)
            {
                headProgress = ApplyRollback(settings, section.SegmentCount, headProgress);
                AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
            }

            if (pressureChanged)
            {
                RebuildSectionHp(
                    settings,
                    hpResolver,
                    sections,
                    i + 1,
                    mainState,
                    acaciaState,
                    runtimePressureMultiplier,
                    headProgress,
                    hasRevivedThisRun);
                pressureChanged = false;
            }

            if (!section.HasCocoon)
                continue;

            RewardRollContext rollContext = new(
                headProgress,
                GetDestructionProgress(destroyedSegments, totalProgressSegments),
                hasRevivedThisRun);
            WormBalanceRewardSelection rewardSelection = ResolveRewardPopup(
                rewardRollService,
                rewardContext,
                section.CocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState,
                adSession);

            if (rewardSelection.Rewards == null || rewardSelection.Rewards.Count == 0)
            {
                AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    null,
                    0f);
                continue;
            }

            for (int rewardIndex = 0; rewardIndex < rewardSelection.Rewards.Count; rewardIndex++)
            {
                RewardChoiceData selectedReward = rewardSelection.Rewards[rewardIndex];

                if (selectedReward == null || selectedReward.Effect == null)
                    continue;

                if (!selectedReward.Effect.CanApply(rewardContext))
                    continue;

                selectedReward.Effect.Apply(rewardContext);
                rewardsTaken++;

                if (firstRewardTime < 0f)
                    firstRewardTime = time;

                AppendRewardLog(
                    rewardLog,
                    time,
                    section.CocoonProfile,
                    selectedReward,
                    rewardSelection.GetDpsGain(selectedReward));
            }

            RebuildSectionHp(
                settings,
                hpResolver,
                sections,
                i + 1,
                mainState,
                acaciaState,
                runtimePressureMultiplier,
                headProgress,
                hasRevivedThisRun);
        }

        WeaponPowerSnapshot finalPower = EstimatePower(settings, mainState, acaciaState);

        return WormBalanceRunResult.Win(
            scenario,
            runIndex,
            time,
            GetDestructionProgress(destroyedSegments, totalProgressSegments),
            headProgress,
            sections.Length,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalPower.IsValid ? finalPower.EstimatedDps * settings.HitEfficiency : 0f,
            playerX,
            settings.PathMetrics.GetHeadX(headProgress),
            maxPlayerXError,
            settings.PathMetrics.GetLocation(headProgress),
            adSession.ToStats(),
            rewardLog.ToString());
    }

    private static WormBalanceSectionState[] BuildSections(
        WormBalanceSimulationSettings settings)
    {
        int bodySegmentCount = WormPatternBuilder.GetBodySegmentCount(settings.SectionCount);
        int totalSections = WormCocoonRules.CountGameplaySections(bodySegmentCount);
        var sections = new WormBalanceSectionState[totalSections];
        int remainingSegments = bodySegmentCount;
        int sectionsWithoutCocoon = 0;

        for (int i = 0; i < totalSections; i++)
        {
            int segmentCount = Mathf.Min(
                WormCocoonRules.SectionSize,
                remainingSegments);
            float progress = WormCocoonRules.GetSectionProgress(i, totalSections);
            CocoonRewardProfile cocoonProfile = null;

            if (WormCocoonRules.ShouldPlaceCocoon(
                    i,
                    totalSections,
                    progress,
                    sectionsWithoutCocoon))
            {
                cocoonProfile = WormCocoonRules.RollCocoonProfile(
                    settings.RewardDatabase.CocoonProfiles,
                    progress);
                sectionsWithoutCocoon = 0;
            }
            else
            {
                sectionsWithoutCocoon++;
            }

            sections[i] = new WormBalanceSectionState(
                i,
                Mathf.Max(1, segmentCount),
                cocoonProfile);
            remainingSegments -= segmentCount;
        }

        return sections;
    }

    private static void RebuildSectionHp(
        WormBalanceSimulationSettings settings,
        WormSectionHpResolver hpResolver,
        WormBalanceSectionState[] sections,
        int startIndex,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        float runtimePressureMultiplier,
        float headProgress,
        bool hasRevivedThisRun,
        bool allowHpDecrease = false)
    {
        if (sections == null || sections.Length == 0)
            return;

        WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);
        int previousHp = 0;

        for (int i = 0; i < sections.Length; i++)
        {
            int baseHp = WormSectionHPGenerator.GetHP(i, settings.LevelNumber);
            int resolvedHp = hpResolver.ResolveHp(
                baseHp,
                i,
                sections.Length,
                settings.LevelNumber,
                power,
                runtimePressureMultiplier,
                GetHeadPressureMultiplier(settings, headProgress),
                hasRevivedThisRun);
            int hp = EnsureHpAbovePrevious(resolvedHp, previousHp);

            if (i >= startIndex)
            {
                if (!allowHpDecrease)
                    hp = Mathf.Max(hp, sections[i].Hp);

                sections[i].Hp = hp;
            }

            previousHp = GetPreviousHpForSection(
                previousHp,
                hp,
                sections[i],
                i,
                startIndex,
                i >= startIndex,
                allowHpDecrease);
        }
    }

    private static int GetPreviousHpForSection(
        int previousHp,
        int resolvedHp,
        WormBalanceSectionState section,
        int sectionIndex,
        int startIndex,
        bool canRebalance,
        bool allowHpDecrease)
    {
        if (canRebalance)
            return resolvedHp;

        if (!allowHpDecrease || sectionIndex == startIndex - 1)
            return Mathf.Max(previousHp, resolvedHp, section != null ? section.Hp : 0);

        return Mathf.Max(previousHp, resolvedHp);
    }

    private static bool AdvanceTime(
        WormBalanceSimulationSettings settings,
        float duration,
        ref float time,
        ref float pressureElapsedTime,
        ref float headProgress,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        float remaining = Mathf.Max(0f, duration);

        while (remaining > 0f)
        {
            float step = remaining;

            if (settings.UseRuntimePressure && settings.PressureConfig != null)
            {
                float timeToPressureSample = Mathf.Max(
                    0.0001f,
                    settings.PressureConfig.SampleInterval - pressureSampleTimer);
                step = Mathf.Min(step, timeToPressureSample);
            }

            if (settings.PathTimeLimitSeconds > 0f)
            {
                float timeToPathEnd = (1f - headProgress) * settings.PathTimeLimitSeconds;

                if (step >= timeToPathEnd)
                {
                    time += Mathf.Max(0f, timeToPathEnd);
                    headProgress = 1f;
                    AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
                    return false;
                }
            }

            time += step;
            pressureElapsedTime += step;
            headProgress = Mathf.Clamp01(headProgress + step / settings.PathTimeLimitSeconds);
            AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
            remaining -= step;

            if (!settings.UseRuntimePressure || settings.PressureConfig == null)
                continue;

            pressureSampleTimer += step;

            if (pressureSampleTimer + Mathf.Epsilon < settings.PressureConfig.SampleInterval)
                continue;

            pressureSampleTimer = 0f;
            float nextPressure = CalculateRuntimePressure(
                settings.PressureConfig,
                pressureElapsedTime,
                headProgress,
                runtimePressureMultiplier);

            if (Mathf.Approximately(nextPressure, runtimePressureMultiplier))
                continue;

            runtimePressureMultiplier = nextPressure;
            pressureChanged = true;
        }

        return true;
    }

    private static void AlignPlayerXWithHead(
        WormBalanceSimulationSettings settings,
        ref float playerX,
        float headProgress,
        ref float maxPlayerXError)
    {
        if (!settings.SimulatePlayerXFollow)
            return;

        float headX = settings.PathMetrics.GetHeadX(headProgress);
        playerX = headX;
        maxPlayerXError = Mathf.Max(maxPlayerXError, Mathf.Abs(playerX - headX));
    }

    private static float CalculateRuntimePressure(
        WormPressureConfig config,
        float elapsedTime,
        float headProgress,
        float currentPressure)
    {
        float expectedProgress = config.GetExpectedProgress(elapsedTime);
        float deadZone = config.ProgressDeadZone;

        if (headProgress + deadZone < expectedProgress)
            return Mathf.Min(config.MaxMultiplier, currentPressure + config.IncreasePerSample);

        if (headProgress > expectedProgress + deadZone)
            return Mathf.Max(1f, currentPressure - config.RecoveryPerSample);

        return currentPressure;
    }

    private static bool TryUseRevive(
        WormBalanceSimulationSettings settings,
        WormBalanceAdSessionState adSession,
        ref bool hasRevivedThisRun,
        ref float headProgress,
        ref float pressureElapsedTime,
        ref float pressureSampleTimer,
        ref float runtimePressureMultiplier,
        ref bool pressureChanged,
        ref float playerX,
        ref float maxPlayerXError)
    {
        if (adSession == null || !adSession.TryUseRevive())
            return false;

        hasRevivedThisRun = true;
        headProgress = settings.ReviveRollbackProgress;
        pressureElapsedTime = settings.PressureConfig != null
            ? settings.PressureConfig.GetElapsedTimeForExpectedProgress(headProgress)
            : 0f;
        pressureSampleTimer = 0f;
        runtimePressureMultiplier = 1f;
        pressureChanged = true;
        AlignPlayerXWithHead(settings, ref playerX, headProgress, ref maxPlayerXError);
        return true;
    }

    private static WormBalanceRewardSelection ResolveRewardPopup(
        RewardRollService rewardRollService,
        RewardRuntimeContext rewardContext,
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        WormBalanceAdSessionState adSession)
    {
        WormBalanceRewardOffer offer = RollAndEvaluateOffer(
            rewardRollService,
            rewardContext,
            cocoonProfile,
            rollContext,
            settings,
            mainState,
            acaciaState);

        float currentDps = GetCurrentEstimatedDps(settings, mainState, acaciaState);

        while (adSession != null
            && ShouldRerollOffer(offer, currentDps, settings.FreeRerollMinDpsGainRatio)
            && adSession.TryUseFreeReroll())
        {
            offer = RollAndEvaluateOffer(
                rewardRollService,
                rewardContext,
                cocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState);
        }

        while (adSession != null
            && ShouldRerollOffer(offer, currentDps, settings.AdRerollMinDpsGainRatio)
            && adSession.TryUseAdReroll())
        {
            offer = RollAndEvaluateOffer(
                rewardRollService,
                rewardContext,
                cocoonProfile,
                rollContext,
                settings,
                mainState,
                acaciaState,
                RewardAdRerollPolicy.RollGuaranteedRarity(
                    rewardContext,
                    cocoonProfile,
                    rollContext),
                1,
                isPaidAssistRoll: true);
        }

        if (adSession != null
            && ShouldTakeAll(
                offer,
                rollContext,
                currentDps,
                settings.TakeAllMinTotalDpsGainRatio,
                settings.TakeAllMinHeadPathProgress)
            && adSession.TryUseTakeAll())
        {
            return offer.CreateTakeAllSelection();
        }

        return offer.CreateSingleSelection();
    }

    private static WormBalanceRewardOffer RollAndEvaluateOffer(
        RewardRollService rewardRollService,
        RewardRuntimeContext rewardContext,
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        RewardRarity? guaranteedRarity = null,
        int guaranteedRaritySlotCount = 1,
        bool isPaidAssistRoll = false)
    {
        RewardRollContext effectiveRollContext = isPaidAssistRoll
            ? rollContext.WithPaidAssistRoll()
            : rollContext;
        RewardRarity rarity = guaranteedRarity
            ?? rewardRollService.RollGuaranteeRarity(
                rewardContext,
                cocoonProfile,
                effectiveRollContext);
        List<RewardChoiceData> choices = rewardRollService.Roll3(
            rewardContext,
            cocoonProfile,
            rarity,
            guaranteedRaritySlotCount,
            effectiveRollContext);
        RewardChoiceData selectedReward = PickReward(
            choices,
            settings,
            mainState,
            acaciaState,
            out float selectedDpsGain);
        var evaluations = new List<WormBalanceRewardChoiceEvaluation>(
            choices != null ? choices.Count : 0);

        if (choices != null)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                RewardChoiceData choice = choices[i];
                float dpsGain = choice != null && choice.Effect != null
                    ? CalculateEstimatedDpsGain(choice, settings, mainState, acaciaState)
                    : float.NegativeInfinity;

                evaluations.Add(new WormBalanceRewardChoiceEvaluation(choice, dpsGain));
            }
        }

        return new WormBalanceRewardOffer(
            choices,
            evaluations,
            selectedReward,
            selectedDpsGain);
    }

    private static bool ShouldRerollOffer(
        WormBalanceRewardOffer offer,
        float currentDps,
        float minDpsGainRatio)
    {
        if (offer == null || offer.SelectedReward == null)
            return true;

        float minimumDpsGain = Mathf.Max(0.01f, currentDps * minDpsGainRatio);
        return offer.SelectedDpsGain < minimumDpsGain;
    }

    private static bool ShouldTakeAll(
        WormBalanceRewardOffer offer,
        RewardRollContext rollContext,
        float currentDps,
        float minTotalDpsGainRatio,
        float minHeadPathProgress)
    {
        if (offer == null || offer.BeneficialRewardCount < 2)
            return false;

        if (!RewardAdRerollPolicy.CanOfferTakeAll(rollContext, minHeadPathProgress))
            return false;

        float minimumTotalDpsGain = Mathf.Max(0.01f, currentDps * minTotalDpsGainRatio);
        return offer.TotalPositiveDpsGain >= minimumTotalDpsGain
            && offer.TotalPositiveDpsGain > offer.SelectedDpsGain + 0.0001f;
    }

    private static float GetCurrentEstimatedDps(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        WeaponPowerSnapshot power = EstimatePower(settings, mainState, acaciaState);
        return power.IsValid ? Mathf.Max(0.01f, power.EstimatedDps) : 0.01f;
    }

    private static RewardChoiceData PickReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = 0f;

        if (choices == null || choices.Count == 0)
            return null;

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.RandomChoice)
            return choices[Random.Range(0, choices.Count)];

        if (settings.RewardPickStrategy == WormBalanceRewardPickStrategy.HighestEstimatedDpsGain)
            return PickHighestEstimatedDpsGainReward(
                choices,
                settings,
                mainState,
                acaciaState,
                out selectedDpsGain);

        RewardRarity bestRarity = RewardRarity.Common;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity > bestRarity)
                bestRarity = choices[i].Rarity;
        }

        int matchingCount = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] != null && choices[i].Rarity == bestRarity)
                matchingCount++;
        }

        int selectedIndex = Random.Range(0, matchingCount);
        int currentIndex = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i] == null || choices[i].Rarity != bestRarity)
                continue;

            if (currentIndex == selectedIndex)
                return choices[i];

            currentIndex++;
        }

        return choices[0];
    }

    private static RewardChoiceData PickHighestEstimatedDpsGainReward(
        List<RewardChoiceData> choices,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState,
        out float selectedDpsGain)
    {
        selectedDpsGain = float.MinValue;
        float bestRarityScore = -1f;
        var candidates = new List<RewardChoiceData>(choices.Count);

        for (int i = 0; i < choices.Count; i++)
        {
            RewardChoiceData choice = choices[i];

            if (choice == null || choice.Effect == null)
                continue;

            float dpsGain = CalculateEstimatedDpsGain(
                choice,
                settings,
                mainState,
                acaciaState);
            float rarityScore = (int)choice.Rarity;

            if (dpsGain > selectedDpsGain + 0.0001f)
            {
                candidates.Clear();
                candidates.Add(choice);
                selectedDpsGain = dpsGain;
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Abs(dpsGain - selectedDpsGain) > 0.0001f)
                continue;

            if (rarityScore > bestRarityScore)
            {
                candidates.Clear();
                candidates.Add(choice);
                bestRarityScore = rarityScore;
                continue;
            }

            if (Mathf.Approximately(rarityScore, bestRarityScore))
                candidates.Add(choice);
        }

        if (candidates.Count == 0)
            return choices[Random.Range(0, choices.Count)];

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static float CalculateEstimatedDpsGain(
        RewardChoiceData choice,
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        WeaponRuntimeState mainClone = mainState.Clone();
        AcaciaThornRuntimeState acaciaClone = acaciaState.Clone();
        RewardRuntimeContext clonedContext = new(
            mainClone,
            acaciaClone,
            () => BuildMainWeaponDamage(settings.MainWeaponConfig, mainClone),
            settings.MainWeaponConfig,
            settings.AcaciaThornConfig);

        WeaponPowerSnapshot before = EstimatePower(settings, mainState, acaciaState);

        if (!choice.Effect.CanApply(clonedContext))
            return float.NegativeInfinity;

        choice.Effect.Apply(clonedContext);

        WeaponPowerSnapshot after = EstimatePower(settings, mainClone, acaciaClone);

        float beforeDps = before.IsValid ? before.EstimatedDps : 0f;
        float afterDps = after.IsValid ? after.EstimatedDps : 0f;

        return afterDps - beforeDps;
    }

    private static WeaponRuntimeState CreateMainWeaponState(WeaponConfig config)
    {
        WeaponRuntimeState state = new();

        if (config == null)
            return state;

        state.SetFireRateBonusLimit(config.MaxFireRateBonus);
        state.SetProjectileSpeedBonusLimit(config.MaxProjectileSpeedBonus);
        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxCriticalChance,
            config.MaxCriticalDamageMultiplier,
            config.MaxPenetrationBonus,
            config.MaxParallelProjectiles,
            config.MaxSalvoExtraShots);

        return state;
    }

    private static AcaciaThornRuntimeState CreateAcaciaThornState(
        AcaciaThornWeaponConfig config)
    {
        AcaciaThornRuntimeState state = new();

        if (config == null)
            return state;

        state.SetProgressionLimits(
            config.MaxDamageMultiplier,
            config.MaxFireRateBonus,
            config.MaxSalvoExtraShots,
            config.MaxProjectileSpeedBonus,
            config.MaxCriticalChance,
            config.CriticalDamageMultiplier,
            config.MaxCriticalDamageMultiplier);
        state.SetBaseDamage(config.Damage);

        return state;
    }

    private static WeaponPowerSnapshot EstimatePower(
        WormBalanceSimulationSettings settings,
        WeaponRuntimeState mainState,
        AcaciaThornRuntimeState acaciaState)
    {
        return WeaponPowerEstimator.Estimate(
            settings.MainWeaponConfig,
            mainState,
            settings.AcaciaThornConfig,
            acaciaState);
    }

    private static int BuildMainWeaponDamage(
        WeaponConfig config,
        WeaponRuntimeState state)
    {
        if (config == null || config.Projectile == null || state == null)
            return 0;

        return WeaponRuntimeState.ClampDamage(
            config.Projectile.Damage * (double)state.DamageMultiplier);
    }

    private static float GetHeadPressureMultiplier(
        WormBalanceSimulationSettings settings,
        float headProgress)
    {
        return settings.HpConfig != null
            ? settings.HpConfig.GetHeadPathPressureMultiplier(headProgress)
            : 1f;
    }

    private static float ApplyRollback(
        WormBalanceSimulationSettings settings,
        int destroyedSegmentCount,
        float headProgress)
    {
        float pathLength = settings.PathMetrics.PathLength;

        if (pathLength <= 0f)
            return headProgress;

        float rollbackDistance = destroyedSegmentCount * settings.SegmentSpacing;
        float rollbackSpeed = Mathf.Max(0.01f, settings.RollbackSpeed);
        float forwardSpeed = Mathf.Max(0f, settings.WormSpeed * settings.SectionRollbackForwardSpeedMultiplier);
        float effectiveRollbackDistance = rollbackDistance *
            (rollbackSpeed / Mathf.Max(0.01f, rollbackSpeed + forwardSpeed));
        float rollbackProgress = effectiveRollbackDistance / pathLength;
        return Mathf.Clamp01(headProgress - rollbackProgress);
    }

    private static int EnsureHpAbovePrevious(int hp, int previousHp)
    {
        if (previousHp <= 0)
            return Mathf.Max(1, hp);

        if (previousHp >= WeaponRuntimeState.MaxProjectileDamage)
            return WeaponRuntimeState.MaxProjectileDamage;

        int minimumIncrease = GetMinimumVisibleHpIncrease(previousHp);

        return Mathf.Min(
            WeaponRuntimeState.MaxProjectileDamage,
            Mathf.Max(hp, previousHp + minimumIncrease));
    }

    private static int CalculateRemainingSectionHp(
        int currentHp,
        float dps,
        float elapsedDamageTime)
    {
        if (currentHp <= 1 || dps <= 0f || elapsedDamageTime <= 0f)
            return Mathf.Max(1, currentHp);

        float remainingHp = currentHp - (dps * elapsedDamageTime);
        return Mathf.Max(1, Mathf.CeilToInt(remainingHp));
    }

    private static float CalculateSectionDamageProgress(
        int currentHp,
        int remainingHp)
    {
        if (currentHp <= 0)
            return 0f;

        return Mathf.Clamp01((currentHp - remainingHp) / (float)currentHp);
    }

    private static int GetMinimumVisibleHpIncrease(int previousHp)
    {
        if (previousHp < ThousandHp)
            return 1;

        if (previousHp < TenThousandHp)
            return 100;

        if (previousHp < MillionHp)
            return 1000;

        if (previousHp < TenMillionHp)
            return 100000;

        return 1000000;
    }

    private static int CountProgressSegments(WormBalanceSectionState[] sections)
    {
        if (sections == null)
            return 0;

        int count = 0;

        for (int i = 0; i < sections.Length; i++)
            count += sections[i].SegmentCount;

        return count;
    }

    private static float GetDestructionProgress(
        float destroyedSegments,
        int totalSegments)
    {
        return totalSegments > 0
            ? Mathf.Clamp01(destroyedSegments / (float)totalSegments)
            : 0f;
    }

    private static void AppendRewardLog(
        StringBuilder builder,
        float time,
        CocoonRewardProfile cocoonProfile,
        RewardChoiceData reward,
        float dpsGain)
    {
        if (builder == null)
            return;

        if (builder.Length > 0)
            builder.Append(" | ");

        string profileName = cocoonProfile != null
            ? cocoonProfile.DisplayName
            : "NoProfile";

        if (reward == null)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0.0}s {1}: no reward",
                time,
                profileName);
            return;
        }

        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0:0.0}s {1}: {2} {3} {4}",
            time,
            profileName,
            reward.Rarity,
            reward.Title,
            reward.ValueText);

        if (dpsGain > 0f)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                " (+{0:0.00} DPS)",
                dpsGain);
        }
    }
}

internal sealed class WormBalanceSectionState
{
    public readonly int Index;
    public readonly int SegmentCount;
    public readonly CocoonRewardProfile CocoonProfile;

    public int Hp;
    public bool HasCocoon => CocoonProfile != null;

    public WormBalanceSectionState(
        int index,
        int segmentCount,
        CocoonRewardProfile cocoonProfile)
    {
        Index = index;
        SegmentCount = Mathf.Max(1, segmentCount);
        CocoonProfile = cocoonProfile;
    }
}

internal readonly struct WormBalanceAdSessionStats
{
    public readonly int AdsWatched;
    public readonly int FreeRerollsUsed;
    public readonly int AdRerollsUsed;
    public readonly int TakeAllAdsUsed;
    public readonly int RevivesUsed;

    public WormBalanceAdSessionStats(
        int adsWatched,
        int freeRerollsUsed,
        int adRerollsUsed,
        int takeAllAdsUsed,
        int revivesUsed)
    {
        AdsWatched = Mathf.Max(0, adsWatched);
        FreeRerollsUsed = Mathf.Max(0, freeRerollsUsed);
        AdRerollsUsed = Mathf.Max(0, adRerollsUsed);
        TakeAllAdsUsed = Mathf.Max(0, takeAllAdsUsed);
        RevivesUsed = Mathf.Max(0, revivesUsed);
    }
}

internal sealed class WormBalanceAdSessionState
{
    private int _freeRerollsLeft;
    private int _adRerollsLeft;
    private int _takeAllAdsLeft;
    private int _revivesLeft;

    private WormBalanceAdSessionState(
        int freeRerollsLeft,
        int adRerollsLeft,
        int takeAllAdsLeft,
        int revivesLeft)
    {
        _freeRerollsLeft = Mathf.Max(0, freeRerollsLeft);
        _adRerollsLeft = Mathf.Max(0, adRerollsLeft);
        _takeAllAdsLeft = Mathf.Max(0, takeAllAdsLeft);
        _revivesLeft = Mathf.Max(0, revivesLeft);
    }

    public int AdsWatched { get; private set; }
    public int FreeRerollsUsed { get; private set; }
    public int AdRerollsUsed { get; private set; }
    public int TakeAllAdsUsed { get; private set; }
    public int RevivesUsed { get; private set; }

    public static WormBalanceAdSessionState Create(
        WormBalanceSimulationSettings settings,
        WormBalanceScenario scenario)
    {
        bool allowPaidAssist = scenario is
            WormBalanceScenario.AdsAssistNoRevive or
            WormBalanceScenario.AdsAssist;
        bool allowRevive = scenario is
            WormBalanceScenario.ReviveOnly or
            WormBalanceScenario.AdsAssist;

        return new WormBalanceAdSessionState(
            settings.FreeRerollAttemptsPerSession,
            allowPaidAssist ? settings.AdRerollAttemptsPerSession : 0,
            allowPaidAssist ? settings.TakeAllAttemptsPerSession : 0,
            allowRevive ? settings.ReviveAttemptsPerSession : 0);
    }

    public bool TryUseFreeReroll()
    {
        if (_freeRerollsLeft <= 0)
            return false;

        _freeRerollsLeft--;
        FreeRerollsUsed++;
        return true;
    }

    public bool TryUseAdReroll()
    {
        if (_adRerollsLeft <= 0)
            return false;

        _adRerollsLeft--;
        AdRerollsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseTakeAll()
    {
        if (_takeAllAdsLeft <= 0)
            return false;

        _takeAllAdsLeft--;
        TakeAllAdsUsed++;
        AdsWatched++;
        return true;
    }

    public bool TryUseRevive()
    {
        if (_revivesLeft <= 0)
            return false;

        _revivesLeft--;
        RevivesUsed++;
        AdsWatched++;
        return true;
    }

    public WormBalanceAdSessionStats ToStats()
    {
        return new WormBalanceAdSessionStats(
            AdsWatched,
            FreeRerollsUsed,
            AdRerollsUsed,
            TakeAllAdsUsed,
            RevivesUsed);
    }
}

internal readonly struct WormBalanceRewardChoiceEvaluation
{
    public readonly RewardChoiceData Reward;
    public readonly float DpsGain;

    public WormBalanceRewardChoiceEvaluation(
        RewardChoiceData reward,
        float dpsGain)
    {
        Reward = reward;
        DpsGain = dpsGain;
    }
}

internal sealed class WormBalanceRewardOffer
{
    private readonly List<WormBalanceRewardChoiceEvaluation> _evaluations;

    public WormBalanceRewardOffer(
        List<RewardChoiceData> choices,
        List<WormBalanceRewardChoiceEvaluation> evaluations,
        RewardChoiceData selectedReward,
        float selectedDpsGain)
    {
        Choices = choices ?? new List<RewardChoiceData>();
        _evaluations = evaluations ?? new List<WormBalanceRewardChoiceEvaluation>();
        SelectedReward = selectedReward;
        SelectedDpsGain = selectedDpsGain;

        for (int i = 0; i < _evaluations.Count; i++)
        {
            float dpsGain = _evaluations[i].DpsGain;

            if (dpsGain <= 0.0001f)
                continue;

            TotalPositiveDpsGain += dpsGain;
            BeneficialRewardCount++;
        }
    }

    public readonly List<RewardChoiceData> Choices;
    public readonly RewardChoiceData SelectedReward;
    public readonly float SelectedDpsGain;
    public readonly float TotalPositiveDpsGain;
    public readonly int BeneficialRewardCount;

    public WormBalanceRewardSelection CreateSingleSelection()
    {
        var rewards = new List<RewardChoiceData>(1);

        if (SelectedReward != null)
            rewards.Add(SelectedReward);

        return new WormBalanceRewardSelection(rewards, _evaluations);
    }

    public WormBalanceRewardSelection CreateTakeAllSelection()
    {
        var rewards = new List<RewardChoiceData>(Choices.Count);

        for (int i = 0; i < Choices.Count; i++)
        {
            RewardChoiceData reward = Choices[i];

            if (reward != null && reward.Effect != null)
                rewards.Add(reward);
        }

        return new WormBalanceRewardSelection(rewards, _evaluations);
    }
}

internal sealed class WormBalanceRewardSelection
{
    private readonly List<WormBalanceRewardChoiceEvaluation> _evaluations;

    public WormBalanceRewardSelection(
        List<RewardChoiceData> rewards,
        List<WormBalanceRewardChoiceEvaluation> evaluations)
    {
        Rewards = rewards ?? new List<RewardChoiceData>();
        _evaluations = evaluations ?? new List<WormBalanceRewardChoiceEvaluation>();
    }

    public readonly List<RewardChoiceData> Rewards;

    public float GetDpsGain(RewardChoiceData reward)
    {
        if (reward == null)
            return 0f;

        for (int i = 0; i < _evaluations.Count; i++)
        {
            if (ReferenceEquals(_evaluations[i].Reward, reward))
                return _evaluations[i].DpsGain;
        }

        return 0f;
    }
}

internal sealed class WormBalanceRunResult
{
    public readonly WormBalanceScenario Scenario;
    public readonly int RunIndex;
    public readonly bool Won;
    public readonly string Reason;
    public readonly float TimeSeconds;
    public readonly float DestructionProgress;
    public readonly float EndpointDestructionProgress;
    public readonly float EndpointSectionDamageProgress;
    public readonly float HeadProgress;
    public readonly int SectionsDestroyed;
    public readonly int LastSectionIndex;
    public readonly int RewardsTaken;
    public readonly float FirstRewardTime;
    public readonly float FinalDps;
    public readonly float EndPlayerX;
    public readonly float EndHeadX;
    public readonly float MaxPlayerXError;
    public readonly WormBalancePathLocation EndLocation;
    public readonly WormBalanceAdSessionStats AdStats;
    public readonly string RewardLog;

    private WormBalanceRunResult(
        WormBalanceScenario scenario,
        int runIndex,
        bool won,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float endpointDestructionProgress,
        float endpointSectionDamageProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog)
    {
        Scenario = scenario;
        RunIndex = runIndex;
        Won = won;
        Reason = reason;
        TimeSeconds = timeSeconds;
        DestructionProgress = destructionProgress;
        EndpointDestructionProgress = Mathf.Clamp01(endpointDestructionProgress);
        EndpointSectionDamageProgress = Mathf.Clamp01(endpointSectionDamageProgress);
        HeadProgress = headProgress;
        SectionsDestroyed = sectionsDestroyed;
        LastSectionIndex = lastSectionIndex;
        RewardsTaken = rewardsTaken;
        FirstRewardTime = firstRewardTime;
        FinalDps = finalDps;
        EndPlayerX = endPlayerX;
        EndHeadX = endHeadX;
        MaxPlayerXError = maxPlayerXError;
        EndLocation = endLocation;
        AdStats = adStats;
        RewardLog = rewardLog ?? string.Empty;
    }

    public static WormBalanceRunResult Win(
        WormBalanceScenario scenario,
        int runIndex,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog,
        float endpointDestructionProgress = -1f,
        float endpointSectionDamageProgress = 1f)
    {
        float resolvedEndpointProgress = endpointDestructionProgress >= 0f
            ? endpointDestructionProgress
            : destructionProgress;

        return new WormBalanceRunResult(
            scenario,
            runIndex,
            true,
            "Worm destroyed",
            timeSeconds,
            destructionProgress,
            resolvedEndpointProgress,
            endpointSectionDamageProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endPlayerX,
            endHeadX,
            maxPlayerXError,
            endLocation,
            adStats,
            rewardLog);
    }

    public static WormBalanceRunResult Loss(
        WormBalanceScenario scenario,
        int runIndex,
        string reason,
        float timeSeconds,
        float destructionProgress,
        float headProgress,
        int sectionsDestroyed,
        int lastSectionIndex,
        int rewardsTaken,
        float firstRewardTime,
        float finalDps,
        float endPlayerX,
        float endHeadX,
        float maxPlayerXError,
        WormBalancePathLocation endLocation,
        WormBalanceAdSessionStats adStats,
        string rewardLog,
        float endpointDestructionProgress = -1f,
        float endpointSectionDamageProgress = 0f)
    {
        float resolvedEndpointProgress = endpointDestructionProgress >= 0f
            ? endpointDestructionProgress
            : destructionProgress;

        return new WormBalanceRunResult(
            scenario,
            runIndex,
            false,
            reason,
            timeSeconds,
            destructionProgress,
            resolvedEndpointProgress,
            endpointSectionDamageProgress,
            headProgress,
            sectionsDestroyed,
            lastSectionIndex,
            rewardsTaken,
            firstRewardTime,
            finalDps,
            endPlayerX,
            endHeadX,
            maxPlayerXError,
            endLocation,
            adStats,
            rewardLog);
    }

    public bool IsEndpointLoss => !Won && HeadProgress >= 0.999f;

    public string BuildDebugLine()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "WormBalance scenario={0} run={1} result={2} reason='{3}' time={4:0.0}s destroyed={5:0.0}% endpointDestroyed={6:0.0}% endpointSectionDamage={7:0.0}% head={8:0.0}% bucket={9} rail={10} sections={11} rewards={12} firstReward={13} dps={14:0.00} ads={15} freeRerolls={16} adRerolls={17} takeAllAds={18} revives={19} playerX={20:0.00} headX={21:0.00} xError={22:0.00}",
            Scenario,
            RunIndex,
            Won ? "WIN" : "LOSS",
            Reason,
            TimeSeconds,
            DestructionProgress * 100f,
            EndpointDestructionProgress * 100f,
            EndpointSectionDamageProgress * 100f,
            HeadProgress * 100f,
            EndLocation.BucketLabel,
            EndLocation.ControlPointLabel,
            SectionsDestroyed,
            RewardsTaken,
            FirstRewardTime >= 0f ? FirstRewardTime.ToString("0.0s", CultureInfo.InvariantCulture) : "none",
            FinalDps,
            AdStats.AdsWatched,
            AdStats.FreeRerollsUsed,
            AdStats.AdRerollsUsed,
            AdStats.TakeAllAdsUsed,
            AdStats.RevivesUsed,
            EndPlayerX,
            EndHeadX,
            MaxPlayerXError);
    }
}

internal sealed class WormBalanceSimulationReport
{
    private const float NoAdsTargetMinWinRate = 0.6f;
    private const float NoAdsTargetMaxWinRate = 0.7f;
    private const float ReviveOnlyTargetMinWinRate = 0.98f;
    private const float ReviveOnlyTargetMaxWinRate = 1f;
    private const float AdsNoReviveTargetMinWinRate = 0.85f;
    private const float AdsNoReviveTargetMaxWinRate = 0.9f;
    private const float FullAdsTargetMinWinRate = 0.98f;
    private const float FullAdsTargetMaxWinRate = 1f;

    public readonly WormBalanceSimulationSettings Settings;
    public readonly List<WormBalanceRunResult> Runs;

    public WormBalanceSimulationReport(
        WormBalanceSimulationSettings settings,
        List<WormBalanceRunResult> runs)
    {
        Settings = settings;
        Runs = runs ?? new List<WormBalanceRunResult>();
    }

    public string BuildSummary()
    {
        StringBuilder builder = new();
        builder.AppendLine("Worm Balance Lab");
        builder.AppendLine($"Simulated games: {Runs.Count} ({Settings.RunCount} per scenario)");
        builder.AppendLine($"Targets: No Ads/No Revive {NoAdsTargetMinWinRate * 100f:0}-{NoAdsTargetMaxWinRate * 100f:0}% wins, Revive Only {ReviveOnlyTargetMinWinRate * 100f:0}-{ReviveOnlyTargetMaxWinRate * 100f:0}% wins, Ads No Revive {AdsNoReviveTargetMinWinRate * 100f:0}-{AdsNoReviveTargetMaxWinRate * 100f:0}% wins, Full Ads {FullAdsTargetMinWinRate * 100f:0}-{FullAdsTargetMaxWinRate * 100f:0}% wins");
        builder.AppendLine(
            $"Setup: reward={Settings.RewardPickStrategy}, ads={Settings.AdSimulationMode}, worm={Settings.SectionCount} sections / {WormPatternBuilder.GetBodySegmentCount(Settings.SectionCount)} body segments / {Settings.PathTimeLimitSeconds:0.0}s path");
        builder.AppendLine(
            $"Damage: estimated DPS x {Settings.HitEfficiency:0.00}, rollback={(Settings.ApplySectionRollback ? "ON" : "OFF")} speed={Settings.RollbackSpeed:0.0} forward x{Settings.SectionRollbackForwardSpeedMultiplier:0.00}, pressure={(Settings.UseRuntimePressure ? "ON" : "OFF")}");
        builder.AppendLine(
            $"Ad power: reroll rare+, legendary after {RewardAdRerollPolicy.LegendaryChanceMinDangerProgress * 100f:0}% danger, take all after {Settings.TakeAllMinHeadPathProgress * 100f:0}% path and {Settings.TakeAllMinTotalDpsGainRatio:0.00}x total DPS gain");
        builder.AppendLine($"Ad limits: free reroll={Settings.FreeRerollAttemptsPerSession}, ad reroll={Settings.AdRerollAttemptsPerSession}, take all={Settings.TakeAllAttemptsPerSession}, revive={Settings.ReviveAttemptsPerSession}");
        builder.AppendLine(Settings.SimulatePlayerXFollow
            ? "Player X follow: ON, instant head X match"
            : "Player X follow: OFF");
        builder.AppendLine();

        if (Settings.IncludesScenario(WormBalanceScenario.NoAds))
            AppendScenarioSummary(builder, WormBalanceScenario.NoAds, "No Ads / No Revive", NoAdsTargetMinWinRate, NoAdsTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.ReviveOnly))
            AppendScenarioSummary(builder, WormBalanceScenario.ReviveOnly, "Revive Only", ReviveOnlyTargetMinWinRate, ReviveOnlyTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.AdsAssistNoRevive))
            AppendScenarioSummary(builder, WormBalanceScenario.AdsAssistNoRevive, "Ads Assist / No Revive", AdsNoReviveTargetMinWinRate, AdsNoReviveTargetMaxWinRate);

        if (Settings.IncludesScenario(WormBalanceScenario.AdsAssist))
            AppendScenarioSummary(builder, WormBalanceScenario.AdsAssist, "Full Ads", FullAdsTargetMinWinRate, FullAdsTargetMaxWinRate);

        AppendAssistanceUplift(builder);

        return builder.ToString();
    }

    private void AppendScenarioSummary(
        StringBuilder builder,
        WormBalanceScenario scenario,
        string title,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        int sampleCount = 0;
        int winCount = 0;
        int lossCount = 0;
        int firstRewardSamples = 0;
        int adSessionCount = 0;
        float totalTime = 0f;
        float totalRewards = 0f;
        float totalFirstRewardTime = 0f;
        float totalAdsWatched = 0f;
        float totalAdRerolls = 0f;
        float totalTakeAllAds = 0f;
        float totalRevives = 0f;
        float totalEndpointLossRewards = 0f;
        float totalEndpointLossTime = 0f;
        List<float> adsWatched = new();
        List<float> lossProgress = new();
        List<float> lossHeadProgress = new();
        List<float> endpointLossProgress = new();
        List<float> endpointLossSectionDamage = new();
        List<float> endpointLossSections = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Scenario != scenario)
                continue;

            sampleCount++;
            totalTime += run.TimeSeconds;
            totalRewards += run.RewardsTaken;
            totalAdsWatched += run.AdStats.AdsWatched;
            totalAdRerolls += run.AdStats.AdRerollsUsed;
            totalTakeAllAds += run.AdStats.TakeAllAdsUsed;
            totalRevives += run.AdStats.RevivesUsed;
            adsWatched.Add(run.AdStats.AdsWatched);

            if (run.AdStats.AdsWatched > 0)
                adSessionCount++;

            if (run.FirstRewardTime >= 0f)
            {
                totalFirstRewardTime += run.FirstRewardTime;
                firstRewardSamples++;
            }

            if (run.Won)
            {
                winCount++;
            }
            else
            {
                lossCount++;
                lossProgress.Add(run.DestructionProgress);
                lossHeadProgress.Add(run.HeadProgress);

                if (run.IsEndpointLoss)
                {
                    endpointLossProgress.Add(run.EndpointDestructionProgress);
                    endpointLossSectionDamage.Add(run.EndpointSectionDamageProgress);
                    endpointLossSections.Add(run.SectionsDestroyed + 1f);
                    totalEndpointLossRewards += run.RewardsTaken;
                    totalEndpointLossTime += run.TimeSeconds;
                }
            }
        }

        builder.AppendLine($"{title}:");

        if (sampleCount == 0)
        {
            builder.AppendLine("No samples.");
            builder.AppendLine();
            return;
        }

        float samples = sampleCount;
        float winRate = winCount / samples;
        float averageRewards = totalRewards / samples;
        float averageFirstRewardTime = firstRewardSamples > 0
            ? totalFirstRewardTime / firstRewardSamples
            : -1f;

        adsWatched.Sort();
        lossProgress.Sort();
        lossHeadProgress.Sort();
        endpointLossProgress.Sort();
        endpointLossSectionDamage.Sort();
        endpointLossSections.Sort();

        builder.AppendLine(
            $"Wins: {winCount}/{sampleCount} ({winRate * 100f:0.0}%) | Losses: {lossCount}/{sampleCount} ({(1f - winRate) * 100f:0.0}%) | Target: {targetMinWinRate * 100f:0}-{targetMaxWinRate * 100f:0}% | {GetWinRateVerdict(winRate, targetMinWinRate, targetMaxWinRate)}");
        builder.AppendLine(firstRewardSamples > 0
            ? $"Avg: time={totalTime / samples:0.0}s, rewards={averageRewards:0.00}, first reward={averageFirstRewardTime:0.0}s"
            : $"Avg: time={totalTime / samples:0.0}s, rewards={averageRewards:0.00}, first reward=none");

        if (scenario != WormBalanceScenario.NoAds)
        {
            builder.AppendLine(
                $"Ads: avg={totalAdsWatched / samples:0.00}, p50={Percentile(adsWatched, 0.5f):0.0}, p90={Percentile(adsWatched, 0.9f):0.0}, sessions={adSessionCount / samples * 100f:0.0}% | uses: ad reroll={totalAdRerolls / samples:0.00}, take all={totalTakeAllAds / samples:0.00}, revive={totalRevives / samples:0.00}");
        }

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"Loss tension: destroyed avg={Average(lossProgress) * 100f:0.0}%, path avg={Average(lossHeadProgress) * 100f:0.0}%");

            if (endpointLossProgress.Count > 0)
            {
                float endpointLossSamples = endpointLossProgress.Count;
                float endpointProgressP10 = Percentile(endpointLossProgress, 0.1f);
                float endpointProgressP50 = Percentile(endpointLossProgress, 0.5f);
                float endpointProgressP90 = Percentile(endpointLossProgress, 0.9f);
                float endpointSectionP50 = Percentile(endpointLossSections, 0.5f);
                float averageEndpointSectionDamage = Average(endpointLossSectionDamage);
                builder.AppendLine(
                    $"Endpoint losses: samples={endpointLossProgress.Count}, " +
                    $"destroyed p10/p50/p90={endpointProgressP10 * 100f:0.0}%/{endpointProgressP50 * 100f:0.0}%/{endpointProgressP90 * 100f:0.0}%, " +
                    $"section p50={endpointSectionP50:0.0}/{Settings.SectionCount}, " +
                    $"current section damage avg={averageEndpointSectionDamage * 100f:0.0}%, " +
                    $"rewards avg={totalEndpointLossRewards / endpointLossSamples:0.00}, " +
                    $"time avg={totalEndpointLossTime / endpointLossSamples:0.0}s");
            }
        }

        string verdict = BuildCompactScenarioVerdict(
            scenario,
            winRate,
            targetMinWinRate,
            targetMaxWinRate,
            averageRewards,
            averageFirstRewardTime,
            totalAdsWatched / samples,
            adSessionCount / samples,
            lossCount,
            lossProgress,
            lossHeadProgress,
            endpointLossProgress);
        builder.AppendLine(
            $"Verdict: {verdict}");
        builder.AppendLine();
    }

    private void AppendAssistanceUplift(StringBuilder builder)
    {
        if (!TryGetWinRate(WormBalanceScenario.NoAds, out float noAdsWinRate))
            return;

        if (TryGetWinRate(WormBalanceScenario.ReviveOnly, out float reviveOnlyWinRate))
        {
            builder.AppendLine(
                $"Revive rescue uplift: +{(reviveOnlyWinRate - noAdsWinRate) * 100f:0.0} pp | target: revive should convert most endpoint losses into wins");
        }

        if (TryGetWinRate(WormBalanceScenario.AdsAssistNoRevive, out float adsNoReviveWinRate))
        {
            builder.AppendLine(
                $"Paid assist uplift without revive: +{(adsNoReviveWinRate - noAdsWinRate) * 100f:0.0} pp | target: paid reroll/take-all should help, not replace revive");
        }

        if (TryGetWinRate(WormBalanceScenario.AdsAssist, out float fullAdsWinRate))
        {
            builder.AppendLine(
                $"Full ads uplift: +{(fullAdsWinRate - noAdsWinRate) * 100f:0.0} pp | target: full assist should feel like a near-guaranteed save");
        }
    }

    private bool TryGetWinRate(
        WormBalanceScenario scenario,
        out float winRate)
    {
        int sampleCount = 0;
        int winCount = 0;

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Scenario != scenario)
                continue;

            sampleCount++;

            if (run.Won)
                winCount++;
        }

        if (sampleCount <= 0)
        {
            winRate = 0f;
            return false;
        }

        winRate = winCount / (float)sampleCount;
        return true;
    }

    private static string GetWinRateVerdict(
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate)
    {
        if (winRate < targetMinWinRate)
            return "too hard";

        if (winRate > targetMaxWinRate)
            return "too easy";

        return "OK";
    }

    private static string BuildCompactScenarioVerdict(
        WormBalanceScenario scenario,
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate,
        float averageRewards,
        float averageFirstRewardTime,
        float averageAdsWatched,
        float adSessionRate,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress,
        List<float> endpointLossProgress)
    {
        List<string> notes = new();

        if (winRate < targetMinWinRate)
            notes.Add(scenario switch
            {
                WormBalanceScenario.NoAds => "baseline is too harsh before revive",
                WormBalanceScenario.ReviveOnly => "revive does not reliably save the run",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is under target",
                WormBalanceScenario.AdsAssist => "full ad assist is under target",
                _ => "win rate is under target"
            });
        else if (winRate > targetMaxWinRate)
            notes.Add(scenario switch
            {
                WormBalanceScenario.NoAds => "pre-revive pressure is too weak",
                WormBalanceScenario.ReviveOnly => "revive save rate is at cap",
                WormBalanceScenario.AdsAssistNoRevive => "paid assist without revive is too strong",
                WormBalanceScenario.AdsAssist => "full ad assist is at cap",
                _ => "win rate is above target"
            });
        else
            notes.Add("win rate is in target");

        if (averageFirstRewardTime < 0f)
            notes.Add("first reward is unreachable");
        else if (averageFirstRewardTime > 12f)
            notes.Add("first reward is late");
        else if (averageFirstRewardTime <= 6f)
            notes.Add("first reward is very early");

        if (averageRewards > 22f)
            notes.Add("reward count is high");
        else if (averageRewards < 10f)
            notes.Add("reward count is low");

        if (scenario != WormBalanceScenario.NoAds)
        {
            if (averageAdsWatched > 2f)
                notes.Add("ads are frequent");
            else if (averageAdsWatched < 0.6f || adSessionRate < 0.5f)
                notes.Add("ads may feel underused");
        }

        if (lossCount > 0)
        {
            List<float> endpointProgressForVerdict = endpointLossProgress != null && endpointLossProgress.Count > 0
                ? endpointLossProgress
                : lossProgress;
            float averageLossDestroyed = Average(endpointProgressForVerdict);
            float averageLossPath = Average(lossHeadProgress);

            if (averageLossPath >= 0.95f &&
                averageLossDestroyed >= 0.7f &&
                averageLossDestroyed <= 0.82f)
            {
                notes.Add("endpoint pressure is in the revive offer zone");
            }
            else if (averageLossPath >= 0.95f && averageLossDestroyed < 0.65f)
            {
                notes.Add("endpoint catches too early; lower mid HP or rollback pressure");
            }
            else if (averageLossPath >= 0.95f && averageLossDestroyed > 0.85f)
            {
                notes.Add("losses are too close; increase path pressure or reduce rollback relief");
            }
        }

        return string.Join("; ", notes);
    }

    private static void AppendScenarioVerdict(
        StringBuilder builder,
        WormBalanceScenario scenario,
        float winRate,
        float targetMinWinRate,
        float targetMaxWinRate,
        float averageRewards,
        float averageFirstRewardTime,
        float averageAdsWatched,
        float adSessionRate,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress)
    {
        builder.AppendLine("Verdict:");

        if (winRate >= targetMinWinRate && winRate <= targetMaxWinRate)
        {
            builder.AppendLine("- Win rate is inside the target tension band.");
        }
        else if (winRate < targetMinWinRate)
        {
            builder.AppendLine("- Too hard for this scenario. Lower mid/end HP or increase reward pressure.");

            if (scenario == WormBalanceScenario.NoAds && winRate < 0.45f)
                builder.AppendLine("- No-ads win rate is dangerously low. Player may read this as a paywall.");
        }
        else
        {
            builder.AppendLine("- Too easy for this scenario. Raise mid/end HP or delay paid-help power.");
        }

        if (averageFirstRewardTime < 0f)
            builder.AppendLine("- Player does not reach the first reward. Lower early HP heavily.");
        else if (averageFirstRewardTime > 12f)
            builder.AppendLine("- First reward is late. Lower only early HP.");
        else if (averageFirstRewardTime < 6f)
            builder.AppendLine("- First reward is very fast. Early hook is strong.");
        else
            builder.AppendLine("- First reward timing is good.");

        if (averageRewards < 10f)
            builder.AppendLine("- Too few rewards for a fun survival curve. Early/mid HP is probably too high.");
        else if (averageRewards > 22f)
            builder.AppendLine("- Reward count is very high. Watch for runaway DPS spikes.");
        else
            builder.AppendLine("- Reward count is in a usable range.");

        if (scenario != WormBalanceScenario.NoAds)
        {
            if (averageAdsWatched < 0.6f || adSessionRate < 0.5f)
                builder.AppendLine("- Ads are underused. The player may not feel the revive/take-all offer often enough.");
            else if (averageAdsWatched > 2f)
                builder.AppendLine("- Ads are too frequent. Lower paid attempts or make take-all stricter.");
            else
                builder.AppendLine("- Ad pressure is in the intended 1-ish view per session zone.");
        }

        if (lossCount <= 0)
            return;

        float averageLossDestroyed = Average(lossProgress);
        float averageLossPath = Average(lossHeadProgress);

        if (averageLossPath >= 0.95f &&
            averageLossDestroyed >= 0.7f &&
            averageLossDestroyed <= 0.82f)
        {
            builder.AppendLine("- Losses happen at endpoint with 70-80% worm destroyed. This is the intended revive offer zone.");
        }
        else if (averageLossPath >= 0.95f && averageLossDestroyed < 0.65f)
        {
            builder.AppendLine("- Endpoint catches too early. Lower mid HP or reduce path pressure.");
        }
        else if (averageLossPath >= 0.95f && averageLossDestroyed > 0.85f)
        {
            builder.AppendLine("- Losses are too close. Increase path pressure or reduce rollback relief.");
        }
    }

    private static void AppendBalanceVerdict(
        StringBuilder builder,
        float winRate,
        float averageRewards,
        float averageFirstRewardTime,
        int lossCount,
        List<float> lossProgress,
        List<float> lossHeadProgress)
    {
        float averageLossDestroyed = Average(lossProgress);
        float averageLossPath = Average(lossHeadProgress);

        builder.AppendLine("Verdict:");

        if (averageFirstRewardTime < 0f)
        {
            builder.AppendLine("- Player does not reach the first reward. Lower early HP heavily.");
        }
        else if (averageFirstRewardTime > 12f)
        {
            builder.AppendLine(
                $"- First reward is late ({averageFirstRewardTime:0.0}s). Lower only early HP, keep mid/end pressure.");
        }
        else if (averageFirstRewardTime < 6f)
        {
            builder.AppendLine(
                $"- First reward is very fast ({averageFirstRewardTime:0.0}s). Early hook is strong enough.");
        }
        else
        {
            builder.AppendLine(
                $"- First reward timing is good ({averageFirstRewardTime:0.0}s).");
        }

        if (averageRewards < 3f)
        {
            builder.AppendLine(
                $"- Too few rewards before loss ({averageRewards:0.00}). Early/mid HP is too high for fun pacing.");
        }
        else if (averageRewards > 7f)
        {
            builder.AppendLine(
                $"- Many rewards before loss ({averageRewards:0.00}). Raise mid/end HP if win rate grows.");
        }
        else
        {
            builder.AppendLine(
                $"- Reward count before loss is in a useful range ({averageRewards:0.00}).");
        }

        if (winRate <= 0.1f)
        {
            builder.AppendLine(
                $"- Loss target is strong ({winRate * 100f:0.0}% wins). Keep this if revive/ad continuation is the goal.");
        }
        else
        {
            builder.AppendLine(
                $"- Too many wins ({winRate * 100f:0.0}%). Raise mid/end HP, not early HP.");
        }

        if (lossCount > 0)
        {
            builder.AppendLine(
                $"- Loss happens at path {averageLossPath * 100f:0.0}% with worm destroyed {averageLossDestroyed * 100f:0.0}%.");

            if (averageLossPath >= 0.95f && averageLossDestroyed < 0.9f)
                builder.AppendLine("- Player reaches the endpoint with a lot of worm left. Make early rewards faster, then retest.");
            else if (averageLossPath >= 0.85f && averageLossDestroyed >= 0.85f)
                builder.AppendLine("- Loss is late and close. This is a good tension zone.");
        }

        builder.AppendLine("Recommended first tuning:");
        builder.AppendLine("- Set early Base Section HP around 3-4.");
        builder.AppendLine("- Set target lifetime curve early keys around 1.1-1.6s, first reward target 8-12s.");
        builder.AppendLine("- If wins appear after that, raise only mid/end lifetime or pressure curve.");
    }

    private void AppendWorstLosses(StringBuilder builder)
    {
        int appended = 0;
        HashSet<int> listedRuns = new();

        for (int i = 0; i < Runs.Count && appended < 8; i++)
        {
            WormBalanceRunResult worst = null;

            for (int j = 0; j < Runs.Count; j++)
            {
                WormBalanceRunResult candidate = Runs[j];

                if (candidate.Won)
                    continue;

                if (listedRuns.Contains(candidate.RunIndex))
                    continue;

                if (worst == null || candidate.DestructionProgress < worst.DestructionProgress)
                    worst = candidate;
            }

            if (worst == null)
                break;

            builder.AppendLine(worst.BuildDebugLine());
            listedRuns.Add(worst.RunIndex);
            appended++;
        }

        if (appended == 0)
            builder.AppendLine("No losses.");
    }

    private void AppendLocationDistribution(
        StringBuilder builder,
        string title,
        bool won)
    {
        int total = 0;
        Dictionary<int, int> bucketCounts = new();
        Dictionary<int, int> controlPointCounts = new();

        for (int i = 0; i < Runs.Count; i++)
        {
            WormBalanceRunResult run = Runs[i];

            if (run.Won != won)
                continue;

            total++;
            Increment(bucketCounts, run.EndLocation.BucketIndex);
            Increment(controlPointCounts, run.EndLocation.ControlPointIndex);
        }

        builder.AppendLine($"{title}:");

        if (total == 0)
        {
            builder.AppendLine("No samples.");
            return;
        }

        builder.Append("Path buckets: ");
        AppendBucketCounts(
            builder,
            bucketCounts,
            total,
            Settings.ProgressBucketCount);
        builder.AppendLine();

        if (Settings.PathMetrics.ControlPointCount <= 0)
        {
            builder.AppendLine("Rail control points: no RailPath assigned.");
            return;
        }

        builder.Append("Rail control points: ");
        AppendControlPointCounts(
            builder,
            controlPointCounts,
            total,
            Settings.PathMetrics.ControlPointCount);
        builder.AppendLine();
    }

    private static void AppendBucketCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int bucketCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < bucketCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            float start = i / (float)Mathf.Max(1, bucketCount) * 100f;
            float end = (i + 1) / (float)Mathf.Max(1, bucketCount) * 100f;
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:0}-{1:0}%: {2} ({3:0.0}%)",
                start,
                end,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void AppendControlPointCounts(
        StringBuilder builder,
        Dictionary<int, int> counts,
        int total,
        int controlPointCount)
    {
        bool wroteAny = false;

        for (int i = 0; i < controlPointCount; i++)
        {
            if (!counts.TryGetValue(i, out int count))
                continue;

            if (wroteAny)
                builder.Append("; ");

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "CP {0}: {1} ({2:0.0}%)",
                i,
                count,
                count / (float)total * 100f);
            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("none");
    }

    private static void Increment(Dictionary<int, int> counts, int key)
    {
        if (counts.TryGetValue(key, out int value))
        {
            counts[key] = value + 1;
            return;
        }

        counts.Add(key, 1);
    }

    private void AppendSampleRuns(StringBuilder builder)
    {
        int count = Mathf.Min(8, Runs.Count);

        for (int i = 0; i < count; i++)
            builder.AppendLine(Runs[i].BuildDebugLine());
    }

    private static float Average(List<float> values)
    {
        if (values == null || values.Count == 0)
            return 0f;

        float total = 0f;

        for (int i = 0; i < values.Count; i++)
            total += values[i];

        return total / values.Count;
    }

    private static float Percentile(List<float> sortedValues, float percentile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return 0f;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        float position = Mathf.Clamp01(percentile) * (sortedValues.Count - 1);
        int lower = Mathf.FloorToInt(position);
        int upper = Mathf.CeilToInt(position);

        if (lower == upper)
            return sortedValues[lower];

        return Mathf.Lerp(
            sortedValues[lower],
            sortedValues[upper],
            position - lower);
    }
}
