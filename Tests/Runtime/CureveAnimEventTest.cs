using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PulseEngine;


[CreateAssetMenu(menuName = "Animation Curve Event", fileName = "AnimCurve")]
public class CureveAnimEventTest : AnimancerEvent
{
    public AnimationCurve curve;


    public override void Process(AnimancerMachine emitter, float delta, float normalizedTime)
    {
        if (curve == null || emitter == null)
            return;
        float value = curve.Evaluate(normalizedTime);
        emitter.MachineTimeScale = Mathf.Clamp(value, 0.01f, float.MaxValue);
    }

}
