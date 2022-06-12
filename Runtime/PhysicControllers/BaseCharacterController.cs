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
        [SerializeField][Range(0, 1)] protected float _snapUpForceMultiplier = 1;
        [SerializeField][Range(0, 1)] protected float _snapDownForceMultiplier = 1;
        [SerializeField][Range(0, 1)] protected float _nonFlatGroundPenetrationFactor = 1;
        [Space]
        [Header("Base State and Alterations")]
        [SerializeField] protected PhysicSpace _currentPhysicSpace;
        [SerializeField] protected bool _innerCollisions;
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
        protected float _noChechSurfaceTime;

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

        /// <summary>
        /// The character can perform a jump
        /// </summary>
        public bool CanJump => _noChechSurfaceTime <= 0;

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
        public abstract void Move(Vector3 direction, float moveSpeed, float rotationSpeed);

        /// <summary>
        /// Add a constant force to the character.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="direction"></param>
        public abstract void AddConstantForce(float force, Vector3 direction = default);

        /// <summary>
        /// Jump to reach a specific peak height
        /// </summary>
        /// <param name="height"></param>
        /// <returns>Get the time needed to reach the peak height</returns>
        public abstract float JumpForHeight(float height);

        /// <summary>
        /// Jump from the current position to a point
        /// </summary>
        /// <param name="point">Location where to try to reach</param>
        /// <param name="height">Peak height of the jump</param>
        /// <returns>return the peak height time as X and the total jump time as Y</returns>
        public abstract Vector2 JumpTo(Vector3 point, float height);

        /// <summary>
        /// Make the controller ignore collision or not.
        /// </summary>
        /// <param name="value"></param>
        public abstract void IgnoreCollisionMode(bool value, bool activeCollisionBetweenChildren = false);

        /// <summary>
        /// Adjust the shape of the character.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public abstract void AdjustShape(float height, float width, float stepOffset);


        /// <summary>
        /// Refresh the list of children colliders.
        /// </summary>
        public abstract void RefreshChildrenColliders();

        /// <summary>
        /// Manage inner 3D collisions
        /// </summary>
        /// <param name="parentCollider"></param>
        /// <param name="state"></param>
        protected void InnerCollision3DState(Collider parentCollider, bool state)
        {
            if (_childColliders3D.Count > 0)
            {
                for (int i = _childColliders3D.Count - 1; i >= 0 ; i--)
                {
                    if (_childColliders3D[i] == null)
                    {
                        _childColliders3D.RemoveAt(i);
                        continue;
                    }
                    if (parentCollider)
                        Physics.IgnoreCollision(parentCollider, _childColliders3D[i], true);
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
                for (int i = _childColliders2D.Count - 1; i >= 0; i--)
                {
                    if (_childColliders2D[i] == null)
                    {
                        _childColliders2D.RemoveAt(i);
                        continue;
                    }
                    if (parentCollider)
                        Physics2D.IgnoreCollision(parentCollider, _childColliders2D[i], true);
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