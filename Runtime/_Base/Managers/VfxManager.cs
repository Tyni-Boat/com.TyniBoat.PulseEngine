using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Manage vfx creation on scene
    /// </summary>
    public class VfxManager : PulseSingleton<VfxManager>
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        public GameObject _hitImpact;
        public GameObject _defImpact;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        public static void CreateHitVfx(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_instance == null)
                return;
            if (_instance._hitImpact)
            {
                Instantiate(_instance._hitImpact, position, rotation, parent);
            }
        }

        internal static void CreateDefenseImpactVfx(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_instance == null)
                return;
            if (_instance._defImpact)
            {
                Instantiate(_instance._defImpact, position, rotation, parent);
            }
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