using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core.Player;

namespace LightPat.StateMachineBehaviours
{
    public class UpdatePlayerLookBound : StateMachineBehaviour
    {
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
            
        //}

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (layerIndex == 0) // If we are the base layer
            {
                bool otherLayerIsPlaying = false;
                for (int i = 1; i < animator.layerCount; i++)
                {
                    if (animator.GetLayerWeight(i) > 0)
                    {
                        otherLayerIsPlaying = true;
                        break;
                    }
                }

                if (!otherLayerIsPlaying)
                {
                    if (animator.GetComponentInParent<PlayerController>())
                    {
                        animator.GetComponentInParent<PlayerController>().mouseUpXRotLimit = mouseUpXRotLimit;
                        animator.GetComponentInParent<PlayerController>().mouseDownXRotLimit = mouseDownXRotLimit;
                    }
                }
            }
            else if (animator.GetLayerWeight(layerIndex) == 1) // If we are any other layer
            {
                if (animator.GetComponentInParent<PlayerController>())
                {
                    animator.GetComponentInParent<PlayerController>().mouseUpXRotLimit = mouseUpXRotLimit;
                    animator.GetComponentInParent<PlayerController>().mouseDownXRotLimit = mouseDownXRotLimit;
                }
            }
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    
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
