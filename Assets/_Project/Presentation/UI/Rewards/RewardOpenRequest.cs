public readonly struct RewardOpenRequest
{
    public RewardOpenRequest(
        CocoonRewardProfile cocoonProfile,
        RewardRollContext rollContext)
    {
        CocoonProfile = cocoonProfile;
        RollContext = rollContext;
    }

    public CocoonRewardProfile CocoonProfile { get; }
    public RewardRollContext RollContext { get; }
}
