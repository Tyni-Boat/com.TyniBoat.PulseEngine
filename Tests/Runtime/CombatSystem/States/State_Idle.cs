using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{
    public class State_Idle : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [Header("Transition Params")]
        [SerializeField] private string _attackStateName;
        [SerializeField] private float _attackStateTransition;
        [SerializeField] private string _moveStateName;
        [SerializeField] private float _moveStateTransition;
        [SerializeField] private string _jumpStateName;
        [SerializeField] private float _jumpStateTransition;
        [SerializeField] private string _interactionStateName;
        [SerializeField] private float _interactionTransition = 0.25f;
        [SerializeField] private string _defenseStateName;
        [SerializeField] private float _defenseStateTransition = 0;

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

        private void Interact()
        {
            if (_character == null)
                return;
            if (!_character.CanInteract)
                return;
            TriggerAnimatorState(_interactionStateName, _interactionTransition);
        }

        //Attcks

        private void Attack()
        {
            //if (!CanMakeActions)
            //    return;
            //if (_animator == null)
            //    return;
            //if (!_character)
            //    return;
            //var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            //if (transitionInfos.duration > 0)
            //    return;
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
            //        AirAttack();
            //        break;
            //    case PhysicSpace.onGround:
            //        GroundAttack();
            //        break;
            //    case PhysicSpace.inFluid:
            //        break;
            //}
        }

        private void GroundAttack()
        {
            //if (_character)
            //{
            //    if (_character.CurrentPhysicSpace != PhysicSpace.onGround)
            //        return;
            //    _character.AttackAction.RemoveListener(Attack);
            //}
            //TriggerAnimatorState(_attackStateName, _attackStateTransition);
        }

        private void AirAttack()
        {
            //if (_character)
            //{
            //    if (_character.CurrentPhysicSpace != PhysicSpace.inAir)
            //        return;
            //    _character.AttackAction.RemoveListener(Attack);
            //}
            //TriggerAnimatorState(_attackStateName, _attackStateTransition);
        }

        //moves

        private void Move(Animator animator, int layer)
        {
            //if (!animator)
            //    return;
            //if (!_character)
            //    return;
            //if (_character.DesiredDirection.sqrMagnitude <= 0.1f)
            //    return;
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
            //        break;
            //    case PhysicSpace.onGround:
            //        GroundMove(animator);
            //        break;
            //    case PhysicSpace.inFluid:
            //        break;
            //}
        }

        private void GroundMove(Animator animator)
        {
            //if (_character.CurrentPhysicSpace != PhysicSpace.onGround)
            //    return;
            //TriggerAnimatorState(_moveStateName, _moveStateTransition);
        }

        //Jumps

        private void Jump()
        {
            //if (!CanMakeActions)
            //    return;
            //if (_animator == null)
            //    return;
            //var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            //if (transitionInfos.duration > 0)
            //    return;
            //if (!_character)
            //    return;
            //if (!_character.CanJump)
            //    return;
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
            //        AirJump();
            //        break;
            //    case PhysicSpace.onGround:
            //        JumpFromGround();
            //        break;
            //    case PhysicSpace.inFluid:
            //        break;
            //}
        }

        private void AirJump()
        {
            //if (_animator == null)
            //    return;
            //if (_character)
            //{
            //    if (_character.CurrentPhysicSpace != PhysicSpace.inAir)
            //        return;
            //    _character.JumpAction.RemoveListener(Jump);
            //}
            //TriggerAnimatorState(_jumpStateName, _jumpStateTransition);
        }

        private void JumpFromGround()
        {
            //if (_animator == null)
            //    return;
            //if (_character)
            //{
            //    if (_character.CurrentPhysicSpace != PhysicSpace.onGround)
            //        return;
            //    _character.JumpAction.RemoveListener(Jump);
            //}
            //TriggerAnimatorState(_jumpStateName, _jumpStateTransition);
        }

        //Defense

        private void Defend(bool v)
        {
            //if (!CanMakeActions)
            //    return;
            //if (!v)
            //    return;
            //if (_animator == null)
            //    return;
            //if (!_character)
            //    return;
            //var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            //if (transitionInfos.duration > 0)
            //    return;
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
            //        break;
            //    case PhysicSpace.onGround:
            //        GroundDefense();
            //        break;
            //    case PhysicSpace.inFluid:
            //        break;
            //}
        }

        private void GroundDefense()
        {
            //if (_character)
            //{
            //    if (_character.CurrentPhysicSpace != PhysicSpace.onGround)
            //        return;
            //    _character.DefenseAction.RemoveListener(Defend);
            //}
            //TriggerAnimatorState(_defenseStateName, _defenseStateTransition);
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
                _character.AttackAction.AddListener(Attack);
                _character.JumpAction.AddListener(Jump);
                _character.InteractAction.AddListener(Interact);
                _character.DefenseAction.AddListener(Defend);
            }
        }

        protected override void OnSubStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateUpdate(animator, stateInfo, layerIndex);
            Move(animator, layerIndex);
        }

        protected override void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateExit(animator, stateInfo, layerIndex);
            if (_character)
            {
                _character.AttackAction.RemoveListener(Attack);
                _character.JumpAction.RemoveListener(Jump);
                _character.InteractAction.RemoveListener(Interact);
                _character.DefenseAction.RemoveListener(Defend);
            }
        }

        #endregion
    }

}