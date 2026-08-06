
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] internal AudioSource audioPlayer_wl;
    [SerializeField] internal AudioSource audioPlayer_button;
    [SerializeField] internal AudioSource audioBet_button;
    [SerializeField] internal AudioSource audioWin;


    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioClip[] Voiceclips;

    private void Start()
    {
        if (bg_adudio) bg_adudio.Play();
        //  audioPlayer_button.clip = clips[0];
        audioBet_button.clip = clips[3];
        audioWin.clip = clips[4];

    }


    internal void PlayWLAudio(string type)
    {
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "betDone":
                index = 0;
                // audioPlayer_wl.loop = true;
                break;
            case "amount":
                index = 1;
                break;
            case "openChip":
                index = 2;
                break;
            case "extraPay":
                index = 3;
                break;
            case "betNow":
                index = 4;
                break;
            case "pop":
                index = 5;
                break;
            case "shakingDice":
                index = 6;
                break;
            case "singleChip":
                index = 7;
                break;

        }
        StopWLAaudio();
        audioPlayer_wl.clip = clips[index];
        audioPlayer_wl.Play();

    }


    internal void PlayButtonAudio()
    {
        audioPlayer_button.Play();
    }

    internal void PlayBetButtonAudio()
    {
        audioBet_button.Play();
    }

    internal void PlayGirlAudio(string type)
    {
        // audioWin.Play();
        audioWin.loop = false;
        int index = 0;
        switch (type)
        {
            case "timeisrunning":
                index = 0;
                audioPlayer_wl.loop = true;
                break;
            case "betSelect":
                index = 1;
                break;
            case "placeyourbet":
                index = 2;
                break;
            case "nomorebets":
                index = 3;
                break;
            case "newround":
                index = 4;
                break;
            case "cards":
                index = 5;
                break;
            case "midCard":
                index = 6;
                break;

        }
        StopWLAaudio();
        audioPlayer_wl.clip = Voiceclips[index];
        audioPlayer_wl.Play();
    }



    internal void StopWLAaudio()
    {
        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;
    }


    internal void StopBgAudio()
    {
        bg_adudio.Stop();
    }

    private bool isForceMuted = false;
    private bool preFocusUserMuted = false;
    private bool userMuted = false;

    // Focus-driven — called from BOTH OnFocusChanged and OnApplicationFocus
    internal void SetMuteAll(bool forceMute)
    {
        if (forceMute == isForceMuted) return;
        isForceMuted = forceMute;

        if (forceMute)
        {
            preFocusUserMuted = userMuted;
            MuteAllSources(true);
        }
        else
        {
            MuteAllSources(preFocusUserMuted);
        }
    }

    // User-toggle-driven — sound/music button callbacks
    internal void SetUserMute(bool mute)
    {
        userMuted = mute;
        if (!isForceMuted)
        {
            MuteAllSources(userMuted);
        }
    }

    private void MuteAllSources(bool mute)
    {
        if (bg_adudio) bg_adudio.mute = mute;
        if (audioPlayer_wl) audioPlayer_wl.mute = mute;
        if (audioPlayer_button) audioPlayer_button.mute = mute;
        if (audioBet_button) audioBet_button.mute = mute;
        if (audioWin) audioWin.mute = mute;
    }

    private void OnApplicationFocus(bool focus)
    {
        SetMuteAll(!focus);
    }

    internal void ToggleMute(bool toggle, string type = "all")
    {
        switch (type)
        {
            case "bg":
                if (bg_adudio) bg_adudio.mute = toggle;
                break;
            case "button":
                if (audioPlayer_button) audioPlayer_button.mute = toggle;
                break;
            case "wl":
                if (audioPlayer_wl) audioPlayer_wl.mute = toggle;
                break;
            case "win":
                if (audioWin) audioWin.mute = toggle;
                break;
            case "bet":
                if (audioBet_button) audioBet_button.mute = toggle;
                break;
            case "all":
                SetUserMute(toggle);
                break;
        }
    }

}