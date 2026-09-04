using System;

public sealed class WormFrameSimulation
{
    private readonly WormForwardMotionController _forwardMotion;
    private readonly WormPathProgressState _pathProgress;
    private readonly WormSegmentChainPresenter _segmentPresenter;
    private readonly WormReviveSequence _reviveSequence;
    private readonly WormSegmentChain<WormSegment> _segmentChain;
    private readonly WormSectionRollbackMotionController<WormSegment> _rollbackMotion;
    private readonly WormSectionRollbackState<WormSegment> _rollbackState;
    private RailPath _finalRenderRail;
    private WormSegmentChainLayout _finalRenderLayout;

    public WormFrameSimulation(
        WormForwardMotionController forwardMotion,
        WormPathProgressState pathProgress,
        WormSegmentChainPresenter segmentPresenter,
        WormReviveSequence reviveSequence,
        WormSegmentChain<WormSegment> segmentChain,
        WormSectionRollbackMotionController<WormSegment> rollbackMotion,
        WormSectionRollbackState<WormSegment> rollbackState)
    {
        _forwardMotion = forwardMotion ?? throw new ArgumentNullException(nameof(forwardMotion));
        _pathProgress = pathProgress ?? throw new ArgumentNullException(nameof(pathProgress));
        _segmentPresenter = segmentPresenter ?? throw new ArgumentNullException(nameof(segmentPresenter));
        _reviveSequence = reviveSequence ?? throw new ArgumentNullException(nameof(reviveSequence));
        _segmentChain = segmentChain ?? throw new ArgumentNullException(nameof(segmentChain));
        _rollbackMotion = rollbackMotion ?? throw new ArgumentNullException(nameof(rollbackMotion));
        _rollbackState = rollbackState ?? throw new ArgumentNullException(nameof(rollbackState));
    }

    public bool Tick(in WormFrameContext context)
    {
        if (_segmentChain.Count == 0 || context.Rail == null)
            return false;

        bool pathCompleted = RunMotionStage(context);
        Render(context.Rail, context.SegmentLayout);
        return pathCompleted;
    }

    public void Render(RailPath rail, in WormSegmentChainLayout layout)
    {
        _segmentPresenter.Render(
            _segmentChain.Segments,
            rail,
            _rollbackState.AnchoredDistances,
            layout);
    }

    private bool RunMotionStage(in WormFrameContext context)
    {
        if (_rollbackState.IsActive)
            return AdvanceRollback(context);

        if (_reviveSequence.IsActive)
        {
            AdvanceRevive(context);
            return false;
        }

        return AdvanceForward(context);
    }

    private bool AdvanceForward(in WormFrameContext context)
    {
        WormForwardMotionResult result = _forwardMotion.Advance(
            _pathProgress.HeadDistance,
            context.DeltaTime,
            context.Rail,
            context.ForwardMotion);
        return _pathProgress.Apply(result);
    }

    private bool AdvanceRollback(in WormFrameContext context)
    {
        WormSectionRollbackMotionResult result = _rollbackMotion.Advance(
            _pathProgress.HeadDistance,
            _segmentChain.Segments,
            context.Rail.TotalLength,
            context.BaseSpeed,
            context.RollbackForwardSpeedMultiplier,
            context.RollbackSpeed,
            context.UnscaledDeltaTime);
        _pathProgress.SetHeadDistance(result.HeadDistance);

        if (!result.Completed)
            return false;

        bool pathCompleted = _pathProgress.TryComplete(
            _pathProgress.HeadDistance >= context.Rail.TotalLength);
        _rollbackState.Complete();
        return pathCompleted;
    }

    private void AdvanceRevive(in WormFrameContext context)
    {
        WormReviveAnimationFrame frame = _reviveSequence.Advance(
            context.UnscaledDeltaTime);
        _pathProgress.SetHeadDistance(frame.HeadDistance);

        if (!frame.Completed)
            return;

        _finalRenderRail = context.Rail;
        _finalRenderLayout = context.SegmentLayout;
        _reviveSequence.CompleteAfterFinalRender(RenderFinalReviveFrame);
    }

    private void RenderFinalReviveFrame()
    {
        Render(_finalRenderRail, _finalRenderLayout);
        _finalRenderRail = null;
        _finalRenderLayout = default;
    }
}
