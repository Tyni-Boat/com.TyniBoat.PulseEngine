using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.PhysicSystem
{
    /// <summary>
    /// Component for an entity physic
    /// </summary>
    public class PhysicSystemComponent : PulseModuleComponent
    {
        #region Variables ##############################################

        internal Rigidbody _rigidbody;
        internal Collider _collider;

        #endregion

        #region Properties ##############################################

        [field: SerializeField]
        public Vector3 DesiredGravityDir { get; internal set; }

        [field: SerializeField]
        public AnimationCurve GravityCurve { get; internal set; }

        [field: SerializeField]
        public LayerMask GroundLayer { get; internal set; }

        [field: SerializeField]
        public float GravityMultiplier { get; set; }

        [field: SerializeField]
        public PhysicSpace CurrentPhysicSpace { get; internal set; }



        public Vector3 SurfaceSnapPoint { get; internal set; }

        public Quaternion SurfaceSnapRot { get; internal set; }

        public float AirTime { get; internal set; }

        public float SurfaceDistance { get; internal set; }

        public PhysicSpace LastPhysicSpace { get; internal set; }


        public Vector3 CurrentGravityDirection { get; internal set; }

        public Vector3 CurrentSurfaceNormal { get; internal set; }


        public Vector3 CenterOfMass
        {
            get
            {
                if (_rigidbody)
                    return _rigidbody.worldCenterOfMass;
                return transform.position;
            }
        }

        public float ColliderWidth
        {
            get
            {
                if (_collider)
                    return _collider.bounds.extents.x;
                return 0.5f;
            }
        }

        #endregion

        #region Events ##############################################

        public event EventHandler<KeyValuePair<PhysicSpace, PhysicSpace>> OnPhysicSpaceChanged;

        public event EventHandler<KeyValuePair<Vector3, Vector3>> OnGravityChanged;

        #endregion

        #region Mono ##############################################

        private void Awake()
        {
            SurfaceSnapPoint = Vector3.negativeInfinity;
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            AddFeature<ThirdPersonPhysic>();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            RemoveFeature<ThirdPersonPhysic>();
        }

        #endregion

        #region functions ##############################################

        internal void TriggerSpaceChange(PhysicSpace newSpace)
        {
            OnPhysicSpaceChanged?.Invoke(this, new KeyValuePair<PhysicSpace, PhysicSpace>(LastPhysicSpace, newSpace));
        }

        /// <summary>
        /// Set the velocity.
        /// </summary>
        /// <param name="velocity"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void SetVelocity(Vector3 velocity)
        {
            if (_rigidbody)
            {
                _rigidbody.velocity = velocity;
            }
        }

        /// <summary>
        /// Add force to the rigidbody.
        /// </summary>
        /// <param name="magnitude"></param>
        /// <param name="direction"></param>
        /// <param name="againstGravity"></param>
        /// <param name="mode"></param>
        public void AddForce(float magnitude, Vector3 direction, bool againstGravity = false, ForceMode mode = ForceMode.Force)
        {
            Vector3 dir = againstGravity ? -CurrentGravityDirection : direction;
            _rigidbody?.AddForce(dir * magnitude, mode);
        }

        #endregion
    }
}