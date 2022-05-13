using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    public class WaitNode : BaseBehaviourNode
    {
        public float _waitTime;
        private float _chrono;


        public override bool ExecutionDone()
        {
            return _chrono <= 0;
        }

        public override void OnStateEnter()
        {
            _chrono = _waitTime;
            Debug.Log($"Waiting {_chrono} sec");
        }

        public override bool OnStateExit(BehaviourNodeState state, BaseTreeBehaviourData tree)
        {
            if (Children.Count > 0) tree.SetCurrentNodes(Children.ToArray()); else tree.ResetMachine();
            return true;
        }

        public override void OnStateUpdate(float deltaTime)
        {
            _chrono -= deltaTime;
        }
    }
}