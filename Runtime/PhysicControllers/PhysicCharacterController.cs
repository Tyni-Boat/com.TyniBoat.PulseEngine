using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace PulseEngine.CharacterControl
{

    public class PhysicCharacterController : BaseCharacterController
    {
        #region Variables ##########################################################################################################################

        [Header("Shape Configs")]
        [SerializeField] private float _height = 1.8f;
        [SerializeField] private float _width = 0.37f;
        [SerializeField] private float _stepOffset = 0.25f;
        [SerializeField] private float _slopeAngle = 60;
        [SerializeField][Range(0, 1)] private float _detectionRadiusScale = 1;

        [Header("Forces Configs")]
        [SerializeField] private float _maximumMomentumForce = 100;
        [SerializeField] private float _momentumDecceleration = 10;

        //privates ////////////////////////////////////////////////////////////////////////////////////////

        private float _rotationSpeed = 15;

        ///components
        private Collider _collider;
        private Rigidbody _rigidBody;

        #endregion

        #region Cache ######################################################################################################################

        private RaycastHit[] _rayCache = new RaycastHit[8];

        #endregion

        #region Properties ########################################################################################################################

        /// <summary>
        /// The current surface the controller in on
        /// </summary>
        public SurfaceInformations CurrentSurface => _currentSurface;

        /// <summary>
        /// The center of mass of the controller in world space
        /// </summary>
        public Vector3 CenterOfMass { get => (transform.position + _collider.GetColliderCenter()); }

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

        public override void Move(Vector3 direction, float moveSpeed, float rotationSpeed)
        {
            _rotationSpeed = rotationSpeed;
            _userMoveVector = direction.normalized * moveSpeed;
        }

        public override void AddConstantForce(float force, Vector3 direction = default)
        {
            _momemtum += direction.normalized * force;
            Vector3.ClampMagnitude(_momemtum, _maximumMomentumForce);
        }

        public override float JumpForHeight(float height)
        {
            if (GravityZone == null)
                return 0;
            if (_noChechSurfaceTime > 0)
                return 0;
            _gravityAcc = 0;
            float Voy = Mathf.Sqrt(2 * GravityMagnitude * (height - 0));
            float fTime = Voy / GravityMagnitude;
            _userImpulsedForce = -GravityZone.GravityDirection.normalized * Voy + _momemtum;
            _noChechSurfaceTime = fTime * 0.25f;
            OnJumpTakeOff(_userImpulsedForce);
            return fTime;
        }

        public override Vector2 JumpTo(Vector3 point, float height)
        {
            if (GravityZone == null)
                return Vector2.zero;
            if (_noChechSurfaceTime > 0)
                return Vector2.zero;
            _gravityAcc = 0;
            Vector3 dir = Vector3.ProjectOnPlane(point - transform.position, transform.up);
            Vector3 hDiff = Vector3.ProjectOnPlane(point - transform.position, dir);
            float h = hDiff.magnitude * Mathf.Sign(Vector3.Dot(-transform.up, hDiff));
            float Voy = Mathf.Sqrt(Mathf.Clamp(2 * GravityMagnitude * (height - h), 0, float.MaxValue));
            float Vox = (dir.magnitude * GravityMagnitude) / (Voy + Mathf.Sqrt(Mathf.Pow(Voy, 2) + (2 * GravityMagnitude * h)));
            float fTime = Voy / GravityMagnitude;
            float pTime = (Voy + Mathf.Sqrt(Mathf.Pow(Voy, 2) + (2 * GravityMagnitude * h))) / GravityMagnitude;

            _userImpulsedForce = -GravityZone.GravityDirection.normalized * Voy + dir.normalized * Vox;
            _noChechSurfaceTime = fTime * 0.25f;
            OnJumpTakeOff(_userImpulsedForce);
            return new Vector2(fTime, pTime);
        }

        public override void IgnoreCollisionMode(bool value, bool activeCollisionBetweenChildren = false)
        {
            _innerCollisions = activeCollisionBetweenChildren;
            _ignoreCollision = value;
        }


        public override void RefreshChildrenColliders()
        {
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

        public override void AdjustShape(float height, float width, float stepOffset)
        {
            if (!_collider)
                return;
            _height = height;
            _width = width;
            _stepOffset = stepOffset;
            //Internal_AdjustShape(height, width);
        }

        #endregion

        #region Private Functions ##########################################################################################################

        /// <summary>
        /// Compute the current max slope angle depending on the collider with.
        /// </summary>
        private void SlopeAngleCalculus()
        {
            float r = _collider.GetColliderWidth() * _detectionRadiusScale;
            float hypo = Mathf.Sqrt((r * r) + (_stepOffset * _stepOffset));
            float sin = r / hypo;
            _slopeAngle = 90 - Mathf.Asin(sin) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Handle the update of the gravity zone.
        /// </summary>
        private void GravityZoneHandling()
        {
            if (GravityZone == null)
                GravityZone = GravityZone3d.Earth;
            GravityZone.UpdateZone(this);
        }

        /// <summary>
        /// Detect ground
        /// </summary>
        /// <returns></returns>
        private SurfaceInformations CheckGround(float delta)
        {
            if (GravityZone == null)
                return SurfaceInformations.NullSurface;

            Vector3 direction = GravityZone.GravityDirection.normalized;
            //Checking Distance ************************************************************************************************************

            if (!Physics.Raycast(new Ray(transform.position - direction.normalized * _stepOffset * 0.999f, direction), out var hit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
            {
                return SurfaceInformations.NullSurface;
            }
            _surfaceDistance = (transform.position - hit.point).magnitude;
            if (_noChechSurfaceTime > 0)
                return SurfaceInformations.NullSurface;

            //Detect ground ****************************************************************************************************************

            float radius = _collider.GetColliderWidth() * _detectionRadiusScale;
            float distance = (_surfaceSnapBufferLenght + _collider.GetColliderCenter().y) - radius;
            if (_debug)
            {
                PulseDebug.DrawTwoPointsCurvedCylinder(CenterOfMass, CenterOfMass + direction * (distance + radius)
                    , radius, direction, direction, Color.white, Color.white, 45, 2, false, true, true);
            }
            int collisions = Physics.SphereCastNonAlloc(new Ray(CenterOfMass, direction), radius, _rayCache, distance, _layerMask, QueryTriggerInteraction.Ignore);
            if (collisions > 0)
            {
                int closestValidIndex = -1;
                float closestDistance = float.MaxValue;
                Vector3 cumulativeNormal = Vector3.zero;
                for (int i = 0; i < collisions; i++)
                {
                    if (_rayCache[i].collider == _collider)
                        continue;
                    if (_rayCache[i].collider)
                    {
                        cumulativeNormal += _rayCache[i].normal;
                        if (_debug && _rayCache[i].normal.sqrMagnitude > 0)
                            PulseDebug.DrawCircle(_rayCache[i].point, radius, _rayCache[i].normal, Color.white);
                    }
                    //Get the closest valid raycast from center of mass
                    if (_rayCache[i].collider && _rayCache[i].AngleFromHorizontal(-direction) <= _slopeAngle)
                    {
                        if (_rayCache[i].distance < closestDistance)
                        {
                            closestDistance = _rayCache[i].distance;
                            closestValidIndex = i;
                        }
                    }
                }
                //no choice, take closest surface found
                if (!closestValidIndex.InInterval(0, collisions))
                {
                    cumulativeNormal = Vector3.zero;
                    List<RaycastHit> sortedList = new List<RaycastHit>(_rayCache).Where(r => r.collider).ToList();
                    foreach (var r in sortedList) cumulativeNormal += r.normal;
                    sortedList.Sort((a, b) => a.distance.CompareTo(b.distance));
                    var ray = sortedList[0];
                    for (int i = 0; i < _rayCache.Length; i++)
                    {
                        if (_rayCache[i].collider == ray.collider
                            && _rayCache[i].point == ray.point
                            && _rayCache[i].normal == ray.normal
                            && _rayCache[i].distance == ray.distance)
                        {
                            closestValidIndex = i;
                            break;
                        }
                    }
                }
                if (closestValidIndex.InInterval(0, collisions))
                {
                    if (_debug && _rayCache[closestValidIndex].normal.sqrMagnitude > 0)
                        PulseDebug.DrawCircle(_rayCache[closestValidIndex].point, radius, _rayCache[closestValidIndex].normal, Color.green);
                    float angle = _rayCache[closestValidIndex].AngleFromHorizontal(-direction);
                    float cumulatedAngle = hit.AngleFromHorizontal(-direction);
                    Vector3 noOffsetPoint = CenterOfMass + Vector3.Project(_rayCache[closestValidIndex].point - CenterOfMass, direction);
                    Vector3 clampPt = noOffsetPoint + direction.normalized * Mathf.Sin(angle * Mathf.Deg2Rad) * (_stepOffset * _nonFlatGroundPenetrationFactor);
                    Vector3 fullClampForce = Vector3.Project(clampPt - transform.position, direction);
                    float clampForceDirection = Vector3.Dot(fullClampForce, direction);
                    return new SurfaceInformations
                    {
                        surfaceCollider = _rayCache[closestValidIndex].collider,
                        Point = _rayCache[closestValidIndex].point,
                        PointNoOffset = noOffsetPoint,
                        PointLocalSurfaceSpace = _rayCache[closestValidIndex].transform.InverseTransformPoint(transform.position),
                        Normal = _rayCache[closestValidIndex].normal,
                        SurfaceClampForce = clampForceDirection > 0.1f ? (fullClampForce / (delta * (1 / _snapDownForceMultiplier))) : (fullClampForce / (delta * (1 / _snapUpForceMultiplier))),
                        IsOnSurface = true,
                        IsSurfaceStable = angle < _slopeAngle,
                        Distance = _rayCache[closestValidIndex].distance,
                        Angle = angle,
                        AngleDetection = cumulatedAngle,
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
        private Vector3 SelectSurface(float delta, bool updateSurfaceLocals, params SurfaceInformations[] surfaces)
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
            //void any jump force
            _userImpulsedForce = Vector3.zero;
            _noChechSurfaceTime = 0;

            if (_currentSurface.surfaceCollider)
            {
                if (_currentSurface.surfaceCollider.TryGetComponent<MovingPlatform3D>(out var platform))
                {
                    transform.rotation *= platform.UpdatePositionAndGetRotation(transform, transform.position, updateSurfaceLocals);
                    outVelocity += platform.RelativeVelocity(transform);
                }
                _currentSurface.surfaceCollider.attachedRigidbody?.AddForceAtPosition(-_currentSurface.Normal * _rigidBody.mass * delta, _currentSurface.PointNoOffset);
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
            else if (newPlatform)
            {
                if (newPlatform.gameObject.TryGetComponent<Rigidbody>(out var rig))
                {
                    rig.AddForceAtPosition(_rigidBody.velocity, _currentSurface.PointNoOffset, ForceMode.Impulse);
                }
            }
            OnSurfaceContact?.Invoke(this, _currentSurface);
        }

        /// <summary>
        /// Acion made when taking off from a jump
        /// </summary>
        private void OnJumpTakeOff(Vector3 jumpForce)
        {
            if (_currentSurface.SurfaceType == SurfaceType.none)
                return;
            if (_currentSurface.surfaceCollider == null)
                return;
            _currentSurface.surfaceCollider.attachedRigidbody?.AddForceAtPosition(-jumpForce, _currentSurface.PointNoOffset, ForceMode.Impulse);
        }

        /// <summary>
        /// Compute user input velocity
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector3 ComputeUserMovement(Vector3 normal, float delta)
        {
            if (_userMoveVector.sqrMagnitude > 0 && normal.sqrMagnitude > 0)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(_userMoveVector, normal.normalized)), delta * _rotationSpeed);
            return _userMoveVector;
        }

        /// <summary>
        /// Compute external forces velocities
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector3 ComputeExternalForces(float delta)
        {
            return _momemtum;
        }

        /// <summary>
        /// Compute user's Jump force
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector3 ComputeUserJumpForce(float delta)
        {
            return _userImpulsedForce;
        }

        /// <summary>
        /// Compute drag for momentum force
        /// </summary>
        /// <param name="delta"></param>
        private void ComputeDrag(float delta)
        {
            if (_momemtum.sqrMagnitude > 0)
            {
                _momemtum = Vector3.Lerp(_momemtum, Vector3.zero, delta * _momentumDecceleration);
            }
        }

        /// <summary>
        /// Adjust the shape of the character.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        private void Internal_AdjustShape(float height, float width, float delta)
        {
            if (!_collider)
                return;
            _collider.SetColliderHeight(Mathf.Lerp(_collider.GetColliderHeight(), height - _stepOffset, delta * 25));
            _collider.SetColliderWidth(Mathf.Lerp(_collider.GetColliderWidth(), width, delta * 25));
            _collider.SetColliderCenter(new Vector3(0, Mathf.Clamp((_collider.GetColliderHeight()) * 0.5f, _collider.GetColliderWidth(), float.MaxValue) + _stepOffset, 0));
            _surfaceSnapBufferLenght = _stepOffset;
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
            if (_currentSurface.IsSurfaceStable)
                _gravityAcc = 0;
            Vector3 gravityNoScale = (GravityZone != null ? GravityZone.GravityDirection : Vector3.zero) * GravityMagnitude;
            Vector3 gravity = gravityNoScale * _gravityAcc;

            //Calculate slide
            if (_currentSurface.Angle >= _slopeAngle && _currentSurface.IsOnSurface && _currentSurface.SurfaceType == SurfaceType.Ground)
            {
                gravity = Vector3.ProjectOnPlane(gravityNoScale * _gravityAcc, _currentSurface.Normal);
            }
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
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CapsuleCollider>();
                if (!_debug)
                    _collider.hideFlags = HideFlags.HideInInspector;
            }
            if (_rigidBody == null)
            {
                _rigidBody = gameObject.AddComponent<Rigidbody>();
                _rigidBody.useGravity = false;
                _rigidBody.freezeRotation = true;
                _rigidBody.mass = 80;
                if (!_debug)
                    _rigidBody.hideFlags = HideFlags.HideInInspector;
            }
            RefreshChildrenColliders();
        }

        public void FixedUpdate()
        {
            float delta = Time.fixedDeltaTime;
            Vector3 finalVel = Vector3.zero;
            Quaternion angularVel = Quaternion.Euler(Vector3.zero);
            _currentPhysicSpace = PhysicSpace.unSpecified;
            if (_noChechSurfaceTime > 0)
                _noChechSurfaceTime -= delta;
            _collider.isTrigger = _ignoreCollision;
            Internal_AdjustShape(_height, _width, delta);
            GravityZoneHandling();
            SlopeAngleCalculus();

            //Prevent inner collision ****************************************************************************************
            InnerCollision3DState(_collider, !_innerCollisions);

            //Surfaces evaluations *************************************************************************************
            var ground = CheckGround(delta);

            //Surface selection **********************************************************************************************
            finalVel += SelectSurface(delta, _userMoveVector.sqrMagnitude > 0, ground);

            //Compute motion *************************************************************************************************
            finalVel += ComputeUserMovement(transform.up, delta);
            finalVel += ComputeUserJumpForce(delta);
            finalVel += ComputeExternalForces(delta);
            finalVel += ComputeGravity(delta);

            //Apply motion ***************************************************************************************************
            MoveController(finalVel, angularVel, delta);

            //Calculate drag *************************************************************************************************
            ComputeDrag(delta);
        }

        private void OnDisable()
        {
            InnerCollision3DState(_collider, false);
        }

        #endregion
    }
}