using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core.Player;

namespace LightPat.StateMachine
{
    public class RotateCameraWithBone : StateMachineBehaviour
    {
        public bool deactivateWeaponLayers;

        PlayerController playerController;
        bool reached;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            reached = false;
            if (animator.GetLayerWeight(layerIndex) != 1 & layerIndex != 0) { return; }

            playerController = animator.GetComponentInParent<PlayerController>();

            if (playerController)
            {
                playerController.playerCamera.deactivateWeaponLayers = deactivateWeaponLayers;
                playerController.playerCamera.updateRotationWithTarget = true;
                reached = true;
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //if (animator.GetLayerWeight(layerIndex) != 1) { return; }
            if (!reached) { return; }

            playerController.playerCamera.deactivateWeaponLayers = false;
            playerController.playerCamera.updateRotationWithTarget = false;
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
