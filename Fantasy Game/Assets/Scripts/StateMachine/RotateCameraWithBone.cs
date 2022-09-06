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
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.GetComponentInParent<PlayerController>().disableLookInput = true;
            Camera.main.GetComponent<PlayerCameraFollow>().updateRotationWithTarget = true;
            Camera.main.GetComponent<PlayerCameraFollow>().aimRig.weightTarget = 0;
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{

        //}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.GetComponentInParent<PlayerController>().disableLookInput = false;
            Camera.main.GetComponent<PlayerCameraFollow>().updateRotationWithTarget = false;
            Camera.main.GetComponent<PlayerCameraFollow>().aimRig.weightTarget = 1;
            animator.GetComponentInParent<PlayerController>().rotationX = Camera.main.transform.eulerAngles.x;
            animator.GetComponentInParent<PlayerController>().rotationY = Camera.main.transform.eulerAngles.y;
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
