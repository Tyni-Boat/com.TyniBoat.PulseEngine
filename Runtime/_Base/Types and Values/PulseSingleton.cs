using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Represent an singleton instance.
    /// </summary>
    public abstract class PulseSingleton<T> : MonoBehaviour where T : class
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        #endregion

        #region Statics   #############################################################

        protected static T _instance;

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                GameObject.DontDestroyOnLoad(gameObject);
                return;
            }
            Destroy(gameObject);
        }

        #endregion
    }


}