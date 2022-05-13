using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{
    public class StateMachineExecutor : MonoBehaviour
    {
        public BaseTreeBehaviourData behaviourData;

        private BaseTreeBehaviourData _clonedBehaviourData;

        // Start is called before the first frame update
        void Start()
        {
            _clonedBehaviourData = Instantiate(behaviourData);
            //_clonedBehaviourData = ScriptableObject.CreateInstance(behaviourData.GetType()) as BaseTreeBehaviourData;
            if (_clonedBehaviourData != null)
            {
                _clonedBehaviourData.ResetMachine();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_clonedBehaviourData != null)
            {
                _clonedBehaviourData.Evaluate(this, Time.deltaTime);
            }
        }
    }
}