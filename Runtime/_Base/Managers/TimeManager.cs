using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Manage time on scene
    /// </summary>
    public class TimeManager : PulseSingleton<TimeManager>
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        private Dictionary<string, Func<bool>> _conditions = new Dictionary<string, Func<bool>>();

        #endregion

        #region Statics   #############################################################



        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Break the time for a duration
        /// </summary>
        /// <param name="duration">break duration in ms</param>
        /// <param name="overridePrevious">override any previous command?</param>
        public static async void BreakTime(float duration = 0.1f, bool overridePrevious = true)
        {
            if (_instance == null)
                return;
            //if (_instance._conditions == null)
            //    return;
            //if (_instance._conditions.ContainsKey("ShortBreak") && !overridePrevious)
            //    return;
            if (_instance.IsInvoking(nameof(_instance.RestoreTime)))
            {
                if (!overridePrevious)
                    return;
                _instance.CancelInvoke(nameof(_instance.RestoreTime));
            }
            float breakScale = 0f;
            Time.timeScale = breakScale;
            await Task.Delay((int)(duration * 1000));
            Time.timeScale = 1;
            //_instance.Invoke(nameof(_instance.RestoreTime), duration * breakScale);
        }

        #endregion

        #region Private Functions #####################################################

        /// <summary>
        /// Evaluate the conditions and remove those that are true.
        /// </summary>
        private void EvaluateConditions()
        {
            if (_conditions == null)
                return;
            if (_conditions.Count <= 0)
                return;
            for (int i = _conditions.Count - 1; i >= 0; i--)
            {
                var condition = _conditions.ElementAt(i);
                if (condition.Value != null)
                {
                    if (condition.Value.Invoke())
                        _conditions.Remove(condition.Key);
                }
                else
                {
                    _conditions.Remove(condition.Key);
                }
            }
        }

        /// <summary>
        /// Restore the time flow
        /// </summary>
        private void RestoreTime()
        {
            Time.timeScale = 1;
        }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void LateUpdate()
        {
            EvaluateConditions();
        }

        #endregion
    }

}