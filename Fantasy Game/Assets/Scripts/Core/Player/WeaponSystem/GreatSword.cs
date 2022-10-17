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

        public override void Attack1(bool pressed)
        {
            swinging = pressed;
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Great Sword";
        }

        private void OnTransformParentChanged()
        {
            if (GetComponentInParent<HumanoidWeaponAnimationHandler>())
                GetComponentInParent<Animator>().SetFloat("swingSpeed", swingSpeed);
        }
    }
}