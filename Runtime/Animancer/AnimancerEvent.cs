using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.Animancer
{
    /// <summary>
    /// Base class for scriptable events
    /// </summary>
    public abstract class AnimancerEvent : ScriptableObject
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        protected int _executionCount;

        private bool _isExecuting;
        private bool _hadExecutedOnce;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        [field: SerializeField] public bool IsOneTimeAction { get; set; }
        [field: SerializeField] public bool IsTimeNormalized { get; set; }
        [field: SerializeField] public bool IsOneFrameAction { get; set; }
        [field: SerializeField] public float StartTime { get; set; }
        [field: SerializeField] public float EndTime { get; set; }

        #endregion

        #region Public Functions ######################################################

        internal void Evaluate(AnimancerMachine machine, float delta, float currentTime, float animDuration)
        {
            if (IsOneTimeAction && (_executionCount > 0 || _hadExecutedOnce))
                return;
            float n_time = Mathf.InverseLerp(0, animDuration, currentTime % animDuration);
            float timeValue = IsTimeNormalized ? n_time : currentTime;
            if (timeValue.InInterval(StartTime, EndTime))
            {
                _isExecuting = true;
                if (!(_hadExecutedOnce && IsOneFrameAction))
                    Process(machine, delta, n_time);
                _hadExecutedOnce = true;
            }
            else
            {
                if (_isExecuting && timeValue >= EndTime)
                    _executionCount++;
                _isExecuting = false;
                _hadExecutedOnce = false;
            }
        }

        public abstract void Process(AnimancerMachine emitter, float delta, float normalizedTime);

        public virtual void Clear()
        {
            _executionCount = 0;
            _isExecuting = false;
            _hadExecutedOnce = false;
        }

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}