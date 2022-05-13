using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{
    public class State_Move : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [Header("Animator Params")]
        [SerializeField] private string _moveSpeedFloatParam;
        [SerializeField] private string _lastMoveSpeedFloatParam;

        [Header("Transition Params")]
        [SerializeField] private string _runGroundAttackStateName;
        [SerializeField] private float _runAttackStateTransition;
        [SerializeField] private string _sprintGroundAttackStateName;
        [SerializeField] private float _sprintAttackStateTransition;
        [SerializeField] private string _groundJumpStateName;
        [SerializeField] private float _jumpStateTransition;

        private bool _isSprinting;
        private float _speed;

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

        private void MoveAttack()
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
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
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
            //if (_animator == null)
            //    return;
            //if (!_character)
            //    return;
            //if (_character.CurrentPhysicSpace != PhysicSpace.onGround)
            //    return;
            //_character.AttackAction.RemoveListener(MoveAttack);
            //if (_isSprinting)
            //    TriggerAnimatorState(_sprintGroundAttackStateName, _sprintAttackStateTransition);
            //else
            //    TriggerAnimatorState(_runGroundAttackStateName, _runAttackStateTransition);
        }

        private void Sprint(bool value)
        {
            if (!CanMakeActions)
                return;
            _isSprinting = value;
        }

        private void MoveCharacterOnGround(float deltaTime, Animator animator)
        {
            if (animator == null)
                return;
            if (_character == null)
                return;
            float deccelerationRatio = 0.5f;
            float targetMagnitude = _character.DesiredDirection.magnitude * (_isSprinting ? 2 : 1);
            float currentCharSpeed = Mathf.Lerp(_speed, targetMagnitude, deltaTime * _character.MoveAcceleration);
            bool isDecceleration = currentCharSpeed > targetMagnitude;
            _speed = currentCharSpeed * (isDecceleration ? deccelerationRatio : 1);
            animator.SetFloat(_moveSpeedFloatParam, _speed);
            if (!isDecceleration)
            {
                animator.SetFloat(_lastMoveSpeedFloatParam, currentCharSpeed);
                _character.RotateToward(_character.transform.position + _character.DesiredDirection, deltaTime * _character.TurnSpeed * (_isSprinting ? 0.5f : 1));
            }
        }

        private void Jump()
        {
            //if (!CanMakeActions)
            //    return;
            //if (_animator == null)
            //    return;
            //var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            //if (transitionInfos.duration > 0)
            //    return;
            //if (_character == null)
            //    return;
            //if (!_character.CanJump)
            //    return;
            //switch (_character.LastPhysicSpace)
            //{
            //    case PhysicSpace.unSpecified:
            //        break;
            //    case PhysicSpace.inAir:
            //        break;
            //    case PhysicSpace.onGround:
            //        JumpFromGround();
            //        break;
            //    case PhysicSpace.inFluid:
            //        break;
            //}
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
            //TriggerAnimatorState(_groundJumpStateName, _jumpStateTransition);
        }


        #endregion

        #region Signals      #############################################################

        protected override void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current)
        {
            base.OnPhysicSpaceChanged(last, current);
            if (_animator)
            {
                _animator.SetFloat(_moveSpeedFloatParam, 0);
                _animator.SetFloat(_lastMoveSpeedFloatParam, 0);
            }
        }

        #endregion

        #region MonoBehaviours ########################################################

        protected override void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateEnter(animator, stateInfo, layerIndex);
            _isSprinting = false;
            _speed = 0;
            if (_character)
            {
                _character.RotateToward(_character.transform.position + _character.DesiredDirection, 1);
                _character.AttackAction.AddListener(MoveAttack);
                _character.SprintAction.AddListener(Sprint);
                _character.JumpAction.AddListener(Jump);
            }
        }

        protected override void OnSubStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //base.OnSubStateMove(animator, stateInfo, layerIndex);
            //if (_character)
            //{
            //    switch (_character.LastPhysicSpace)
            //    {
            //        case PhysicSpace.unSpecified:
            //            break;
            //        case PhysicSpace.inAir:
            //            break;
            //        case PhysicSpace.onGround:
            //            MoveCharacterOnGround(Time.deltaTime, animator);
            //            break;
            //        case PhysicSpace.inFluid:
            //            break;
            //    }
            //}
        }

        protected override void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateExit(animator, stateInfo, layerIndex);
            if (_character)
            {
                _character.AttackAction.RemoveListener(MoveAttack);
                _character.SprintAction.RemoveListener(Sprint);
                _character.JumpAction.RemoveListener(Jump);
            }
        }

        #endregion
    }

}