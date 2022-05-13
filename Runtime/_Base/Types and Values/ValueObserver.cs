using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// To observe the evolution of an value
    /// </summary>
    public class ValueObserver<T> where T : IComparable<T>, IEquatable<T>
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        private T _lastValue;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// To evaluate the observer
        /// </summary>
        /// <param name="delta"></param>
        public void Evaluate(T target, T threshold = default, Action<T> OnValueChanged = null, Action<bool> OnThresholdReached = null, float delta = -1)
        {
            if (!target.Equals(_lastValue))
            {
                OnValueChanged?.Invoke(target);
            }
            bool thresholdUp = _lastValue.CompareTo(threshold) <= 0 && target.CompareTo(threshold) > 0;
            bool thresholdDown = _lastValue.CompareTo(threshold) > 0 && target.CompareTo(threshold) <= 0;
            if (thresholdUp || thresholdDown)
            {
                OnThresholdReached?.Invoke(thresholdUp);
            }
            _lastValue = target;
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