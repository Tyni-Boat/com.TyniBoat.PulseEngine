using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{
    public class State_Defense : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField] private string _idleStateName;
        [SerializeField] private float _idleStateTransition = 0.15f;
        [SerializeField] private string _reverseStateName;

        [SerializeField] private bool _parryState;
        [SerializeField] private bool _loopState;


        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public bool ParryState { get => _parryState; }

        #endregion

        #region Public Functions ######################################################

        public void Reverse()
        {
            if (!CanMakeActions)
                return;
            if (_animator == null)
                return;
            if (!_character)
                return;
            var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            if (transitionInfos.duration > 0)
                return;
            TriggerAnimatorState(_reverseStateName);
        }

        #endregion

        #region Private Functions #####################################################

        private void ReleaseDefense(bool value)
        {
            if (value)
                return;
            if (_animator == null)
                return;
            var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            if (transitionInfos.duration > 0)
                return;
            if (_character)
            {
                _character.DefenseAction.RemoveListener(ReleaseDefense);
            }
            TriggerAnimatorState(_idleStateName, _idleStateTransition);
        }

        protected override void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current)
        {
            base.OnPhysicSpaceChanged(last, current);
            ReleaseDefense(!string.IsNullOrEmpty(_idleStateName));
        }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################
        protected override void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateEnter(animator, stateInfo, layerIndex);
            if (_character)
            {
                if (_loopState)
                    _character.DefenseAction.AddListener(ReleaseDefense);
            }
        }

        protected override void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateExit(animator, stateInfo, layerIndex);
            if (_character)
            {
                if (_loopState)
                    _character.DefenseAction.RemoveListener(ReleaseDefense);
            }
        }


        #endregion
    }

}