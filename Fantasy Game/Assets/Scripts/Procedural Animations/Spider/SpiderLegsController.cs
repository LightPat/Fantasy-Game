using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using LightPat.Audio;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderLegsController : MonoBehaviour
    {
        public Transform rootBone;
        [HideInInspector] public SpiderPhysics physics;
        [Header("Animation Settings")]
        public float stepDistance;
        public float stepHeight;
        public float xAxisBodyRotationMultiplier;
        public float yAxisBodyRotationMultiplier;
        public float zAxisBodyRotationMultiplier;
        public float lerpSpeedMultiplier;
        public float angularLerpSpeedMultiplier;
        public float minimumLerpSpeed;

        public SpiderLegIKSolver[] legSet1 { get; private set; }
        public SpiderLegIKSolver[] legSet2 { get; private set; }

        [Header("Audio Settings")]
        public float footstepVolume = 1;
        public AudioClip[] footStepSounds;

        private RigBuilder rigBuilder;
        private float[] previousHeights;
        private float[] heightDifferences;

        public void PlayFootstep(SpiderLegIKSolver leg)
        {
            AudioManager.Singleton.PlayClipAtPoint(footStepSounds[Random.Range(0, footStepSounds.Length)], leg.transform.position, footstepVolume);
        }

        private void Start()
        {
            physics = GetComponentInParent<SpiderPhysics>();
            rigBuilder = GetComponent<RigBuilder>();
            legSet1 = new SpiderLegIKSolver[4];
            legSet2 = new SpiderLegIKSolver[4];
            previousHeights = new float[legSet1.Length + legSet2.Length];
            heightDifferences = new float[legSet1.Length + legSet2.Length];

            // Get references to each legIKSolver
            int counter = 0;
            foreach (RigLayer rigLayer in rigBuilder.layers)
            {
                int i = 0;
                foreach (IRigConstraint constraint in rigLayer.constraints)
                {
                    if (constraint.component.GetType() == typeof(TwoBoneIKConstraint))
                    {
                        TwoBoneIKConstraint twoBoneConstraint = (TwoBoneIKConstraint)constraint.component;

                        if (counter == 0)
                        {
                            legSet1[i] = twoBoneConstraint.data.target.GetComponent<SpiderLegIKSolver>();
                            legSet1[i].controller = this;
                            legSet1[i].permissionToMove = true;
                        }
                        else
                        {
                            legSet2[i] = twoBoneConstraint.data.target.GetComponent<SpiderLegIKSolver>();
                            legSet2[i].controller = this;
                            legSet2[i].permissionToMove = true;
                        }
                        i++;
                    }
                    else
                    {
                        Debug.LogWarning(constraint.component + " is not a TwoBoneIKConstraint, so it will be ignored");
                    }
                }
                counter++;
            }

            set1Moving = true;
            set2Moving = false;
        }

        private bool set1Moving;
        private bool set2Moving;
        private void Update()
        {
            // Switch legs that are moving
            if (set1Moving != legSet1[0].IsMoving())
            {
                foreach (SpiderLegIKSolver leg in legSet2)
                {
                    leg.permissionToMove = true;
                }
                foreach (SpiderLegIKSolver leg in legSet1)
                {
                    leg.permissionToMove = false;
                }
                set2Moving = true;
                set1Moving = false;
            }
            else if (set2Moving != legSet2[0].IsMoving())
            {
                foreach (SpiderLegIKSolver leg in legSet1)
                {
                    leg.permissionToMove = true;
                }
                foreach (SpiderLegIKSolver leg in legSet2)
                {
                    leg.permissionToMove = false;
                }
                set1Moving = true;
                set2Moving = false;
            }

            // Calcualate main body rotation depending on height of legs
            // Get average height difference of each leg between this frame and last frame
            // Then scale that difference to create the spider bodies rotation
            int heightIterator = 0;
            foreach (SpiderLegIKSolver leg in legSet1)
            {
                // Get height difference
                heightDifferences[heightIterator] = leg.transform.localPosition.y - previousHeights[heightIterator];
                // Assign new previous frame height
                previousHeights[heightIterator] = leg.transform.localPosition.y;
                heightIterator++;
            }

            foreach (SpiderLegIKSolver leg in legSet2)
            {
                heightDifferences[heightIterator] = leg.transform.localPosition.y - previousHeights[heightIterator];
                previousHeights[heightIterator] = leg.transform.localPosition.y;
                heightIterator++;
            }

            // If we are not airborne, and we are landing
            //if (!physics.airborne & !physics.landing)
            //{
            //    float average = heightDifferences.Average();
            //    transform.rotation = transform.parent.rotation * Quaternion.Euler(average * xAxisBodyRotationMultiplier, average * yAxisBodyRotationMultiplier, average * zAxisBodyRotationMultiplier);
            //}
        }
    }
}
