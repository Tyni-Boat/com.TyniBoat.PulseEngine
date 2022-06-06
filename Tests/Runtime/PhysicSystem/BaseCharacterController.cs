using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine.CharacterControl
{

    public abstract class BaseCharacterController : MonoBehaviour
    {
        #region Constants ##########################################################################################################################

        public const float MAX_SURFACE_DETECTION_DISTANCE = 25;

        #endregion

        #region Variables ##########################################################################################################################

        [Header("Base configs")]
        [SerializeField] protected LayerMask _layerMask = 1;
        [SerializeField] protected float _groundedSurfaceDist = 0.5f;
        [field: SerializeField] public BaseGravityZone GravityZone { get; internal set; }
        [SerializeField] protected SurfaceInformations _currentSurface = new SurfaceInformations();
        [SerializeField] protected float _surfaceSnapBufferLenght = 0;
        [SerializeField][Range(0, 1)] protected float _surfaceSnapForceMultiplier = 1;
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
        protected Vector3 _userImpulsedForce;
        protected Vector3 _momemtum;
        protected float _airTime;
        protected float _surfaceDistance;
        protected float _gravityAcc;

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

        #region Cache ########################################################################################################################

        protected List<Collider> _childColliders3D = new List<Collider>();

        protected List<Collider2D> _childColliders2D = new List<Collider2D>();

        #endregion

        #region Functions ########################################################################################################################

        /// <summary>
        /// Move the controller in a direction
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        public abstract void Move(Vector3 direction, float speed);

        /// <summary>
        /// Impulse an instant force to the character.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="direction"></param>
        public abstract void ImpulseForce(float force, Vector3 direction = default);

        /// <summary>
        /// Manage inner 3D collisions
        /// </summary>
        /// <param name="parentCollider"></param>
        /// <param name="state"></param>
        protected void InnerCollision3DState(Collider parentCollider, bool state)
        {
            if (_childColliders3D.Count > 0)
            {
                for (int i = 0; i < _childColliders3D.Count; i++)
                {
                    if (parentCollider)
                        Physics.IgnoreCollision(parentCollider, _childColliders3D[i], state);
                    for (int j = 0; j < _childColliders3D.Count; j++)
                    {
                        Physics.IgnoreCollision(_childColliders3D[i], _childColliders3D[j], state);
                    }
                }
            }
        }

        /// <summary>
        /// Manage inner 2D collisions
        /// </summary>
        /// <param name="parentCollider"></param>
        /// <param name="state"></param>
        protected void InnerCollision2DState(Collider2D parentCollider, bool state)
        {
            if (_childColliders2D.Count > 0)
            {
                for (int i = 0; i < _childColliders2D.Count; i++)
                {
                    if (parentCollider)
                        Physics2D.IgnoreCollision(parentCollider, _childColliders2D[i], state);
                    for (int j = 0; j < _childColliders2D.Count; j++)
                    {
                        Physics2D.IgnoreCollision(_childColliders2D[i], _childColliders2D[j], state);
                    }
                }
            }
        }

        #endregion
    }
}