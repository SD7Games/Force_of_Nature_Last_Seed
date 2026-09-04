using System;
using System.Collections;
using System.Collections.Generic;

public sealed class WormSpawnLifecycle
{
    private readonly WormSegmentPool _segmentPool;
    private readonly WormFactory _wormFactory;
    private readonly WormSpawnSettings _settings;
    private readonly WormAdaptiveHpController _adaptiveHpController;
    private readonly WormController _wormController;
    private readonly WormCombatController _wormCombat;
    private readonly WormSectionHpPresenter _hpPresenter;
    private readonly WormFaceBurstPresenter _faceBurstPresenter;
    private readonly List<WormSegment> _activeSegments = new();
    private readonly List<WormSection> _activeSections = new();

    private WormSegment _head;

    public WormSpawnLifecycle(
        WormSegmentPool segmentPool,
        WormFactory wormFactory,
        WormSpawnSettings settings,
        WormAdaptiveHpController adaptiveHpController,
        WormController wormController,
        WormCombatController wormCombat,
        WormSectionHpPresenter hpPresenter,
        WormFaceBurstPresenter faceBurstPresenter)
    {
        _segmentPool = segmentPool ?? throw new ArgumentNullException(nameof(segmentPool));
        _wormFactory = wormFactory ?? throw new ArgumentNullException(nameof(wormFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _adaptiveHpController = adaptiveHpController ??
            throw new ArgumentNullException(nameof(adaptiveHpController));
        _wormController = wormController ?? throw new ArgumentNullException(nameof(wormController));
        _wormCombat = wormCombat ?? throw new ArgumentNullException(nameof(wormCombat));
        _hpPresenter = hpPresenter ?? throw new ArgumentNullException(nameof(hpPresenter));
        _faceBurstPresenter = faceBurstPresenter ??
            throw new ArgumentNullException(nameof(faceBurstPresenter));
    }

    public bool IsSpawned { get; private set; }

    public IEnumerator PrewarmRoutine()
    {
        return _segmentPool.PrewarmRoutine(
            _settings.BodyPoolCapacity,
            _settings.PrewarmBatchSize);
    }

    public void Spawn(
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        float currentTime)
    {
        if (IsSpawned)
            return;

        List<WormSegment> segments = null;
        List<WormSection> sections = null;
        WormSegment head = null;
        WormSegment tail = null;

        try
        {
            segments = CreateSegmentViews(out head, out tail);
            sections = BuildSectionModels(segments, cocoonProfiles, currentTime);
            BindGameplayAndPresentation(segments, sections, head, tail);
            CommitSpawn(segments, sections, head);
        }
        catch
        {
            RollbackFailedSpawn(segments, currentTime);
            throw;
        }
    }

    public void Despawn(float currentTime)
    {
        UnbindGameplayAndPresentation();
        ReleaseSegments(_activeSegments);
        _activeSegments.Clear();
        _activeSections.Clear();
        _adaptiveHpController.Reset(currentTime);
        _head = null;
        IsSpawned = false;
    }

    public void RebindFacePresentation()
    {
        if (IsSpawned)
            _faceBurstPresenter.Bind(_head?.FaceVisual);
    }

    public void UnbindFacePresentation()
    {
        _faceBurstPresenter.Unbind();
    }

    private List<WormSegment> CreateSegmentViews(
        out WormSegment head,
        out WormSegment tail)
    {
        List<WormPatternEntry> pattern =
            WormPatternBuilder.BuildPattern(_settings.SectionCount);

        return _wormFactory.CreateSegments(pattern, out head, out tail);
    }

    private List<WormSection> BuildSectionModels(
        List<WormSegment> segments,
        IReadOnlyList<CocoonRewardProfile> cocoonProfiles,
        float currentTime)
    {
        List<WormSection> sections =
            WormSectionBuilder.BuildSections(segments, cocoonProfiles);

        _adaptiveHpController.InitializeSections(sections, currentTime);
        return sections;
    }

    private void BindGameplayAndPresentation(
        List<WormSegment> segments,
        List<WormSection> sections,
        WormSegment head,
        WormSegment tail)
    {
        _wormFactory.AttachDamageReceivers(segments, _wormCombat);
        _wormController.Init(segments);
        _faceBurstPresenter.Bind(head.FaceVisual);
        _wormCombat.Init(head, tail, sections);
        _hpPresenter.BindSections(sections);
    }

    private void CommitSpawn(
        List<WormSegment> segments,
        List<WormSection> sections,
        WormSegment head)
    {
        _activeSegments.AddRange(segments);
        _activeSections.AddRange(sections);
        _head = head;
        IsSpawned = true;
    }

    private void RollbackFailedSpawn(
        List<WormSegment> rentedSegments,
        float currentTime)
    {
        UnbindGameplayAndPresentation();
        ReleaseSegments(rentedSegments);
        _adaptiveHpController.Reset(currentTime);
        _head = null;
        IsSpawned = false;
    }

    private void UnbindGameplayAndPresentation()
    {
        _hpPresenter.Clear();
        _wormCombat.Clear();
        _faceBurstPresenter.Unbind();
        _wormController.ClearWorm();
    }

    private void ReleaseSegments(IReadOnlyList<WormSegment> segments)
    {
        if (segments == null)
            return;

        for (int index = segments.Count - 1; index >= 0; index--)
            _segmentPool.Release(segments[index]);
    }
}
