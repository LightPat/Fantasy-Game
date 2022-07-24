using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class SpiderLegsController : MonoBehaviour
    {
        public Transform rootBone;

        [Header("Animation Settings")]
        public float stepDistance;
        public float lerpSpeed;
        public float stepHeight;

        private RigBuilder rigBuilder;
        private SpiderLegIKSolver[] legSet1;
        private SpiderLegIKSolver[] legSet2;

        private void Start()
        {
            rigBuilder = GetComponent<RigBuilder>();
            legSet1 = new SpiderLegIKSolver[4];
            legSet2 = new SpiderLegIKSolver[4];

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
                        }
                        else
                        {
                            legSet2[i] = twoBoneConstraint.data.target.GetComponent<SpiderLegIKSolver>();
                        }
                        i++;
                    }
                    else
                    {
                        Debug.Log(constraint.component + " is not a TwoBoneIKConstraint, so it will be ignored");
                    }
                }
                counter++;
            }

            // Apply animation settings to both leg sets, and set legset1 to be able to move
            foreach (SpiderLegIKSolver leg in legSet1)
            {
                leg.controller = this;
                leg.rootBone = rootBone;
                leg.stepDistance = stepDistance;
                leg.lerpSpeed = lerpSpeed;
                leg.stepHeight = stepHeight;
                leg.permissionToMove = true;
            }
            set1Moving = true;

            foreach (SpiderLegIKSolver leg in legSet2)
            {
                leg.controller = this;
                leg.rootBone = rootBone;
                leg.stepDistance = stepDistance;
                leg.lerpSpeed = lerpSpeed;
                leg.stepHeight = stepHeight;
                leg.permissionToMove = false;
            }
            set2Moving = false;
        }

        [HideInInspector]public bool switchTrigger;
        private bool set1Moving;
        private bool set2Moving;

        private void Update()
        {
            if (switchTrigger)
            {
                if (set1Moving)
                {
                    set2Moving = true;
                    set1Moving = false;
                    foreach (SpiderLegIKSolver leg in legSet2)
                    {
                        leg.permissionToMove = true;
                    }
                    foreach (SpiderLegIKSolver leg in legSet1)
                    {
                        leg.permissionToMove = false;
                    }
                }
                else if (set2Moving)
                {
                    set1Moving = true;
                    set2Moving = false;
                    foreach (SpiderLegIKSolver leg in legSet1)
                    {
                        leg.permissionToMove = true;
                    }
                    foreach (SpiderLegIKSolver leg in legSet2)
                    {
                        leg.permissionToMove = false;
                    }
                }
                switchTrigger = false;
            }
        }
    }
}
