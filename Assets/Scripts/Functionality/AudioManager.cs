using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
  [Header("Sources")]
  [FormerlySerializedAs("bg_adudio")]
  [SerializeField] private AudioSource musicSource;
  [FormerlySerializedAs("audioPlayer_wl")]
  [SerializeField] private AudioSource sfxSource;
  // Its own source so the fade never touches the other effects
  [FormerlySerializedAs("audioPlayer_button")]
  [SerializeField] private AudioSource diceSource;

  [Header("Clips")]
  [SerializeField] private AudioClip bgMusic;
  [SerializeField] private AudioClip placeBetsNow;
  [SerializeField] private AudioClip betLocked;
  [SerializeField] private AudioClip extraPay;
  [SerializeField] private AudioClip diceShaking;
  [SerializeField] private AudioClip button;
  [SerializeField] private AudioClip chipSelector;
  [SerializeField] private AudioClip chipPlace;
  [SerializeField] private AudioClip coinWin;

  [Header("Dice")]
  // Seconds of fade at the end of the roll; the clip is longer than the animation
  [SerializeField] private float diceFadeOut = 0.6f;

  private Tween diceFade;

  private void Start()
  {
    if (!musicSource || !bgMusic) return;
    musicSource.clip = bgMusic;
    musicSource.loop = true;
    musicSource.Play();
  }

  internal void PlayButton() => PlayOneShot(button);
  internal void PlayChipSelector() => PlayOneShot(chipSelector);
  internal void PlayChipPlace() => PlayOneShot(chipPlace);
  internal void PlayBetNow() => PlayOneShot(placeBetsNow);
  internal void PlayBetLocked() => PlayOneShot(betLocked);
  internal void PlayExtraPay() => PlayOneShot(extraPay);
  internal void PlayWin() => PlayOneShot(coinWin);

  // One-shots overlap, so a click never cuts off a banner voice
  private void PlayOneShot(AudioClip clip)
  {
    if (sfxSource && clip) sfxSource.PlayOneShot(clip);
  }

  // Full volume from the start, silent by the time the roll ends
  internal void PlayDiceRoll(float rollDuration)
  {
    if (!diceSource || !diceShaking) return;

    diceFade?.Kill();
    diceSource.Stop();
    diceSource.clip = diceShaking;
    diceSource.loop = false;
    diceSource.volume = 1f;
    diceSource.Play();

    float fade = Mathf.Min(diceFadeOut, rollDuration);
    diceFade = diceSource.DOFade(0f, fade)
        .SetDelay(Mathf.Max(0f, rollDuration - fade))
        .OnComplete(diceSource.Stop);
  }

  private bool isForceMuted = false;
  private bool soundMuted = false;
  private bool musicMuted = false;

  // Focus-driven — called from BOTH OnFocusChanged and OnApplicationFocus
  internal void SetMuteAll(bool forceMute)
  {
    if (forceMute == isForceMuted) return;
    isForceMuted = forceMute;
    ApplyMutes();
  }

  internal void ToggleSoundMute(bool mute)
  {
    soundMuted = mute;
    ApplyUserMute();
  }

  internal void ToggleMusicMute(bool mute)
  {
    musicMuted = mute;
    ApplyUserMute();
  }

  // A toggle can only be pressed while focused, so it also clears a focus mute that got stuck
  private void ApplyUserMute()
  {
    isForceMuted = false;
    ApplyMutes();
  }

  private void ApplyMutes()
  {
    bool music = isForceMuted || musicMuted;
    bool sound = isForceMuted || soundMuted;

    if (musicSource) musicSource.mute = music;
    if (sfxSource) sfxSource.mute = sound;
    if (diceSource) diceSource.mute = sound;
  }

  // Separate from the mute flags so focus restore and the user's sound setting are untouched
  internal void SetStartupSilence(bool silent)
  {
    AudioListener.volume = silent ? 0f : 1f;
  }

  private void OnApplicationFocus(bool focus)
  {
    SetMuteAll(!focus);
  }
}
