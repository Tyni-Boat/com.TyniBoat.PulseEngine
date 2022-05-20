using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine.CharacterControl
{

    public abstract class BaseCharacterController : MonoBehaviour
    {

        #region Variables ##########################################################################################################################

        [Header("Base configs")]
        [SerializeField] protected LayerMask _layerMask = 1;
        [SerializeField] protected float _groundedSurfaceDist = 0.5f;
        [field: SerializeField] public BaseGravityZone GravityZone { get; internal set; }
        [SerializeField] protected SurfaceInformations _currentSurface = new SurfaceInformations();
        [SerializeField] protected float _surfaceSnapSpeed = 50;
        [Space]
        [Header("Base State and Alterations")]
        [SerializeField] protected PhysicSpace _currentPhysicSpace;
        [SerializeField] protected bool _ignoreCollision;
        [SerializeField] protected float _airTimeToBeInAir;
        [SerializeField] protected bool _debug;

        //state
        protected PhysicSpace _lastPhysicSpace;

        ///motion
        protected Vector3 _userMoveVector;
        protected Vector3 _userJumpVector;
        protected Vector3 _momemtum;
        protected float _airTime;
        protected float _surfaceDistance;
        protected float _gravityAcc;
        protected float _jumpRequestTimeToPeak;

        #endregion

        #region Properties ########################################################################################################################

        /// <summary>
        /// The current physic space of the controller
        /// </summary>
        public PhysicSpace CurrentPhysicSpace { get => _currentPhysicSpace; }

        /// <summary>
        /// Determine if collisions should be ignored
        /// </summary>
        public bool IgnoreCollision { get => _ignoreCollision; }

        /// <summary>
        /// The normal user for planar movement.
        /// </summary>
        public Vector3 MovementNormal { get; protected set; }

        /// <summary>
        /// The surface distance
        /// </summary>
        public float SurfaceDistance { get => _surfaceDistance; }

        /// <summary>
        /// Time Spend in the air.
        /// </summary>
        public float AirTime { get => _airTime; }

        #endregion
    }
}