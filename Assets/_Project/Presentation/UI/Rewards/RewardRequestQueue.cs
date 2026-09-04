using System.Collections.Generic;

public sealed class RewardRequestQueue
{
    private readonly Queue<RewardOpenRequest> _requests = new();

    public int Count => _requests.Count;

    public void Enqueue(in RewardOpenRequest request)
    {
        _requests.Enqueue(request);
    }

    public bool TryDequeue(out RewardOpenRequest request)
    {
        return _requests.TryDequeue(out request);
    }

    public void Clear()
    {
        _requests.Clear();
    }
}
