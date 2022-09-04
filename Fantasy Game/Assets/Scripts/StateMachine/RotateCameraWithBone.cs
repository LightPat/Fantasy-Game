using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using LightPat.Core.Player;
using LightPat.Util;

namespace LightPat.StateMachine
{
    public class RotateCameraWithBone : StateMachineBehaviour
    {
        public float aimWeightTarget;
        public bool updateCameraRotation;

        private RigBuilder rigBuilder;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animator.GetComponent<RigBuilder>()) { return; }
            rigBuilder = animator.GetComponent<RigBuilder>();
            foreach (RigLayer rigLayer in rigBuilder.layers)
            {
                if (rigLayer.name == "AimRig")
                {
                    rigLayer.rig.GetComponent<RigWeightTarget>().weightTarget = aimWeightTarget;
                    Camera.main.GetComponent<PlayerCameraFollow>().updateRotationWithTarget = updateCameraRotation;
                    animator.GetComponentInParent<PlayerController>().disableLookInput = updateCameraRotation;
                    animator.GetComponentInParent<PlayerController>().disableCameraLookInput = updateCameraRotation;
                    break;
                }
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //}

        // OnStateMove is called right after Animator.OnAnimatorMove()
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that processes and affects root motion
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK()
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that sets up animation IK (inverse kinematics)
        //}
    }
}