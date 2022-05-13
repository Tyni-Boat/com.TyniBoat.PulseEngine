using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;


namespace PulseEngine.CombatSystem
{

    public class State_Attack : BaseState
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [Header("Combo Params")]
        [SerializeField] private List<ComboChainPhase> _comboChainPhases;
        [Header("Hitbox Params")]
        [SerializeField] private List<HItFrame> _hitFrames;
        private int _currentHitFrame;
        private int _currentComboChainPhase;
        private bool _canChainCombo;
        private bool _canRotate;
        private bool _forceExitState;
        private Transform _target;
        private Dictionary<HItFrame, List<Character>> _monoHits = new Dictionary<HItFrame, List<Character>>();

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Inject attack parameters into this state
        /// </summary>
        /// <param name="attackParams"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void InjectParams(AttackParams attackParams)
        {
            _comboChainPhases?.Clear();
            _hitFrames?.Clear();
            if (attackParams == null)
                return;
            _comboChainPhases = new List<ComboChainPhase>();
            _comboChainPhases.AddRange(attackParams?.ChainsPhases);
            _hitFrames = new List<HItFrame>();
            _hitFrames.AddRange(attackParams?.HitFrames);
        }

        #endregion

        #region Private Functions #####################################################

        /// <summary>
        /// Rotate the character toward the nearest target or the desired direction.
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="deltaTime"></param>
        private void RotateToward(Animator animator, Character character, float deltaTime)
        {
            if (animator == null)
                return;
            if (character == null)
                return;
            if (_target == null)
            {
                Vector3 lookDirection = character.transform.forward;
                Vector3 currentPosition = character.transform.position;

                List<Character> inRangeCharacters = new List<Character>();
                var scanAround = Physics.OverlapSphere(animator.transform.position, character.AutoLockDistance);

                Character parentCharacter = null;
                for (int i = 0; i < scanAround.Length; i++)
                {
                    parentCharacter = scanAround[i].GetComponentInParent<Character>();
                    if (parentCharacter != null && parentCharacter == character)
                    {
                        continue;
                    }
                    if (scanAround[i].TryGetComponent<Character>(out var characterX))
                    {
                        inRangeCharacters.Add(characterX);
                    }
                }
                if (inRangeCharacters.Count > 0)
                {
                    bool desiredDir = character.DesiredDirection.sqrMagnitude > 0.1f;
                    var pointingToCharacters = inRangeCharacters.Where(col => Vector3.Dot(desiredDir ? character.DesiredDirection : lookDirection, col.transform.position - currentPosition) >= 0.4f).ToList();
                    if (pointingToCharacters.Count > 0)
                    {
                        pointingToCharacters.Sort((a, b) => { return (a.transform.position - currentPosition).sqrMagnitude.CompareTo((b.transform.position - currentPosition).sqrMagnitude); });
                        PulseDebug.DrawRLine(currentPosition, pointingToCharacters[0].transform.position, desiredDir ? Color.magenta : Color.gray);
                        if (_target != pointingToCharacters[0].transform)
                        {
                            character.RotateToward(pointingToCharacters[0].transform.position);
                            _target = pointingToCharacters[0].transform;
                        }
                        if (desiredDir || _canChainCombo)
                        {
                            character.RotateToward(pointingToCharacters[0].transform.position, deltaTime * character.TurnSpeed);
                        }
                    }
                }
            }
            else if (character.DesiredDirection.sqrMagnitude > 0.1f && _canChainCombo)
            {
                if (_canRotate)
                {
                    character.RotateToward(character.transform.position + character.DesiredDirection, deltaTime * character.TurnSpeed);
                }
            }
        }

        /// <summary>
        /// Allow to chain with thw next attack.
        /// </summary>
        private void ComboChaining()
        {
            if (!CanMakeActions)
                return;
            if (!_canChainCombo)
                return;
            if (_currentComboChainPhase < 0 || _currentComboChainPhase >= _comboChainPhases.Count)
                return;
            var phase = _comboChainPhases[_currentComboChainPhase];
            if (phase == null)
                return;
            if (_animator == null)
                return;
            //Remove listeners
            if (_character)
            {
                _character.AttackAction.RemoveListener(ComboChaining);
            }
            var transitionInfos = _animator.GetAnimatorTransitionInfo(_layer);
            if (transitionInfos.duration > 0)
                return;
            TriggerAnimatorState(phase.NextStateName, phase.NextStateTransition);
        }

        /// <summary>
        /// Trigger the hitBox
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="hit"></param>
        private void ImpactFrame(Animator animator, HItFrame hit, Character character)
        {
            if (animator == null)
                return;
            if (hit == null)
                return;
            if (character == null)
                return;
            //Hit box params
            Vector3 hitBoxPos = Vector3.zero;
            if (animator.isHuman && hit.SourceBone != HumanBodyBones.Hips)
            {
                hitBoxPos = animator.GetBoneTransform(hit.SourceBone).position;
            }
            else
            {
                //hitBoxPos = character.CenterOfMass
                //    + character.transform.forward * (character.ColliderRadius + hit.BoxOffset.z + hit.BoxSize.z * 0.5f)
                //    + character.transform.up * hit.BoxOffset.y
                //    + character.transform.right * hit.BoxOffset.x;
            }
            //hitBoxPos.y += hit.BoxSize.y * 0.5f;
            Vector3 hitBoxSize = hit.BoxSize;
            Quaternion boxRotation = character.transform.rotation;

            //HitBox Appear
            if (hit.UseWeaponCollider && character.CurrentWeapon != null)
            {
                Weapon weapon = character.CurrentWeapon as Weapon;
                if (weapon != null && weapon.CollisionCollider)
                {
                    hitBoxPos = weapon.CollisionCollider.bounds.center;
                    hitBoxSize = weapon.WeaponSize;
                    boxRotation = weapon.transform.rotation;
                }
            }
            var collideds = Physics.OverlapBox(hitBoxPos, hitBoxSize, boxRotation);
            List<Character> characters = new List<Character>();

            Character parentCharacter = null;
            for (int i = 0; i < collideds.Length; i++)
            {
                parentCharacter = collideds[i].GetComponentInParent<Character>();
                if (parentCharacter != null && parentCharacter == character)
                {
                    continue;
                }
                if (collideds[i].TryGetComponent<Character>(out var characterX))
                {
                    characters.Add(characterX);
                }
            }
            if (characters.Count > 0)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    if (_monoHits.ContainsKey(hit) && _monoHits[hit].Contains(characters[i]) && hit.HitEveryXFrames <= 0)
                        continue;
                    if (characters[i] == null)
                        continue;
                    BaseState baseState = characters[i].CurrentAnimationState as BaseState;
                    if (baseState != null)
                    {
                        if (DamagesImmunity > hit.CalculateDamages(character))
                            continue;
                    }
                    State_Defense defense = characters[i].CurrentAnimationState as State_Defense;
                    if (defense != null)
                    {
                        if (defense.ParryState)
                            defense.Reverse();
                        else
                            characters[i].Defense(animator.transform.position, hitBoxPos, hit.CalculateDamages(character), hit.ImpactDirection, (int)hit.ImpactIntensity);
                    }
                    else
                    {
                        characters[i].GetHit(animator.transform.position, hitBoxPos, hit.CalculateDamages(character), hit.ImpactDirection, (int)hit.ImpactIntensity);
                    }
                }
            }
            if (_monoHits.ContainsKey(hit))
                _monoHits[hit].AddRange(characters);
            else
                _monoHits.Add(hit, characters);

            //Debug
            PulseDebug.DrawCube(hitBoxPos, hitBoxSize, boxRotation, characters.Count > 0 ? Color.red : Color.gray);
        }

        #endregion

        #region Signals      #############################################################

        protected override void OnPhysicSpaceChanged(PhysicSpace last, PhysicSpace current)
        {
            base.OnPhysicSpaceChanged(last, current);
            _forceExitState = true;
            JumpToEndOfState();
        }


        #endregion

        #region MonoBehaviours ########################################################

        protected override void OnSubStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateEnter(animator, stateInfo, layerIndex);
            _comboChainPhases?.Sort((a, b) => { return a.TriggerTime.CompareTo(b.TriggerTime); });
            _hitFrames?.Sort((a, b) => { return a.ImpactTime.CompareTo(b.ImpactTime); });
            _currentComboChainPhase = 0;
            _currentHitFrame = 0;
            _canRotate = true;
            _forceExitState = false;
            _target = null;
            _monoHits?.Clear();

            if (_character)
            {
                _character.AttackAction.AddListener(ComboChaining);
            }
        }

        protected override void OnSubStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateUpdate(animator, stateInfo, layerIndex);
            if (_forceExitState)
                return;
            float deltaTime = animator.updateMode == AnimatorUpdateMode.AnimatePhysics ? Time.fixedDeltaTime : Time.deltaTime;
            if (stateInfo.loop && stateInfo.normalizedTime <= 0)
            {
                _monoHits?.Clear();
            }
            //Chain Phase
            _canChainCombo = false;
            for (int i = _currentComboChainPhase; i < _comboChainPhases.Count; i++)
            {
                if (stateInfo.normalizedTime >= _comboChainPhases[i].TriggerTime)
                {
                    if (stateInfo.normalizedTime >= _comboChainPhases[i].EndTime)
                    {
                        _currentComboChainPhase++;
                        continue;
                    }
                    _canChainCombo = true;
                    PulseDebug.DrawCircle(animator.transform.position, 1.5f, animator.transform.up, Color.yellow, 15);
                }
            }
            //Hit Frame
            for (int i = 0; i < _hitFrames.Count; i++)
            {
                if (_hitFrames[i] != null && _hitFrames[i].ImpactFrame(stateInfo.normalizedTime, Time.frameCount))
                {
                    ImpactFrame(animator, _hitFrames[i], _character);
                }
            }
            //Manage movements
            _canRotate = _currentHitFrame <= 0;
        }

        protected override void OnSubStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateMove(animator, stateInfo, layerIndex);
            if (_forceExitState)
                return;
            if (_character)
            {
                RotateToward(animator, _character, Time.deltaTime);
            }
        }

        protected override void OnSubStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSubStateExit(animator, stateInfo, layerIndex);
            //Remove listeners
            if (_character)
            {
                _character.AttackAction.RemoveListener(ComboChaining);
            }
        }

        #endregion
    }

}