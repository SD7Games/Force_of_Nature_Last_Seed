public interface IWormCocoonShakeClock
{
    float RotationOffset { get; }

    void Register(float interval, float angle);
    void Unregister();
}
