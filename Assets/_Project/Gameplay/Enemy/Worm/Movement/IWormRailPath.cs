public interface IWormRailPath
{
    float TotalLength { get; }

    bool TryGetControlPointDistance(int pointIndex, out float distance);
}
