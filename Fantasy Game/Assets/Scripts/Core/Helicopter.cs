using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Helicopter : MonoBehaviour
    {
        public Transform passengerSeat;

        private void Update()
        {
            if (passengerSeat.childCount > 0)
                passengerSeat.GetChild(0).localPosition = Vector3.zero;
            //passengerSeat.GetChild(0).localPosition = Vector3.Lerp(passengerSeat.GetChild(0).localPosition, Vector3.zero, 5 * Time.deltaTime);
        }
    }
}