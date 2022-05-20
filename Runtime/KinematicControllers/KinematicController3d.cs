using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.CharacterControl
{
    /// <summary>
    /// Control Characters Movements in a kinematic way.
    /// </summary>
    public class KinematicController3d : PulseComponent
    {
        #region Constants ##########################################################################################################################

        const float MAX_SURFACE_DETECTION_DISTANCE = 25;
        const float NO_GRAVITY_SURFACE_DIST = 0.05f;
        const float INNER_RADIUS_DETECTION_SCALE = 0.75f;

        #endregion

        #region Variables ##########################################################################################################################

        [Header("Configs")]
        [SerializeField] private LayerMask _layerMask = 1;
        [SerializeField] private float _groundedSurfaceDist = 0.5f;
        [SerializeField] private float _gravityScale = 5f;
        [Space]
        [Header("State and Alterations")]
        [SerializeField] private PhysicSpace _currentPhysicSpace;
        [SerializeField] private bool _ignoreCollision;
        [Space]
        [Header("State and Alterations")]
        [SerializeField] private bool _debug;

        //privates ////////////////////////////////////////////////////////////////////////////////////////

        ///components
        private CharacterController _controller;
        private Transform _posMarker;

        ///surface
        private SurfaceInfos _currentSurface = new SurfaceInfos();
        Vector3 _lastSurfacePos = Vector3.zero;
        Quaternion _lastSurfaceRot = Quaternion.identity;

        ///motion
        private Vector3 _userMoveVector;
        private Vector3 _userJumpVector;
        private Vector3 _momemtum;
        private float _airTime;
        private float _surfaceDistance;
        private float _gravityAcc;
        private float _jumpRequestTimeToPeak;
        [SerializeField] private float _surfaceSnapSpeed = 50;

        ///State
        private bool _crouchState;
        private float _charHeightMax = 1.8f;
        private float _charHeightMin = 1f;
        private float _charHeightCurrent = 1.8f;
        private float _charWidth = 0.37f;
        private float _charStepOffset = 0.25f;
        private float _charSlopeAngle = 60;

        #endregion

        #region Cache   ##########################################################################################################################

        private Collider[] _colliderCache = new Collider[8];

        private List<Collider> _childColliders = new List<Collider>();

        #endregion

        #region Inner Types ######################################################################################################################

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
        /// The current surface the controller in on
        /// </summary>
        public SurfaceInfos CurrentSurface => _currentSurface;

        /// <summary>
        /// The normal user for planar movement.
        /// </summary>
        public Vector3 MovementNormal { get; private set; }

        /// <summary>
        /// Adjust the scale of the object gravity
        /// </summary>
        public float GravityScale { get => _gravityScale; set => _gravityScale = value; }

        /// <summary>
        /// The center of mass of the controller in world space
        /// </summary>
        public Vector3 CenterOfMass { get => (transform.position + _controller.center); }

        /// <summary>
        /// The surface distance
        /// </summary>
        public float SurfaceDistance { get => _surfaceDistance; }

        /// <summary>
        /// Time Spend in the air.
        /// </summary>
        public float AirTime { get => _airTime; }

        /// <summary>
        /// Is the controller in crouched state?
        /// </summary>
        public bool IsCrouch { get; private set; }

        /// <summary>
        /// The event called when the surface contact changed.
        /// </summary>
        public event EventHandler<bool> OnSurfaceContactChanged;

        //Local Properties ////////////////////////////////////////////////////////////////////////

        private float GravityMagnitude { get => Physics.gravity.magnitude * _gravityScale; }

        #endregion

        #region Public Functions ############################################################################################################

        /// <summary>
        /// Make controller move base on inputs direction.
        /// </summary>
        /// <param name="inputs"></param>
        public void LookFromInputs(Transform cameraTr, Vector2 inputs, float turnSpeed)
        {
            if (!cameraTr)
                return;
            Vector3 cameraBasedDirection = (cameraTr.forward * inputs.y + cameraTr.right * inputs.x);
            Vector3 projectedDirection = Vector3.ProjectOnPlane(cameraBasedDirection, transform.up);
            ApplyMovement(projectedDirection, turnSpeed, true);
        }

        /// <summary>
        /// Apply force to the controller
        /// </summary>
        /// <param name="worldDirection"></param>
        /// <param name="magnitude"></param>
        public void ApplyMovement(Vector3 worldDirection, float rotateTowardSpeed = 0, bool rotateOnly = false, bool overrideMagnituderestriction = false)
        {
            if (rotateTowardSpeed > 0)
            {
                if (worldDirection.sqrMagnitude > 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(worldDirection.normalized, transform.up)), Time.deltaTime * rotateTowardSpeed);
            }
            if (!rotateOnly)
            {
                if (overrideMagnituderestriction || worldDirection.sqrMagnitude > 0)
                    _userMoveVector = worldDirection;
            }
        }

        /// <summary>
        /// Reduce COntroller height
        /// </summary>
        /// <param name="value"></param>
        public void Crouch(bool value)
        {
            _crouchState = value;
        }

        /// <summary>
        /// Make the controller ignore collision or not.
        /// </summary>
        /// <param name="value"></param>
        public void NoClipState(bool value)
        {
            _ignoreCollision = value;
        }

        /// <summary>
        /// Jump to reach a specific peak height
        /// </summary>
        /// <param name="height"></param>
        /// <returns>Get the time needed to reach the peak height</returns>
        public float JumpForHeight(float height)
        {
            _gravityAcc = 0;
            float Voy = Mathf.Sqrt(2 * GravityMagnitude * (height - 0));
            float fTime = Voy / GravityMagnitude;
            _userJumpVector = -Physics.gravity.normalized * Voy + _momemtum;
            OnJump(_userJumpVector);
            _jumpRequestTimeToPeak = fTime;
            return fTime;
        }

        /// <summary>
        /// Jump from the current position to a point
        /// </summary>
        /// <param name="point">Location where to try to reach</param>
        /// <param name="height">Peak height of the jump</param>
        /// <returns>return the peak height time as X and the total jump time as Y</returns>
        public Vector2 JumpTo(Vector3 point, float height)
        {
            _gravityAcc = 0;
            Vector3 dir = Vector3.ProjectOnPlane(point - transform.position, transform.up);
            Vector3 hDiff = Vector3.ProjectOnPlane(point - transform.position, dir);
            float h = hDiff.magnitude * Mathf.Sign(Vector3.Dot(-transform.up, hDiff));
            float Voy = Mathf.Sqrt(Mathf.Clamp(2 * GravityMagnitude * (height - h), 0, float.MaxValue));
            float Vox = (dir.magnitude * GravityMagnitude) / (Voy + Mathf.Sqrt(Mathf.Pow(Voy, 2) + (2 * GravityMagnitude * h)));
            float fTime = Voy / GravityMagnitude;
            float pTime = (Voy + Mathf.Sqrt(Mathf.Pow(Voy, 2) + (2 * GravityMagnitude * h))) / GravityMagnitude;

            _userJumpVector = -Physics.gravity.normalized * Voy + dir.normalized * Vox;
            OnJump(_userJumpVector);
            _jumpRequestTimeToPeak = fTime;
            return new Vector2(fTime, pTime);
        }

        public void AdjustShape(float height, float width)
        {
            if (!_controller)
                return;
            _charHeightMax = height;
            _charWidth = width;
            _controller.height = _charHeightMax;
            _controller.radius = _charWidth;
        }

        #endregion

        #region Private Functions ##########################################################################################################

        private void CheckSurface(Vector3 direction, float delta, Action<bool> OnSurfaceDetection = null, Action<bool> OnSurfaceContact = null)
        {
            if (_currentPhysicSpace != PhysicSpace.unSpecified)
                return;

            bool wereOnSurface = _currentSurface.IsOnSurfaceLarge;
            bool wereOnSurfaceContact = _currentSurface.NoGravityForce;

            Vector3 pt_medium = transform.position;
            Vector3 pt_large = transform.position;
            Vector3 normal_medium = -direction.normalized;
            Vector3 normal_large = -direction.normalized;
            Vector3 normal_small = direction.normalized;
            Vector3 offsetVec = Vector3.zero;
            Vector3 offsetNormal = -transform.up;
            float charHeight = _charHeightCurrent * 0.5f;
            float maxDistance = MAX_SURFACE_DETECTION_DISTANCE;
            Collider surfaceCol_medium = null;
            Collider surfaceCol_large = null;
            Collider surfaceCol_small = null;

            //Heights
            if (Physics.SphereCast(CenterOfMass, _controller.radius, direction, out var heightHit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
            {
                maxDistance = heightHit.distance - charHeight;

                //large
                if (Physics.SphereCast(CenterOfMass, _controller.radius, direction, out var bigHit, charHeight, _layerMask, QueryTriggerInteraction.Ignore))
                {
                    pt_large = bigHit.point;
                    normal_large = bigHit.normal;
                    surfaceCol_large = bigHit.collider;
                    offsetVec = Vector3.ProjectOnPlane(bigHit.point - CenterOfMass, -direction);

                    //Medium
                    if (Physics.SphereCast(CenterOfMass, _controller.radius * INNER_RADIUS_DETECTION_SCALE, direction, out var mediumHit, charHeight, _layerMask, QueryTriggerInteraction.Ignore))
                    {
                        pt_medium = mediumHit.point;
                        normal_medium = mediumHit.normal;
                        surfaceCol_medium = mediumHit.collider;


                        //Central Point
                        if (mediumHit.collider.Raycast(new Ray(CenterOfMass, direction), out var centralHit, MAX_SURFACE_DETECTION_DISTANCE))
                        {
                            surfaceCol_small = centralHit.collider;
                            normal_small = centralHit.normal;
                        }
                    }

                    //offset
                    if (bigHit.collider.Raycast(new Ray(CenterOfMass + offsetVec, direction), out var offsetHit, (_controller.height / 2) + 0.001f))
                    {
                        offsetNormal = offsetHit.normal;
                    }
                }
            }

            //Evaluate step height
            Vector3 ptOnCollider = _controller.PointOnSurface(pt_medium);
            Vector3 centerProj = Vector3.Project((ptOnCollider - CenterOfMass), -transform.up);
            Vector3 ptToStepTo = CenterOfMass + centerProj;
            float distance = surfaceCol_medium ? (pt_medium - transform.position).magnitude * Mathf.Sign(Vector3.Dot(direction, pt_medium - transform.position))
                : float.MaxValue;

            //Height from surface
            _surfaceDistance = maxDistance;

            //Larges
            _currentSurface.IsOnSurfaceLarge = surfaceCol_large;
            _currentSurface.NormalLarge = normal_large;
            _currentSurface.LargeAngle = Mathf.Acos(Vector3.Dot(-direction, normal_large)) * Mathf.Rad2Deg;

            //switching colliders
            _currentSurface.lastSurfaceCollider = _currentSurface.surfaceCollider;
            _currentSurface.surfaceCollider = surfaceCol_medium;

            //mediums
            _currentSurface.Point = pt_medium;
            _currentSurface.Normal = normal_medium;

            //
            _currentSurface.TrueNormal = normal_small;
            _currentSurface.CentralNormal = normal_small;
            _currentSurface.OffsetNormal = offsetNormal;
            _currentSurface.PointOffset = offsetVec.magnitude;

            //
            _currentSurface.Distance = distance;
            _currentSurface.InnerAngle = Mathf.Acos(Vector3.Dot(-direction, normal_medium)) * Mathf.Rad2Deg;

            //
            _currentSurface.IsOnSurface = distance <= (_controller.skinWidth * 1.01f) && surfaceCol_medium;
            _currentSurface.IsSurfaceStable = _currentSurface.IsOnSurface && Vector3.Dot(-direction, normal_small) >= Mathf.Cos(_charSlopeAngle);
            _currentSurface.IsWalkableStep = !surfaceCol_small && surfaceCol_medium;
            _currentSurface.NoGravityForce = _currentSurface.IsSurfaceStable || _currentSurface.IsWalkableStep;

            _currentPhysicSpace = _currentSurface.IsOnSurface ? PhysicSpace.onGround : _currentPhysicSpace;

            //detect surface events
            if (wereOnSurface != _currentSurface.IsOnSurfaceLarge)
            {
                OnSurfaceDetection?.Invoke(_currentSurface.IsOnSurfaceLarge);
            }
            //detect contact events
            if (wereOnSurfaceContact != _currentSurface.NoGravityForce)
            {
                OnSurfaceContact?.Invoke(_currentSurface.NoGravityForce);
            }

            //Debug
            if (_debug)
            {
                PulseDebug.DrawCircle(pt_medium, 0.15f, _currentSurface.Normal, Color.red);
                PulseDebug.DrawCircle(pt_large, 0.1f, _currentSurface.OffsetNormal, Color.magenta);
                PulseDebug.DrawCircle(ptOnCollider, 0.1f, _currentSurface.Normal, Color.yellow);
                PulseDebug.DrawCircle(_posMarker.position, 0.2f, _currentSurface.TrueNormal, Color.blue);
                PulseDebug.DrawCircle(pt_large, 0.55f, _currentSurface.NormalLarge, Color.white);
            }
        }
        private void OnSurfaceDetected(bool surfaceDetected)
        {
            //Surface contact state changed
            if (surfaceDetected)
            {
                //Nullify any jump force 
                if (_gravityAcc > 0.15f)
                    _userJumpVector = Vector3.zero;
            }
        }
        private void OnSurfaceContact(bool surfaceContact)
        {
            //Surface contact state changed
            if (surfaceContact)
            {
                _userJumpVector = Vector3.zero;
                //landing
                if (_currentSurface.surfaceCollider && _currentSurface.NoGravityForce && _currentSurface.IsOnSurface && _controller)
                {
                    _currentSurface.surfaceCollider.attachedRigidbody?.WakeUp();
                    _currentSurface.surfaceCollider.attachedRigidbody?.AddForceAtPosition(_controller.velocity, transform.position, ForceMode.Force);
                }
            }
            else
            {
                //Take off
                if (_jumpRequestTimeToPeak > 0)
                {
                    _jumpRequestTimeToPeak = 0;
                }
            }
            OnSurfaceContactChanged?.Invoke(this, surfaceContact);
        }
        private void OnSurfaceOps(ref Vector3 moveVector, ref Quaternion rotation)
        {
            //Detect surface changes
            bool detection = Physics.SphereCast(transform.position + transform.up * _charStepOffset, 0, -transform.up, out var mediumHit, _charStepOffset * 2, _layerMask, QueryTriggerInteraction.Ignore);
            if (!detection)
            {
                //switch surface
                _posMarker?.SetParent(transform);
                _posMarker.position = transform.position;
                _posMarker.rotation = transform.rotation;
                _lastSurfacePos = _posMarker.position;
                _lastSurfaceRot = _posMarker.rotation;
                moveVector = Vector3.zero;
                return;
            }
            _posMarker?.SetParent(mediumHit.transform);
            //Follow movement of surface
            Vector3 m = _posMarker.position - _lastSurfacePos;
            moveVector = m;
            if (_lastSurfaceRot != _posMarker.rotation)
            {
                Vector3 rDiff = (_posMarker.rotation.eulerAngles - _lastSurfaceRot.eulerAngles);
                rDiff.x = rDiff.z = 0;
                var childRot = Quaternion.Euler(rDiff);
                rotation *= childRot;
            }
            if (_userMoveVector.sqrMagnitude > 0)
                _posMarker.position = mediumHit.point;

            //set values
            _lastSurfacePos = _posMarker.position;
            _lastSurfaceRot = _posMarker.rotation;
        }
        private bool SelfCollideOnly(Collider[] collection)
        {
            bool foundSelf = false;
            for (int i = 0; i < collection.Length; i++)
            {
                if (collection[i] == null)
                    continue;
                if (collection[i].transform != transform)
                    return false;
                else
                    foundSelf = true;
            }
            return foundSelf;
        }
        private Vector3 ComputeInputVelocity(float delta)
        {
            Vector3 clamp = _userMoveVector;
            MovementNormal = transform.up;
            if (_currentSurface.IsSurfaceStable)
            {
                MovementNormal = _currentSurface.CentralNormal;
            }
            clamp = Vector3.ProjectOnPlane(_userMoveVector, MovementNormal);
            if (_jumpRequestTimeToPeak > 0)
            {
                _jumpRequestTimeToPeak -= delta;
                if (_jumpRequestTimeToPeak <= 0 && _currentPhysicSpace == PhysicSpace.onGround)
                    _userJumpVector = Vector3.zero;
            }
            Vector3 finalMoveVec = clamp * delta;
            //Cancel moves into collider
            if (Physics.OverlapSphereNonAlloc(CenterOfMass + _userMoveVector * delta, _charHeightMax * 2, _colliderCache) > 0)
            {
                for (int i = 0; i < _colliderCache.Length; i++)
                {
                    if (_colliderCache[i] == null)
                        continue;
                    if (_colliderCache[i] == _controller)
                        continue;
                    if (_colliderCache[i].isTrigger)
                        continue;
                    if (Physics.ComputePenetration(_controller, transform.position + _userMoveVector * delta, transform.rotation, _colliderCache[i], _colliderCache[i].transform.position, _colliderCache[i].transform.rotation
                        , out Vector3 dir, out float dist))
                    {
                        return Vector3.zero;
                    }
                }
            }

            return finalMoveVec;
        }
        private Vector3 ComputeGravity(float delta)
        {
            _airTime += delta;
            if (_currentSurface.NoGravityForce)
                _airTime = 0;
            _gravityAcc += delta;
            if (_currentSurface.NoGravityForce)
                _gravityAcc = 0;
            Vector3 gravityNoScale = Physics.gravity.normalized * GravityMagnitude;
            Vector3 gravity = gravityNoScale * _gravityAcc;
            if ((_currentSurface.PointOffset > _controller.radius * INNER_RADIUS_DETECTION_SCALE))
            {
                //if (_userMoveVector.sqrMagnitude <= _controller.minMoveDistance && _currentPhysicSpace == PhysicSpace.inAir)
                if (!_currentSurface.NoGravityForce)
                {
                    _gravityAcc = Mathf.Clamp(_gravityAcc, 0, 0.25f);
                    gravity = Vector3.ProjectOnPlane(gravityNoScale * _gravityAcc, _currentSurface.NormalLarge);
                }
            }
            return gravity * delta;
        }
        private void ComputeDrag(float delta)
        {
            if (_currentSurface.IsOnSurfaceLarge)
            {
                _userMoveVector = Vector3.zero;
            }
            else
            {
                _userMoveVector = Vector3.Lerp(_userMoveVector, Vector3.zero, delta);
            }
        }
        private Vector3 EvaluateDepenetration()
        {
            Vector3 depenetrationVector = Vector3.zero;
            if (!_ignoreCollision)
            {
                Vector3 lowerHemi = CenterOfMass - transform.up * (_controller.height * 0.5f - _controller.radius - _controller.stepOffset) * 0.99f;
                Vector3 upperHemi = CenterOfMass + transform.up * (_controller.height * 0.5f - _controller.radius) * 0.99f;
                float radius = _controller.radius * 0.95f;
                if (Physics.OverlapCapsuleNonAlloc(lowerHemi, upperHemi, radius, _colliderCache) > 0)
                {
                    for (int i = 0; i < _colliderCache.Length; i++)
                    {
                        if (_colliderCache[i] == null)
                            continue;
                        if (_colliderCache[i] == _controller)
                            continue;
                        if (_colliderCache[i].isTrigger)
                            continue;
                        if (Physics.ComputePenetration(_controller, transform.position, transform.rotation, _colliderCache[i], _colliderCache[i].transform.position, _colliderCache[i].transform.rotation
                            , out Vector3 dir, out float dist))
                        {
                            depenetrationVector += dir * (dist * 0.5f);
                        }
                    }
                }
            }
            return depenetrationVector;
        }
        private void EvaluateCrouchState()
        {
            if (!_controller)
                return;
            bool internalCrouchState = _crouchState;
            if (!_crouchState && _controller.height <= 1 && Physics.SphereCast(CenterOfMass, _controller.radius, transform.up, out var hit, (_charHeightMax * 0.5f) * 1.01f))
            {
                internalCrouchState = true;
            }
            _controller.center = internalCrouchState ? new Vector3(0, (_charHeightMax * 0.25f), 0) : new Vector3(0, (_charHeightMax * 0.5f), 0);
            _controller.height = internalCrouchState ? (_charHeightMax * 0.5f) : _charHeightMax;
            IsCrouch = internalCrouchState;
        }
        private void OnJump(Vector3 jumpVector)
        {
            if (_currentSurface.surfaceCollider && _currentSurface.NoGravityForce && _currentSurface.IsOnSurface)
            {
                _currentSurface.surfaceCollider.attachedRigidbody?.WakeUp();
                _currentSurface.surfaceCollider.attachedRigidbody?.AddForceAtPosition(-jumpVector, transform.position, ForceMode.Impulse);
            }
        }

        #endregion

        #region Jobs      ##########################################################################################################################

        #endregion

        #region MonoBehaviours ################################################################################################################

        private void OnEnable()
        {
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = _charHeightMax - _charStepOffset;
                _controller.center = new Vector3(0, (_controller.height * 0.5f) + _charStepOffset, 0);
                _controller.radius = _charWidth;
                _controller.skinWidth = 0.001f;
                _controller.slopeLimit = 0;
                _controller.stepOffset = 0;
                _controller.minMoveDistance = 0.001f;
                _controller.hideFlags = HideFlags.HideInInspector;
            }
            //
            if (_posMarker == null)
            {
                _posMarker = new GameObject(nameof(_posMarker)).transform;
            }
            //Get all child colliders
            _childColliders.Clear();
            var allColls = GetComponentsInChildren<Collider>();
            for (int i = 0; i < allColls.Length; i++)
            {
                if (allColls[i].transform == transform)
                    continue;
                _childColliders.Add(allColls[i]);
            }
        }

        public void Update()
        {
            float delta = Time.deltaTime;
            Vector3 finalVel = Vector3.zero;
            _currentPhysicSpace = PhysicSpace.unSpecified;

            //Prevent inner collision
            if (_childColliders.Count > 0)
            {
                for (int i = 0; i < _childColliders.Count; i++)
                {
                    Physics.IgnoreCollision(_controller, _childColliders[i], true);
                }
            }

            //Physic related evaluations
            //spatial evals
            CheckSurface(_gravityScale >= 0 ? -transform.up : transform.up, delta, OnSurfaceDetected, OnSurfaceContact);
            if (_currentPhysicSpace == PhysicSpace.unSpecified)
                _currentPhysicSpace = PhysicSpace.inAir;
            //depenetration
            Vector3 depenetrationVector = EvaluateDepenetration();
            //surface momentum
            Quaternion rot = transform.rotation;
            OnSurfaceOps(ref _momemtum, ref rot);
            transform.rotation = rot;
            //shape evals
            //EvaluateCrouchState();

            //Compute motion
            if (_currentSurface.surfaceCollider)
            {
                Vector3 offset = Vector3.ProjectOnPlane(_currentSurface.Point - CenterOfMass, -transform.up);
                finalVel+= ((_currentSurface.Point - offset) - transform.position) * delta * _surfaceSnapSpeed;
            }
            finalVel += _momemtum;
            finalVel += ComputeInputVelocity(delta);
            finalVel += _userJumpVector * delta;
            finalVel += depenetrationVector;
            finalVel += ComputeGravity(delta);

            //Apply motion
            if (_ignoreCollision)
            {
                transform.position += finalVel;
                _controller.enabled = false;
            }
            else
            {
                _controller.enabled = true;
                _controller.Move(finalVel);
            }

            //Calculate drag
            ComputeDrag(delta);
        }

        #endregion
    }

}