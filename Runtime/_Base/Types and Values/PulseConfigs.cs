using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{
    /// <summary>
    /// Scriptable object storing global configurable values.
    /// </summary>
    public class PulseConfigs : ScriptableObject
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField] private string _resourcesBasePath;


        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        /// <summary>
        /// The Base path for resources
        /// </summary>
        public string ResourcesBasePath { get => _resourcesBasePath; }

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}