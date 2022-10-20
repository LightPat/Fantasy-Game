using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get { return _instance; } }
        private static List<AudioSource> audioSources = new List<AudioSource>();
        public GameObject audioSourcePrefab;
        public float initialVolume = 1;

        private static AudioManager _instance;

        public void RegisterAudioSource(AudioSource audioSource)
        {
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
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        private void Start()
        {
            foreach (AudioSource audioSouce in FindObjectsOfType<AudioSource>())
            {
                RegisterAudioSource(audioSouce);
            }
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