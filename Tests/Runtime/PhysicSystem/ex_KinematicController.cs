using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace PulseEngine.CharacterControl
{

    public class ex_KinematicController : BaseCharacterController
    {
        #region Variables ##########################################################################################################################

        [Header("Self Configs")]
        [SerializeField] private float _height = 1.8f;
        [SerializeField] private float _width = 0.37f;
        [SerializeField] private float _stepOffset = 0.25f;
        [SerializeField] private float _slopeAngle = 60;

        //privates ////////////////////////////////////////////////////////////////////////////////////////

        ///components
        private CapsuleCollider _capsuleCollider;
        private Rigidbody _rigidBody;

        #endregion

        #region Cache ######################################################################################################################

        private RaycastHit[] rayCache = new RaycastHit[4];

        #endregion

        #region Properties ########################################################################################################################

        /// <summary>
        /// The current surface the controller in on
        /// </summary>
        public SurfaceInformations CurrentSurface => _currentSurface;

        /// <summary>
        /// The center of mass of the controller in world space
        /// </summary>
        public Vector3 CenterOfMass { get => (transform.position + _capsuleCollider.center); }

        /// <summary>
        /// The event called when the surface contact changed.
        /// </summary>
        public event EventHandler<SurfaceInformations> OnSurfaceContact;

        //Local Properties ////////////////////////////////////////////////////////////////////////

        private float GravityMagnitude
        {
            get
            {
                if (GravityZone == null)
                    return 0;
                return GravityZone.GravityScale;
            }
        }

        #endregion

        #region Public Functions ############################################################################################################

        public override void Move(Vector3 direction, float speed)
        {
            _userMoveVector = direction.normalized * speed;
        }

        public override void ImpulseForce(float force, Vector3 direction = default)
        {

        }


        /// <summary>
        /// Adjust the shape of the character.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        public void AdjustShape(float height, float width)
        {
            if (!_capsuleCollider)
                return;
            _capsuleCollider.height = height - _stepOffset;
            _capsuleCollider.radius = width;
            _capsuleCollider.center = new Vector3(0, (_capsuleCollider.height) * 0.5f + _stepOffset, 0);
            _surfaceSnapBufferLenght = _stepOffset;
        }

        #endregion

        #region Private Functions ##########################################################################################################

        /// <summary>
        /// Detect ground
        /// </summary>
        /// <returns></returns>
        private SurfaceInformations CheckGround(float delta)
        {
            if (GravityZone == null)
                return SurfaceInformations.NullSurface;

            Vector3 direction = GravityZone.GravityDirection.normalized;
            //Detect ground
            if (_debug)
            {
                PulseDebug.DrawRay(CenterOfMass, direction * (_surfaceSnapBufferLenght + _capsuleCollider.center.y), Color.green);
            }
            //surface distance
            if(!Physics.Raycast(new Ray(CenterOfMass,direction),out var hit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
            {
                return SurfaceInformations.NullSurface;
            }
            _surfaceDistance = (transform.position - hit.point).magnitude;
            //
            float radius = _capsuleCollider.radius * 0.85f;
            int collisions = Physics.SphereCastNonAlloc(new Ray(CenterOfMass, direction), radius
                , rayCache, _surfaceSnapBufferLenght + _capsuleCollider.center.y, _layerMask, QueryTriggerInteraction.Ignore);
            if (collisions > 0)
            {
                if (_debug)
                {
                    for (int i = rayCache.Length - 1; i >= collisions; i--)
                    {
                        if (rayCache[i].normal.sqrMagnitude > 0)
                            PulseDebug.DrawCircle(rayCache[i].point, radius, rayCache[i].normal, Color.white);
                    }
                }
                int closestValidIndex = -1;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < collisions; i++)
                {
                    if (rayCache[i].collider == _capsuleCollider)
                        continue;
                    if (_debug && rayCache[i].normal.sqrMagnitude > 0)
                        PulseDebug.DrawCircle(rayCache[i].point, radius, rayCache[i].normal, Color.yellow);
                    //Get the closest valid raycast from center of mass
                    if (rayCache[i].collider && rayCache[i].AngleFromHorizontal(-direction) <= _slopeAngle)
                    {
                        if (rayCache[i].distance < closestDistance)
                        {
                            closestDistance = rayCache[i].distance;
                            closestValidIndex = i;
                        }
                    }
                }
                if (closestValidIndex.InInterval(0, collisions))
                {
                    if (_debug && rayCache[closestValidIndex].normal.sqrMagnitude > 0)
                        PulseDebug.DrawCircle(rayCache[closestValidIndex].point, radius, rayCache[closestValidIndex].normal, Color.green);
                    Vector3 fullClampForce = Vector3.Project(rayCache[closestValidIndex].point - transform.position, direction);
                    Vector3 noOffsetPoint = CenterOfMass + Vector3.Project(rayCache[closestValidIndex].point - CenterOfMass, direction);
                    float clampForceDirection = Vector3.Dot(fullClampForce, direction);
                    return new SurfaceInformations
                    {
                        surfaceCollider = rayCache[closestValidIndex].collider,
                        Point = rayCache[closestValidIndex].point,
                        PointNoOffset = noOffsetPoint,
                        PointLocalSurfaceSpace = rayCache[closestValidIndex].transform.InverseTransformPoint(transform.position),
                        Normal = rayCache[closestValidIndex].normal,
                        SurfaceClampForce = clampForceDirection > 0.1f ? (fullClampForce / (delta * (1 / _snapDownForceMultiplier))) : (fullClampForce / (delta * (1 / _snapUpForceMultiplier))),
                        IsOnSurface = true,
                        Angle = rayCache[closestValidIndex].AngleFromHorizontal(-direction),
                        SurfaceType = SurfaceType.Ground,
                    };
                }
            }
            return SurfaceInformations.NullSurface;
        }

        /// <summary>
        /// Select a priority suface among multiples detected.
        /// </summary>
        /// <param name="surfaces"></param>
        /// <returns></returns>
        private Vector3 SelectSurface(bool updateSurfaceLocals, params SurfaceInformations[] surfaces)
        {
            var lastSurface = _currentSurface.surfaceCollider;
            if (surfaces == null || surfaces.Length <= 0)
            {
                //It must be an error. check call stack for the call of this method
                if (_debug) { PulseDebug.Log($"[Character controller] - {name} doesn't found any suface, check call stack for the call of this method"); }
                _currentSurface = default;
                _currentPhysicSpace = PhysicSpace.inAir;
                PlatformSwap(null, lastSurface?.transform);
                return Vector3.zero;
            }
            var validSurfaces = surfaces.Where(s => s.surfaceCollider && s.SurfaceType != SurfaceType.none).ToList();
            if (validSurfaces.Count <= 0)
            {
                //The character is probably in air
                _currentSurface = default;
                _currentPhysicSpace = PhysicSpace.inAir;
                PlatformSwap(null, lastSurface?.transform);
                return Vector3.zero;
            }
            validSurfaces.Sort((a, b) => { return ((int)a.SurfaceType).CompareTo((int)b.SurfaceType); });
            var surface = validSurfaces.FirstOrDefault();
            _currentSurface = validSurfaces.FirstOrDefault();
            _currentSurface.lastSurfaceCollider = lastSurface;
            Vector3 outVelocity = _currentSurface.SurfaceClampForce;

            //Set the current physic state
            switch (_currentSurface.SurfaceType)
            {
                case SurfaceType.Ground:
                    _currentPhysicSpace = PhysicSpace.onGround;
                    break;
                case SurfaceType.Wall:
                    _currentPhysicSpace = PhysicSpace.onWall;
                    break;
                case SurfaceType.ceilling:
                    _currentPhysicSpace = PhysicSpace.unSpecified;
                    break;
            }

            if (_currentSurface.surfaceCollider && _currentSurface.surfaceCollider.TryGetComponent<MovingPlatform3D>(out var platform))
            {
                transform.rotation *= platform.UpdatePositionAndGetRotation(transform, transform.position, updateSurfaceLocals);
                outVelocity += platform.RelativeVelocity(transform);
            }
            if (_currentSurface.surfaceCollider != _currentSurface.lastSurfaceCollider)
            {
                PlatformSwap(_currentSurface.surfaceCollider?.transform, lastSurface?.transform);
            }
            return outVelocity;
        }

        /// <summary>
        /// Detach from the last platform detected.
        /// </summary>
        private void PlatformSwap(Transform newPlatform, Transform oldPlatform)
        {
            if (oldPlatform)
            {
                if (oldPlatform.TryGetComponent<MovingPlatform3D>(out var platform))
                {
                    platform.TakeOff(transform);
                }
            }
            OnSurfaceContact?.Invoke(this, _currentSurface);
        }

        /// <summary>
        /// Compute user input velocity
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector3 ComputeUserMovement(Vector3 normal, float delta)
        {
            if (_userMoveVector.sqrMagnitude > 0 && normal.sqrMagnitude > 0)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(_userMoveVector, normal.normalized)), delta * 15);
            return _userMoveVector;
        }

        /// <summary>
        /// Compute gravity force
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector3 ComputeGravity(float delta)
        {
            _airTime += delta;
            if (_currentSurface.IsOnSurface)
                _airTime = 0;
            _gravityAcc += delta;
            if (_currentSurface.IsOnSurface) //Replace by surface stable
                _gravityAcc = 0;
            Vector3 gravityNoScale = (GravityZone != null ? GravityZone.GravityDirection : Vector3.zero) * GravityMagnitude;
            Vector3 gravity = gravityNoScale * (_gravityAcc > 0? Mathf.Clamp(_gravityAcc * _gravityAcc, 0.66f, float.MaxValue) : _gravityAcc);

            //Calculate slide
            //if ((_currentSurface.PointOffset > _controller.radius * INNER_RADIUS_DETECTION_SCALE))
            //{
            //    if (!_currentSurface.NoGravityForce)
            //    {
            //        _gravityAcc = Mathf.Clamp(_gravityAcc, 0, 0.25f);
            //        gravity = Vector3.ProjectOnPlane(gravityNoScale * _gravityAcc, _currentSurface.NormalLarge);
            //    }
            //}
            return Vector3.ClampMagnitude(gravity, 250);
        }

        /// <summary>
        /// Apply movement to the controller
        /// </summary>
        /// <param name="move"></param>
        private void MoveController(Vector3 move, Quaternion angularVelocity, float delta)
        {
            if (_rigidBody == null)
                return;
            _rigidBody.AddForce(move - _rigidBody.velocity, ForceMode.VelocityChange);
        }

        #endregion

        #region Jobs      ##########################################################################################################################

        #endregion

        #region MonoBehaviours ################################################################################################################

        private void OnEnable()
        {
            if (_capsuleCollider == null)
            {
                _capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                if (!_debug)
                    _capsuleCollider.hideFlags = HideFlags.HideInInspector;
            }
            AdjustShape(_height, _width);
            if (_rigidBody == null)
            {
                _rigidBody = gameObject.AddComponent<Rigidbody>();
                _rigidBody.useGravity = false;
                _rigidBody.freezeRotation = true;
                if (!_debug)
                    _rigidBody.hideFlags = HideFlags.HideInInspector;
            }
            //Get all child colliders
            _childColliders3D.Clear();
            var allColls = GetComponentsInChildren<Collider>();
            for (int i = 0; i < allColls.Length; i++)
            {
                if (allColls[i].transform == transform)
                    continue;
                _childColliders3D.Add(allColls[i]);
            }
        }

        public void FixedUpdate()
        {
            float delta = Time.fixedDeltaTime;
            Vector3 finalVel = Vector3.zero;
            Quaternion angularVel = Quaternion.Euler(Vector3.zero);
            _currentPhysicSpace = PhysicSpace.unSpecified;
            if (GravityZone == null)
                GravityZone = GravityZone3d.Earth;
            GravityZone.UpdateZone(this);

            //Prevent inner collision ****************************************************************************************
            InnerCollision3DState(_capsuleCollider, false);

            //Physic related evaluations *************************************************************************************
            var ground = CheckGround(delta);

            //Surface selection **********************************************************************************************
            finalVel += SelectSurface(_userMoveVector.sqrMagnitude > 0, ground);

            //Compute motion *************************************************************************************************
            finalVel += ComputeUserMovement(transform.up, delta);
            finalVel += ComputeGravity(delta);

            //Apply motion ***************************************************************************************************
            MoveController(finalVel, angularVel, delta);

            //Calculate drag *************************************************************************************************
        }

        #endregion
    }
}