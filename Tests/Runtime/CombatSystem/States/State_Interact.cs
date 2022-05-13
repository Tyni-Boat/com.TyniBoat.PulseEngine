using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    public class State_Interact : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private List<float> _interactionFrames;

        private int _lastInteraction = -1;

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

        private void MakeInteraction()
        {
            //if (!_character)
            //    return;
            //PulseDebug.DrawCircle(_character.transform.position, 2, _character.CurrentGravityDirection, Color.yellow);
            //_character.Interact();
        }

        #endregion

        #region Signals      #############################################################

        protected override void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current)
        {
            base.OnPhysicSpaceChanged(last, current);
            JumpToEndOfState();
        }

        #endregion

        #region MonoBehaviours ########################################################

        protected override void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateEnter(animator, stateInfo, layerIndex);
            _interactionFrames?.Sort();
            _lastInteraction = -1;
        }

        protected override void OnSubStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateUpdate(animator, stateInfo, layerIndex);
            if (_interactionFrames != null)
            {
                for (int i = _interactionFrames.Count - 1; i > _lastInteraction; i--)
                {
                    if (_interactionFrames[i] <= stateInfo.normalizedTime)
                    {
                        MakeInteraction();
                        _lastInteraction = i;
                        break;
                    }
                }
            }
        }

        #endregion
    }

}