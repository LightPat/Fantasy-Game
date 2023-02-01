using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using LightPat.ProceduralAnimations;

namespace LightPat.Core.Player
{
    public class GreatSword : Weapon
    {
        [Header("Great Sword Specific")]
        public Transform leftFingersGrips;
        public float swingSpeed = 1;
        public bool swinging;
        public float blockSpeed = 10;

        public Collider normalCollider;
        public Collider trigger;

        HumanoidWeaponAnimationHandler playerWeaponAnimationHandler;
        Animator playerAnimator;
        Attributes playerAttributes;
        PlayerController playerController;
        Coroutine swingRoutine;
        bool attack1;

        public void StopSwing()
        {
            StopCoroutine(swingRoutine);
            StartCoroutine(ResetSwing());
        }

        public override NetworkObject Attack1(bool pressed)
        {
            if (pressed == attack1) { return null; }

            attack1 = pressed;
            if (swingRoutine != null)
                StopCoroutine(swingRoutine);
            if (pressed)
                swingRoutine = StartCoroutine(Swing());
            else
            {
                StartCoroutine(ResetColliders());
                swinging = false;
            }
            return null;
        }

        float oldRigSpeed;
        int oldCullingMask;
        bool attack2;
        public override void Attack2(bool pressed)
        {
            if (pressed == attack2) { return; }
            attack2 = pressed;

            playerAttributes.blocking = pressed;

            if (pressed)
            {
                playerWeaponAnimationHandler.rightHandTarget.target = playerWeaponAnimationHandler.blockConstraints;
                playerWeaponAnimationHandler.rightArmRig.weightTarget = 1;
                playerWeaponAnimationHandler.spineAimRig.weightTarget = 1;
                oldRigSpeed = playerWeaponAnimationHandler.rightArmRig.weightSpeed;
                playerWeaponAnimationHandler.rightArmRig.weightSpeed = blockSpeed;
                playerWeaponAnimationHandler.spineAimRig.weightSpeed = blockSpeed;
                playerWeaponAnimationHandler.blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();

                if (playerAnimator.GetFloat("lookAngle") < 0)
                    playerAnimator.SetBool("mirrorIdle", true);
                else
                    playerAnimator.SetBool("mirrorIdle", false);

                if (playerController)
                {
                    Camera playerCamera = playerController.playerCamera.GetComponent<Camera>();
                    oldCullingMask = playerCamera.cullingMask;
                    playerCamera.cullingMask = -1;
                }
            }
            else
            {
                playerWeaponAnimationHandler.spineAimRig.weightTarget = 0;
                playerWeaponAnimationHandler.rightArmRig.weightTarget = 0;
                playerWeaponAnimationHandler.spineAimRig.weightSpeed = oldRigSpeed;
                StartCoroutine(ChangeFollowTargetAfterWeightTargetReached(playerWeaponAnimationHandler.rightHandTarget, playerWeaponAnimationHandler.rightHandIK.data.tip, playerWeaponAnimationHandler.rightArmRig, oldRigSpeed));
                
                playerWeaponAnimationHandler.blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();
                if (playerController)
                    playerController.playerCamera.GetComponent<Camera>().cullingMask = oldCullingMask;
            }
        }

        private IEnumerator ChangeFollowTargetAfterWeightTargetReached(FollowTarget followTarget, Transform newTarget, RigWeightTarget rig, float originalSpeed)
        {
            yield return new WaitUntil(() => rig.GetRig().weight == 0);
            followTarget.target = newTarget;
            rig.weightSpeed = originalSpeed;
        }

        private IEnumerator ResetSwing()
        {
            swinging = false;
            playerAnimator.SetBool("attack1", false);
            yield return null;
            playerAnimator.SetBool("attack1", attack1);
            yield return ResetColliders();
            Attack1(attack1);
        }

        private IEnumerator ResetColliders()
        {
            yield return new WaitUntil(() => playerAnimator.GetCurrentAnimatorStateInfo(playerAnimator.GetLayerIndex("Great Sword")).IsName("Idle"));
            foreach (Collider c in normalCollider.GetComponentsInChildren<Collider>())
            {
                c.enabled = true;
            }
            trigger.enabled = false;
            playerWeaponAnimationHandler.ClearSwingHits();
        }

        private IEnumerator Swing()
        {
            yield return new WaitUntil(() => playerAnimator.IsInTransition(playerAnimator.GetLayerIndex("Great Sword")));
            yield return new WaitForSeconds(playerAnimator.GetNextAnimatorClipInfo(playerAnimator.GetLayerIndex("Great Sword"))[0].clip.length * 0.2f);
            swinging = true;
            foreach (Collider c in normalCollider.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            trigger.enabled = true;
            yield return new WaitUntil(() => playerAnimator.GetCurrentAnimatorStateInfo(playerAnimator.GetLayerIndex("Great Sword")).IsName("Idle"));
            playerWeaponAnimationHandler.ClearSwingHits();
            if (attack1)
                swingRoutine = StartCoroutine(Swing());
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Great Sword";
        }

        private void OnTransformParentChanged()
        {
            playerWeaponAnimationHandler = GetComponentInParent<HumanoidWeaponAnimationHandler>();
            if (playerWeaponAnimationHandler)
            {
                playerController = playerWeaponAnimationHandler.GetComponent<PlayerController>();
                playerAttributes = playerWeaponAnimationHandler.GetComponent<Attributes>();
                playerAnimator = playerWeaponAnimationHandler.GetComponentInChildren<Animator>();
                playerAnimator.SetFloat("swingSpeed", swingSpeed);
            }
        }

        [SerializeField]
        [Tooltip("The empty game object located at the tip of the blade")]
        private GameObject _tip = null;

        [SerializeField]
        [Tooltip("The empty game object located at the base of the blade")]
        private GameObject _base = null;
        
        [SerializeField]
        [Tooltip("The amount of force applied to each side of a slice")]
        private float _forceAppliedToCut = 3f;

        public float sliceDelay = 2;

        private float lastSliceTime;
        bool slicing;
        private Vector3 _triggerEnterTipPosition;
        private Vector3 _triggerEnterBasePosition;
        private Vector3 _triggerExitTipPosition;

        public void SliceStart()
        {
            if (Time.time - lastSliceTime < sliceDelay) { return; }
            if (slicing) { return; }
            slicing = true;
            _triggerEnterTipPosition = _tip.transform.position;
            _triggerEnterBasePosition = _base.transform.position;
        }

        public void SliceEnd(Collision collision)
        {
            if (Time.time - lastSliceTime < sliceDelay) { return; }
            lastSliceTime = Time.time;
            _triggerExitTipPosition = _tip.transform.position;

            //Create a triangle between the tip and base so that we can get the normal
            Vector3 side1 = _triggerExitTipPosition - _triggerEnterTipPosition;
            Vector3 side2 = _triggerExitTipPosition - _triggerEnterBasePosition;

            //Get the point perpendicular to the triangle above which is the normal
            //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            //Transform the normal so that it is aligned with the object we are slicing's transform.
            Vector3 transformedNormal = ((Vector3)(collision.collider.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;

            //Get the enter position relative to the object we're cutting's local transform
            Vector3 transformedStartingPoint = collision.collider.gameObject.transform.InverseTransformPoint(_triggerEnterTipPosition);

            Plane plane = new Plane();

            plane.SetNormalAndPosition(
                    transformedNormal,
                    transformedStartingPoint);

            var direction = Vector3.Dot(Vector3.up, transformedNormal);

            //Flip the plane so that we always know which side the positive mesh is on
            if (direction < 0)
            {
                plane = plane.flipped;
            }

            GameObject[] slices = Slicer.Slice(plane, collision.collider.gameObject);
            Destroy(collision.collider.gameObject);

            Rigidbody rigidbody = slices[1].GetComponent<Rigidbody>();
            Vector3 newNormal = transformedNormal + Vector3.up * _forceAppliedToCut;
            rigidbody.AddForce(newNormal, ForceMode.Impulse);
            slicing = false;
        }

        public void SliceEnd(Collider other)
        {
            if (Time.time - lastSliceTime < sliceDelay) { return; }
            lastSliceTime = Time.time;
            _triggerExitTipPosition = _tip.transform.position;

            //Create a triangle between the tip and base so that we can get the normal
            Vector3 side1 = _triggerExitTipPosition - _triggerEnterTipPosition;
            Vector3 side2 = _triggerExitTipPosition - _triggerEnterBasePosition;

            //Get the point perpendicular to the triangle above which is the normal
            //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
            Vector3 normal = Vector3.Cross(side1, side2).normalized;

            //Transform the normal so that it is aligned with the object we are slicing's transform.
            Vector3 transformedNormal = ((Vector3)(other.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;

            //Get the enter position relative to the object we're cutting's local transform
            Vector3 transformedStartingPoint = other.gameObject.transform.InverseTransformPoint(_triggerEnterTipPosition);

            Plane plane = new Plane();

            plane.SetNormalAndPosition(
                    transformedNormal,
                    transformedStartingPoint);

            var direction = Vector3.Dot(Vector3.up, transformedNormal);

            //Flip the plane so that we always know which side the positive mesh is on
            if (direction < 0)
            {
                plane = plane.flipped;
            }

            GameObject[] slices = Slicer.Slice(plane, other.gameObject);
            Destroy(other.gameObject);

            Rigidbody rigidbody = slices[1].GetComponent<Rigidbody>();
            Vector3 newNormal = transformedNormal + Vector3.up * _forceAppliedToCut;
            rigidbody.AddForce(newNormal, ForceMode.Impulse);
            slicing = false;
        }
    }
}