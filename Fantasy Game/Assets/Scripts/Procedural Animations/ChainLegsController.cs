using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class ChainLegsController : MonoBehaviour
    {
        public Transform rootBone;
        public Rig legsRig;

        [Header("Leg Groupings to Move")]
        public int[] moveSet1;
        public int[] moveSet2;

        [Header("Animation Settings")]
        public float footSpacing = 0.2f;
        public float stepDistance = 0.7f;
        public float lerpSpeed = 5;
        public float stepHeight = 0.5f;

        private List<Transform> legSet1;
        private List<Transform> legSet2;

        void Start()
        {
            int counter = 0;
            foreach (Transform IK in legsRig.transform)
            {
                if (moveSet1.Contains(counter))
                {
                    legSet1.Add(IK.Find("Target"));
                }
                else if (moveSet2.Contains(counter))
                {
                    legSet2.Add(IK.Find("Target"));
                }
                counter++;
            }
        }

        void Update()
        {

        }
    }
}
