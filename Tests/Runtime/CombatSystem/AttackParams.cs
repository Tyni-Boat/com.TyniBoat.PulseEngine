using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    [System.Serializable]
    public class AttackParams
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private string _overrideAnimName;
        [SerializeField]
        private PhysicSpace _physicSpace;
        [SerializeField]
        private List<ComboChainPhase> _chainsPhases = new List<ComboChainPhase>();
        [SerializeField]
        private List<HItFrame> _hitFrames = new List<HItFrame>();

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public string OverrideAnimName { get => _overrideAnimName; }
        public PhysicSpace PhysicSpace { get => _physicSpace; }
        public List<ComboChainPhase> ChainsPhases { get => _chainsPhases; }
        public List<HItFrame> HitFrames { get => _hitFrames; }

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