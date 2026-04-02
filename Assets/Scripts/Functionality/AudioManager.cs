
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
                audioPlayer_wl.loop = true;
                break;
            case "betSelect":
                index = 1;
                break;
            case "numberchange":
                index = 2;
                break;
            case "coinSelect":
                index = 3;
                break;
            case "double":
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

    internal void ToggleMute(bool toggle, string type = "all")
    {
        switch (type)
        {
            case "bg":
                bg_adudio.mute = toggle;
                break;
            case "button":
                audioPlayer_button.mute = toggle;
                break;
            case "wl":
                audioPlayer_wl.mute = toggle;
                break;
            case "win":
                audioWin.mute = toggle;
                break;
            case "bet":
                audioBet_button.mute = toggle;
                break;
            case "all":
                audioPlayer_wl.mute = toggle;
                bg_adudio.mute = toggle;
                audioPlayer_button.mute = toggle;
                break;
        }
    }

}