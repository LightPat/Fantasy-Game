using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using LightPat.Core.Player;
using LightPat.ProceduralAnimations;

namespace LightPat.StateMachine
{
    public class RotateBodyButNotCamera : StateMachineBehaviour
    {
        PlayerController playerController;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            playerController = animator.GetComponentInParent<PlayerController>();
            playerController.playerCamera.updateRotationWithTarget = true;
            playerController.disableCameraLookInput = true;
            playerController.playerCamera.neckAimRig.weightTarget = 0;
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            playerController.playerCamera.updateRotationWithTarget = false;
            playerController.disableCameraLookInput = false;
            playerController.playerCamera.neckAimRig.weightTarget = 1;
        }

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