using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{
    public class State_GetHit : BaseState
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

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Signals      #############################################################

        protected override void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current)
        {
            base.OnPhysicSpaceChanged(last, current);
            _onExitStateTransition = true;
        }

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}