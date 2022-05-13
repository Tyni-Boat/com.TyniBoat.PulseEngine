using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    public class BaseState : StateMachineBehaviour
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        [HideInInspector]
        private string _stateName;
        [SerializeField]
        [HideInInspector]
        private string _stateAnimName;
        [SerializeField]
        [HideInInspector]
        private string[] _stateBlendTreeAnimNames;
        [SerializeField]
        [HideInInspector]
        private bool _isBlendTree;

        [Header("State Params")]
        [SerializeField]
        [Tooltip("The Operating Physic space of the state")]
        protected PhysicSpace _statePhysicSpace;

        [Header("Character Params")]
        [SerializeField]
        [Tooltip("Disable ground snaping")]
        protected bool PreventCharacterPositionClamping;
        [SerializeField]
        [Tooltip("Disable gravity on the character")]
        protected bool DisableGravity;
        [SerializeField]
        [Tooltip("Disable the ability to continue transition between states")]
        protected bool DisableTrasitionContinuation;
        [SerializeField]
        [Tooltip("Continu the execution of the state logic despite the transition is going out of the state")]
        protected bool EnableLogicOnExitStateTransition;
        [SerializeField]
        [Tooltip("Disable Root Motion of the state")]
        protected bool DisableRootMotion;
        [SerializeField]
        [Tooltip("Lerp Between Last state root motion and this frame root motion, the duration of the normalized time of this frame. It's only active when root motion is active")]
        protected bool LerpRootMotion;
        [SerializeField]
        [Tooltip("The character is Immunized to all damages below this value during this state")]
        protected float DamagesImmunity;

        protected Character _character;
        protected Animator _animator;
        protected Rigidbody _rigidBody;
        protected GizmoDebug _gizmo;
        protected int _layer;
        protected bool _onExitStateTransition;

        private PhysicSpace _physicsSpaceOnStart;
        //animation transition
        private int _currentTranstionTargetHash;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        protected bool CanMakeActions { get => (!_onExitStateTransition || EnableLogicOnExitStateTransition); }
        public string StateName { get => _stateName; }
        public string StateAnimName { get => _isBlendTree ? string.Join("|", _stateBlendTreeAnimNames) : _stateAnimName; }

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Return the number of motions matching motionNames
        /// </summary>
        /// <param name="motionNames"></param>
        /// <returns></returns>
        public int HaveMotions(params string[] motionNames)
        {
            if (motionNames == null)
                return 0;
            int matches = 0;
            if (motionNames.Length > 1)
            {
                if (_isBlendTree && _stateBlendTreeAnimNames != null)
                {
                    for (int i = 0; i < motionNames.Length; i++)
                    {
                        for (int j = 0; j < _stateBlendTreeAnimNames.Length; j++)
                        {
                            if (motionNames[i] == _stateBlendTreeAnimNames[j])
                                matches++;
                        }
                    }
                    return matches;
                }
                return 0;
            }
            else if (motionNames.Length > 0)
            {
                return motionNames[0] == _stateAnimName ? 1 : 0;
            }
            return 0;
        }

        #endregion

        #region Private Functions #####################################################

        protected void TriggerAnimatorState(string stateName, float trasitionValue = 0, int layer = -1)
        {
            if (_animator == null)
                return;
            if (layer <= -1)
                layer = _layer;
            //We cannot transit to self
            var currentState = _animator.GetCurrentAnimatorStateInfo(layer);
            if (currentState.IsName(stateName))
                return;

            //We cannot transit to empty state
            if (string.IsNullOrEmpty(stateName) || string.IsNullOrWhiteSpace(stateName))
                return;

            //If the state dont exist in this layer
            if (!_animator.HasState(layer, Animator.StringToHash(stateName)))
                return;

            if (trasitionValue <= 0)
            {
                _animator.Play(stateName);
            }
            else
            {
                var nexstateInfos = _animator.GetNextAnimatorStateInfo(layer);
                if (_animator.IsInTransition(layer))
                {
                    if (_currentTranstionTargetHash == nexstateInfos.fullPathHash)
                        return;
                    var transitionInfos = _animator.GetAnimatorTransitionInfo(layer);
                    if (!DisableTrasitionContinuation)
                    {
                        trasitionValue = transitionInfos.duration;
                    }
                }
                _animator.CrossFade(stateName, trasitionValue, layer);
                nexstateInfos = _animator.GetNextAnimatorStateInfo(layer);
                _currentTranstionTargetHash = nexstateInfos.fullPathHash;
                _onExitStateTransition = true;
            }
        }

        protected void JumpToEndOfState()
        {
            if (_animator == null)
                return;
            _animator.Play(0, _layer, 1);
        }

        protected virtual void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current) { }

        protected virtual void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        protected virtual void OnSubStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        protected virtual void OnSubStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
        protected virtual void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //base.OnStateEnter(animator, stateInfo, layerIndex);
            //_character = animator.GetComponent<Character>();
            //_rigidBody = animator.GetComponent<Rigidbody>();
            //_gizmo = animator.GetComponent<GizmoDebug>();
            //_animator = animator;
            //_layer = layerIndex;
            //_onExitStateTransition = false;
            //if (_character)
            //{
            //    _physicsSpaceOnStart = _character.CurrentPhysicSpace;
            //    if (_character.SuspendPositionClamp)
            //        _character.SuspendPositionClamp = false;
            //    if (_character.SuspendGravity)
            //        _character.SuspendGravity = false;
            //    if (PreventCharacterPositionClamping)
            //        _character.SuspendPositionClamp = true;
            //    if (DisableGravity)
            //        _character.SuspendGravity = true;
            //}
            //OnSubStateEnter(animator, stateInfo, layerIndex);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //base.OnStateUpdate(animator, stateInfo, layerIndex);
            //if (_character)
            //{
            //    if (_physicsSpaceOnStart != _character.CurrentPhysicSpace)
            //    {
            //        OnPhysicSpaceChanged(_physicsSpaceOnStart, _character.CurrentPhysicSpace);
            //    }
            //    //Manage current anim state
            //    if (_character.CurrentAnimationState != (object)this)
            //        _character.CurrentAnimationState = this;
            //}
            //if (animator.IsInTransition(layerIndex))
            //{
            //    var transitionInfos = animator.GetAnimatorTransitionInfo(layerIndex);
            //    string transitionName = $"{StateName} -> Exit";
            //    int hash = Animator.StringToHash(transitionName);
            //    if (hash == transitionInfos.nameHash)
            //    {
            //        _onExitStateTransition = true;
            //    }
            //    //if (transitionInfos.fullPathHash != 0 && !transitionInfos.anyState && transitionInfos.userNameHash == 0)
            //    //{
            //    //    Debug.Log($"Trasition {transitionName}: nameHash={transitionInfos.nameHash}, userNameHash={transitionInfos.userNameHash}, tryied={hash}");
            //    //    //_onExitStateTransition = true;
            //    //}
            //}
            //if (CanMakeActions)
            //    OnSubStateUpdate(animator, stateInfo, layerIndex);
        }

        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //base.OnStateMove(animator, stateInfo, layerIndex);
            //if (CanMakeActions)
            //{
            //    if (!DisableRootMotion && _rigidBody)
            //    {
            //        Vector3 velocityToApply = animator.velocity;
            //        if (_character)
            //        {
            //            if (_character.CurrentPhysicSpace == PhysicSpace.onGround || _character.CurrentPhysicSpace == PhysicSpace.onWall)
            //            {
            //                velocityToApply = Vector3.ProjectOnPlane(velocityToApply, _character.CurrentSurfaceNormal);
            //            }
            //        }
            //        //Debug.DrawRay(_character.CenterOfMass, animator.velocity.normalized, Color.yellow);
            //        if (LerpRootMotion)
            //            _rigidBody.velocity = Vector3.Lerp(_rigidBody.velocity, velocityToApply, stateInfo.normalizedTime);
            //        else
            //            _rigidBody.velocity = velocityToApply;
            //    }
            //    OnSubStateMove(animator, stateInfo, layerIndex);
            //}
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //base.OnStateExit(animator, stateInfo, layerIndex);
            //if (_character)
            //{
            //    if (PreventCharacterPositionClamping)
            //        _character.SuspendPositionClamp = false;
            //    if (DisableGravity)
            //        _character.SuspendGravity = false;
            //    //Manage current anim state
            //    if (_character.CurrentAnimationState == (object)this)
            //        _character.CurrentAnimationState = null;
            //};
            //OnSubStateExit(animator, stateInfo, layerIndex);
        }

        #endregion
    }

}