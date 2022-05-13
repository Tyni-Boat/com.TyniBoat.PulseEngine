using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    [System.Serializable]
    public class ComboChainPhase
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        [field: SerializeField] public float TriggerTime { get; set; }
        [field: SerializeField] public float EndTime { get; set; }
        [field: SerializeField] public string NextStateName { get; set; }
        [field: SerializeField] public float NextStateTransition { get; set; }

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