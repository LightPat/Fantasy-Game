using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Helicopter : MonoBehaviour
    {
        public Transform passengerSeat;
        public Transform mainRotor;
        public Transform tailRotor;
        public float mainRotorSpeed;
        public float tailRotorSpeed;
        public bool engineStarted;

        float currentHelicopterSpeed;
        Animator animator;
        Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // Rotate rotors
            if (engineStarted)
                currentHelicopterSpeed = Mathf.MoveTowards(currentHelicopterSpeed, 1, Time.deltaTime / 4);
            else
                currentHelicopterSpeed = Mathf.MoveTowards(currentHelicopterSpeed, 0, Time.deltaTime / 4);
            mainRotor.Rotate(0, 0, mainRotorSpeed * Time.deltaTime * currentHelicopterSpeed, Space.Self);
            tailRotor.Rotate(tailRotorSpeed * Time.deltaTime * currentHelicopterSpeed, 0, 0, Space.Self);
        }
    }
}