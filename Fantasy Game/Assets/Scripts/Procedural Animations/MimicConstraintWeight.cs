using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MimicConstraintWeight : MonoBehaviour
{
    public TwoBoneIKConstraint masterConstraint;
    public TwoBoneIKConstraint[] slaveTwoBoneConstraints;
    public MultiAimConstraint[] slaveAimConstraints;

    private void Update()
    {
        foreach (IRigConstraint constraint in slaveTwoBoneConstraints)
        {
            constraint.weight = masterConstraint.weight;
        }

        foreach (IRigConstraint constraint in slaveAimConstraints)
        {
            constraint.weight = masterConstraint.weight;
        }
    }
}
