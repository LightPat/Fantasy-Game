using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.UI
{
    public class WorldSpaceInfoLabel : MonoBehaviour
    {
        public TextMeshPro nameDisplay;

        public float rotationSpeed = 0.4f;
        public float animationSpeed = 0.02f;
        public float viewDistance = 10f;
        Vector3 positionOffset;
        Transform target;

        private void Start()
        {
            nameDisplay.SetText(transform.parent.name);
            target = transform.parent;
            positionOffset = transform.localPosition;
            transform.SetParent(null, true);
            name = name + " (" + target.name + ")";
        }

        private void LateUpdate()
        {
            if (!Camera.main) { return; }

            if (target == null)
                Destroy(gameObject);

            gameObject.SetActive(target.gameObject.activeInHierarchy);

            transform.position = target.position + positionOffset;

            Quaternion rotTarget = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotTarget, rotationSpeed);

            if (Vector3.Distance(Camera.main.transform.position, transform.position) > viewDistance)
            {
                // Might have to add time.DeltaTime to this instead of just using animationSpeed
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, animationSpeed);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, animationSpeed);
            }
        }
    }
}
