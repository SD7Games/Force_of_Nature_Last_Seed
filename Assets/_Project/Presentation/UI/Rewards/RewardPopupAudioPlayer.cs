using UnityEngine;

public sealed class RewardPopupAudioPlayer
{
    private readonly AudioSource _source;
    private readonly AudioClip _showWhooshClip;
    private readonly AudioClip _showSettleClip;
    private readonly AudioClip _refreshClip;
    private readonly AudioClip _cardRevealClip;
    private readonly float _volume;

    public RewardPopupAudioPlayer(
        AudioSource source,
        AudioClip showWhooshClip,
        AudioClip showSettleClip,
        AudioClip refreshClip,
        AudioClip cardRevealClip,
        float volume)
    {
        _source = source;
        _showWhooshClip = showWhooshClip;
        _showSettleClip = showSettleClip;
        _refreshClip = refreshClip;
        _cardRevealClip = cardRevealClip;
        _volume = volume;
    }

    public void PlayShowWhoosh() => Play(_showWhooshClip);
    public void PlayShowSettle() => Play(_showSettleClip);
    public void PlayRefresh() => Play(_refreshClip);
    public void PlayCardReveal() => Play(_cardRevealClip);

    private void Play(AudioClip clip)
    {
        if (_source == null || clip == null)
            return;

        _source.PlayOneShot(clip, _volume);
    }
}
