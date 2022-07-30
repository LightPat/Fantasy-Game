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
                        Debug.Log(constraint.component + " is not a TwoBoneIKConstraint, so it will be ignored");
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

        }
    }
}
