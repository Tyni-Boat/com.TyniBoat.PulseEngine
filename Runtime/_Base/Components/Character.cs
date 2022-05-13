using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;


namespace PulseEngine
{

    /// <summary>
    /// Represent a basic movable character
    /// </summary>
    public abstract class Character : PulseObject
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        protected Camera _camera;
        protected Collider _collider;

        private int _jumpsCount;
        private List<IInteractableObject> _interactableObjectList = new List<IInteractableObject>();
        private PulseObject _currentWeapon;
        private object _currentAnimationState;


        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        [field: SerializeField]
        public float MoveMaxSpeed { get; protected set; }

        [field: SerializeField]
        public float MoveAcceleration { get; protected set; }

        [field: SerializeField]
        public float TurnSpeed { get; protected set; }

        [field: SerializeField]
        public float JumpForce { get; protected set; }

        [field: SerializeField]
        public int MaxJumpsCount { get; protected set; }

        [field: SerializeField]
        public float AutoLockDistance { get; protected set; }

        [field: SerializeField]
        public bool SuspendInputs { get; protected set; }




        public PhysicSpace CurrentPhysicSpace { get; set; }

        public bool CanJump => _jumpsCount < MaxJumpsCount;

        public Vector3 DesiredDirection { get; protected set; }

        public float ColliderRadius
        {
            get
            {
                var capsule = _collider as CapsuleCollider;
                return capsule ? capsule.radius : 0;
            }
        }

        public bool CanInteract { get => _interactableObjectList.Count > 0 && _interactableObjectList.Where(i => i.CanInteract(gameObject)).Count() > 0; }

        public PulseObject CurrentWeapon => _currentWeapon;

        public object CurrentAnimationState { get => _currentAnimationState; set => _currentAnimationState = value; }

        //Actions

        public UnityEvent AttackAction { get; protected set; } = new UnityEvent();
        public UnityEvent<bool> SprintAction { get; protected set; } = new UnityEvent<bool>();
        public UnityEvent<bool> DefenseAction { get; protected set; } = new UnityEvent<bool>();
        public UnityEvent JumpAction { get; protected set; } = new UnityEvent();
        public UnityEvent InteractAction { get; protected set; } = new UnityEvent();

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// To override the animator controller.
        /// </summary>
        /// <typeparam name="T">The type of State Machine behaviour to return</typeparam>
        /// <param name="overrideController">The override controller</param>
        /// <param name="getBehaviours">get the behaviour array?</param>
        /// <param name="nullToBase">restore base animator controller if override is null?</param>
        /// <returns></returns>
        public T[] OverrideAnimations<T>(AnimatorOverrideController overrideController, bool getBehaviours = false, bool nullToBase = true) where T : StateMachineBehaviour
        {
            //if (_animator == null)
            //    return null;
            //if (overrideController == null)
            //{
            //    if (_baseAnimatorController && nullToBase)
            //        _animator.runtimeAnimatorController = _baseAnimatorController;
            //    return nullToBase ? new T[0] : null;
            //}
            //if (overrideController.runtimeAnimatorController != _baseAnimatorController)
            //    return null;
            //_animator.runtimeAnimatorController = overrideController;
            //return _animator.GetBehaviours<T>();
            return default;
        }

        /// <summary>
        /// Make the character able or unable to interact with this interactable
        /// </summary>
        /// <param name="interactable"></param>
        public void CanInteractWith(IInteractableObject interactable, bool state)
        {
            if (interactable == null)
                return;
            if (state && !_interactableObjectList.Contains(interactable))
                _interactableObjectList.Add(interactable);
            else if (!state && _interactableObjectList.Contains(interactable))
                _interactableObjectList.Remove(interactable);
        }

        /// <summary>
        /// Get Bone Transform on the character, or the character transform itself if the character animator is not human
        /// </summary>
        /// <param name="equipBone"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Transform GetBone(HumanBodyBones bone)
        {
            //if (_animator == null)
            //    return transform;
            //if (!_animator.isHuman)
            //    return transform;
            //return _animator.GetBoneTransform(bone);
            return default;
        }

        /// <summary>
        /// Set weapon as the character's current weapon.
        /// </summary>
        /// <param name="weapon"></param>
        public bool SetCurrentWeapon(PulseObject weapon)
        {
            if (weapon == null)
                return false;
            _currentWeapon = weapon;
            return true;
        }



        /// <summary>
        /// Block an incoming attack.
        /// </summary>
        /// <param name="hitMakerPosition">The position of the one who perform the attack</param>
        /// <param name="hitBoxCenter">The center of the hit box in world coordinates</param>
        /// <param name="damages">the amount of damages taken</param>
        /// <param name="hitDirection">The direction of the hit</param>
        /// <param name="hitIntensity">the hit intensity</param>
        public virtual float Defense(Vector3 hitMakerPosition, Vector3 hitBoxCenter, float damages, Direction hitDirection = Direction.forward, int hitIntensity = 0)
        {
            float damagesTaken = 0;
            RotateToward(hitMakerPosition);
            damagesTaken = InflictDamages(damages * 0.01f);
            PlayDefenseImpactAnimation(hitIntensity);
            PlayHitEffects(hitBoxCenter, hitDirection, hitIntensity, true);
            return damagesTaken;
        }

        /// <summary>
        /// Inflict damages with animations and return the total damages.
        /// </summary>
        /// <param name="hitMakerPosition">The position of the one who perform the attack</param>
        /// <param name="hitBoxCenter">The center of the hit box in world coordinates</param>
        /// <param name="damages">the amount of damages taken</param>
        /// <param name="hitDirection">The direction of the hit</param>
        /// <param name="hitIntensity">the hit intensity</param>
        public virtual float GetHit(Vector3 hitMakerPosition, Vector3 hitBoxCenter, float damages, Direction hitDirection = Direction.forward, int hitIntensity = 0)
        {
            float damagesTaken = 0;
            RotateToward(hitMakerPosition);
            damagesTaken = InflictDamages(damages);
            PlayHitAnimation(hitDirection, hitIntensity);
            PlayHitEffects(hitBoxCenter, hitDirection, hitIntensity);
            return damagesTaken;
        }

        /// <summary>
        /// Rotate to look at the position
        /// </summary>
        /// <param name="position"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void RotateToward(Vector3 position, float delta = 0)
        {
            if (position == transform.position)
                return;
            Vector3 planar = Vector3.ProjectOnPlane(position - transform.position, transform.up).normalized;
            Transform tr = transform;
            //if (_parentTransform != null && _parentTransform.parent != null)
            //    tr = _parentTransform.transform;
            if (delta > 0)
                tr.localRotation = Quaternion.Slerp(tr.localRotation, Quaternion.LookRotation(planar), delta);
            else
                tr.localRotation = Quaternion.LookRotation(planar);
        }

        /// <summary>
        /// Make the character jumps
        /// </summary>
        public virtual void Jump(float multiplier = 1, bool useInputDirection = false)
        {
            //if (!_rigidbody)
            //    return;

            //Vector3 planarVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, -CurrentGravityDirection.normalized);
            //_rigidbody.velocity = planarVelocity;
            //if (useInputDirection)
            //{
            //    if (DesiredDirection.sqrMagnitude > 0.1f)
            //    {
            //        _rigidbody.velocity = Vector3.zero;
            //        RotateToward(CenterOfMass + DesiredDirection);
            //    }
            //    _rigidbody.AddForce((-CurrentGravityDirection.normalized + DesiredDirection.normalized * 0.5f) * JumpForce * multiplier, ForceMode.VelocityChange);
            //}
            //else
            //{
            //    _rigidbody.AddForce((-CurrentGravityDirection.normalized) * JumpForce * multiplier, ForceMode.VelocityChange);
            //}
            //AirTime = 0;
            //_jumpsCount++;
        }

        /// <summary>
        /// Interact with the nearest interactable object.
        /// </summary>
        public virtual void Interact()
        {
            if (_interactableObjectList == null)
                return;
            if (_interactableObjectList.Count <= 0)
                return;
            List<IInteractableObject> tmpList = new List<IInteractableObject>(_interactableObjectList);
            _interactableObjectList.Clear();
            for (int i = 0; i < tmpList.Count; i++)
            {
                if (tmpList[i] == null)
                    continue;
                if (!tmpList[i].CanInteract(gameObject))
                    continue;
                _interactableObjectList.Add(tmpList[i]);
            }
            if (_interactableObjectList.Count <= 0)
                return;
            RotateToward(_interactableObjectList[0].InteractableTransformParams.position, 1);
            _interactableObjectList[0].Interract(gameObject);
        }

        #endregion

        #region Private Functions #####################################################

        /// <summary>
        /// Get the desired direction
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="animator"></param>
        protected virtual void CalculateDesiredDirection(float deltaTime)
        {
        }

        /// <summary>
        /// Get the triggered actions.
        /// </summary>
        protected virtual void GetActions()
        {
        }

        /// <summary>
        /// Play Hit sfx and Vfx
        /// </summary>
        /// <param name="hitActorPosition"></param>
        /// <param name="hitDirection"></param>
        /// <param name="hitIntensity"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void PlayHitEffects(Vector3 impactPoint, Direction hitDirection, int hitIntensity, bool defending = false)
        {
            Vector3 point = impactPoint;
            if (_collider)
            {
                point = _collider.ClosestPoint(impactPoint);
            }
            if (defending)
                VfxManager.CreateDefenseImpactVfx(point, Quaternion.identity);
            else
                VfxManager.CreateHitVfx(point, Quaternion.identity);
        }

        /// <summary>
        /// Play the corresponding get hit animation
        /// </summary>
        /// <param name="hitDirection"></param>
        /// <param name="hitIntensity"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void PlayHitAnimation(Direction hitDirection, int hitIntensity)
        {
            //if (_animator == null)
            //    return;
            //StringBuilder stateName = new StringBuilder();
            //if (hitIntensity > 0)
            //{
            //    switch (hitIntensity)
            //    {
            //        case 1://BLow
            //            stateName.Append("Blow_");
            //            break;
            //        case 2://Projection
            //            stateName.Append("Project_");
            //            break;
            //        case 3://SimpleSpin
            //            stateName.Append("Spin_");
            //            break;
            //        case 4://SpinProjection
            //            stateName.Append("SpinProject_");
            //            break;
            //        case 5://Ambush
            //            stateName.Append("Ambush_");
            //            break;
            //        case 6://Ragdoll
            //            _rigidbody?.AddRelativeForce((Vector3.up - Vector3.forward) * 10, ForceMode.VelocityChange);
            //            return;
            //    }
            //}
            //stateName.Append("GetHit_");
            //switch (hitDirection)
            //{
            //    case Direction.none:
            //        stateName.Append("F");
            //        break;
            //    case Direction.forward:
            //        stateName.Append("F");
            //        break;
            //    case Direction.back:
            //        stateName.Append("B");
            //        break;
            //    case Direction.left:
            //        stateName.Append("L");
            //        break;
            //    case Direction.right:
            //        stateName.Append("R");
            //        break;
            //    case Direction.up:
            //        stateName.Append("U");
            //        break;
            //    case Direction.down:
            //        stateName.Append("D");
            //        break;
            //}
            //_animator.Play(stateName.ToString(), -1, 0);
        }

        /// <summary>
        /// Play the Defense impact animation
        /// </summary>
        /// <param name="hitIntensity"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void PlayDefenseImpactAnimation(int hitIntensity)
        {
            //if (hitIntensity > 0)
            //{
            //    _animator.Play("Block_Ground_Break", -1, 0);
            //}
            //else
            //{
            //    _animator.Play("Block_Ground_Impact", -1, 0);
            //}
        }

        /// <summary>
        /// Inflict damages to the character with his stats in account
        /// </summary>
        /// <param name="damages"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual float InflictDamages(float damages)
        {
            PulseDebug.Log($"{name} will be inflicted Damages of {damages}");
            return damages;
        }


        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        protected override void Update()
        {
            base.Update();
            CalculateDesiredDirection(Time.deltaTime);
            GetActions();

            if (_interactableObjectList != null)
            {
                for (int i = 0; i < _interactableObjectList.Count; i++)
                {
                    if (_interactableObjectList[i] == null)
                        continue;
                    PulseDebug.DrawCircle(_interactableObjectList[i].InteractableTransformParams.position, 2, transform.up, Color.white);
                }
            }
        }

        protected virtual void Start()
        {
            _camera = Camera.main;
            //_animator = GetComponent<Animator>();
            //if (_animator)
            //    _baseAnimatorController = _animator.runtimeAnimatorController;
            //_rigidbody = GetComponent<Rigidbody>();
            //if (_rigidbody)
            //{
            //    _rigidbody.freezeRotation = true;
            //}
            _collider = GetComponent<Collider>();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        #endregion
    }


}