using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LightPat.Audio;

namespace LightPat.Core
{
    public class AnimationAudioLibrary : MonoBehaviour
    {
        public AudioClip[] audioClips;
        public string[] clipNames;
        public float[] volumes;

        private void Start()
        {
            if (audioClips.Length != clipNames.Length)
                Debug.LogError("Audio clip array and clip name array lengths are not equal");
            if (audioClips.Length != volumes.Length)
                Debug.LogError("Audio clip array and volume array lengths are not equal");
        }

        private void PlayClip(string clipName)
        {
            int index = Array.IndexOf(clipNames, clipName);
            AudioManager.Singleton.PlayClipAtPoint(audioClips[index], transform.position, volumes[index]);
        }
    }
}