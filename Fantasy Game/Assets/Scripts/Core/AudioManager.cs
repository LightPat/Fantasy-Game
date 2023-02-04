using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class AudioManager : MonoBehaviour
    {
        public GameObject audioSourcePrefab;
        public float initialVolume = 1;

        public AudioClip[] networkAudioClips;

        private static List<AudioSource> audioSources = new List<AudioSource>();
        private static AudioManager _singleton;

        public static AudioManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Debug.Log("Audio Manager is Null");
                }

                return _singleton;
            }
        }

        public void RegisterAudioSource(AudioSource audioSource)
        {
            audioSource.spatialBlend = 1;
            audioSource.minDistance = 5;
            audioSources.Add(audioSource);
        }

        public void PlayClipAtPoint(AudioClip audioClip, Vector3 position, float volume = 1)
        {
            if (!audioClip) { return; }

            GameObject g = Instantiate(audioSourcePrefab, position, Quaternion.identity);
            StartCoroutine(PlayPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator PlayPrefab(AudioSource audioSouce, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSouce);
            audioSouce.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSouce.isPlaying);
            Destroy(audioSouce.gameObject);
        }

        private void Awake()
        {
            _singleton = this;
        }

        private void Start()
        {
            foreach (AudioSource audioSouce in FindObjectsOfType<AudioSource>())
            {
                RegisterAudioSource(audioSouce);
            }
            if (NetworkManager.Singleton.IsClient)
                AudioListener.volume = 0;
            else
                AudioListener.volume = initialVolume;
        }

        private void Update()
        {
            audioSources.RemoveAll(item => item == null);
            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.pitch = Time.timeScale;
            }
        }
    }
}