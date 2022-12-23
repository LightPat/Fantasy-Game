using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class PlaySoundOnCollision : MonoBehaviour
    {
        public AudioClip[] audioClip;
        public float volume = 1;

        private void OnCollisionEnter(Collision collision)
        {
            AudioManager.Singleton.PlayClipAtPoint(audioClip[Random.Range(0, audioClip.Length)], collision.GetContact(0).point, volume);
        }
    }
}