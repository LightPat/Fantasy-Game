using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class GreatSword : Weapon
    {
        [Header("Great Sword Specific")]
        public Transform leftFingersGrips;
        public float swingSpeed = 1;
        public bool swinging { get; private set; }

        Animator playerAnimator;
        Coroutine swingRoutine;

        public override void Attack1(bool pressed)
        {
            if (swingRoutine != null)
                StopCoroutine(swingRoutine);
            if (pressed)
                swingRoutine = StartCoroutine(Swing());
            else
                swinging = false;
        }

        private IEnumerator Swing()
        {
            yield return new WaitUntil(() => playerAnimator.IsInTransition(playerAnimator.GetLayerIndex("Great Sword")));
            yield return new WaitForSeconds(playerAnimator.GetNextAnimatorClipInfo(playerAnimator.GetLayerIndex("Great Sword"))[0].clip.length * 0.2f);
            swinging = true;
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Great Sword";
        }

        private void OnTransformParentChanged()
        {
            if (GetComponentInParent<HumanoidWeaponAnimationHandler>())
            {
                playerAnimator = GetComponentInParent<Animator>();
                playerAnimator.SetFloat("swingSpeed", swingSpeed);
            }
        }
    }
}