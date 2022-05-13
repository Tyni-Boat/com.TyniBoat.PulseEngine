using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{
    public class DebugNode : BaseBehaviourNode
    {
        public string _message;

        public override bool ExecutionDone()
        {
            return true;
        }

        public override void OnStateEnter()
        {
            Debug.Log($"{StateMachine} enter Debug Node : {_message}");
        }

        public override bool OnStateExit(BehaviourNodeState state, BaseTreeBehaviourData tree)
        {
            if (Children.Count > 0) tree.SetCurrentNodes(Children.ToArray()); else tree.ResetMachine();
            return true;
        }

        public override void OnStateUpdate(float deltaTime)
        {
        }
    }
}