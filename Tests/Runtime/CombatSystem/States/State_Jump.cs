using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    public class State_Jump : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [Header("State Params")]
        [SerializeField] [Range(0, 1)] private float _jumpTime = 0;
        [SerializeField] [Range(0, 1)] private float _velocityconservationRatio = 0;
        [SerializeField] private float _jumpForceMultiplier = 0;
        [SerializeField] private bool _useInputDirection;

        private bool _isJumping = false;
        private Vector3 _lastStateVelocity;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        private void Propulse(Rigidbody rigidbody, Character character)
        {
            if (rigidbody == null)
                return;
            if (character == null)
                return;
            if (_isJumping)
                return;
            _isJumping = true;
            _character.Jump(_jumpForceMultiplier, _useInputDirection);
        }

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        protected override void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateEnter(animator, stateInfo, layerIndex);
            if (_rigidBody)
                _lastStateVelocity = _rigidBody.velocity;
            _isJumping = false;
        }

        protected override void OnSubStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateUpdate(animator, stateInfo, layerIndex);
            if (stateInfo.normalizedTime >= _jumpTime)
            {
                Propulse(_rigidBody, _character);
            }
        }

        protected override void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateExit(animator, stateInfo, layerIndex);
        }

        #endregion
    }

}