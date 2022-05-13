using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine
{
    /// <summary>
    /// Control Characters Movements in a kinematic way.
    /// </summary>
    public class KinematicController3d : PulseComponent
    {
        #region Constants ##########################################################################################################################

        const float MAX_SURFACE_DETECTION_DISTANCE = 25;
        const float NO_GRAVITY_SURFACE_DIST = 0.05f;
        const float INNER_RADIUS_DETECTION_SCALE = 0.5f;

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

        ///State
        private bool _crouchState;
        private float _charHeight = 2f;
        private float _charWidth = 0.5f;

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
            _charHeight = height;
            _charWidth = width;
            _controller.height = _charHeight;
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

            Vector3 pt = transform.position;
            Vector3 pt_large = transform.position;
            Vector3 normal = -direction.normalized;
            Vector3 normalLarge = -direction.normalized;
            Vector3 tr_normal = direction.normalized;
            Vector3 cr_normal = direction.normalized;
            Vector3 offsetVec = Vector3.zero;
            Vector3 offsetNormal = -transform.up;
            Collider surfaceCol = null;

            //Large
            if (Physics.SphereCast(CenterOfMass, _controller.radius, -transform.up, out var bigHit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
            {
                pt_large = bigHit.point;
                normalLarge = bigHit.normal;
                tr_normal = normal;
                offsetVec = Vector3.ProjectOnPlane(pt_large - CenterOfMass, -transform.up);

                //Medium
                if (Physics.SphereCast(CenterOfMass, _controller.radius * INNER_RADIUS_DETECTION_SCALE, direction, out var mediumHit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
                {
                    pt = mediumHit.point;
                    normal = mediumHit.normal;
                    surfaceCol = mediumHit.collider;
                    tr_normal = normal;

                    //Small
                    if (mediumHit.collider.Raycast(new Ray(CenterOfMass, direction), out var smallHit, (_controller.height / 2) + 0.001f))
                    {
                        tr_normal = smallHit.normal;
                    }
                }

                //offset
                if (bigHit.collider.Raycast(new Ray(CenterOfMass + offsetVec, direction), out var offsetHit, (_controller.height / 2) + 0.001f))
                {
                    offsetNormal = offsetHit.normal;
                }

                //Central Point
                if (Physics.Raycast(new Ray(CenterOfMass, direction), out var centralHit, (_controller.height / 2) + _controller.radius * 0.15f))
                {
                    cr_normal = centralHit.normal;
                }
            }

            //Evaluate step height
            Vector3 ptOnCollider = _controller.PointOnSurface(pt);
            Vector3 centerProj = Vector3.Project((ptOnCollider - CenterOfMass), -transform.up);
            Vector3 ptToStepTo = CenterOfMass + centerProj;

            _surfaceDistance = pt_large != transform.position? Vector3.Project(pt_large - transform.position, direction).magnitude : MAX_SURFACE_DETECTION_DISTANCE;
            _currentSurface.lastSurfaceCollider = _currentSurface.surfaceCollider;
            _currentSurface.Point = pt;
            _currentSurface.Normal = normal;
            _currentSurface.NormalLarge = normalLarge;
            _currentSurface.TrueNormal = tr_normal;
            _currentSurface.CentralNormal = cr_normal;
            _currentSurface.OffsetNormal = offsetNormal;
            _currentSurface.Distance = Vector3.Distance(pt, ptOnCollider);
            _currentSurface.surfaceCollider = _currentSurface.Distance < _groundedSurfaceDist ? surfaceCol : null;
            _currentSurface.PointOffset = offsetVec.magnitude;
            _currentSurface.Angle = Vector3.Angle(transform.up, normalLarge);
            _currentSurface.InnerAngle = Vector3.Angle(transform.up, normal);
            _currentSurface.IsOnSurfaceLarge = Vector3.Distance(pt_large, _controller.PointOnSurface(pt_large)) <= _groundedSurfaceDist && surfaceCol;
            _currentSurface.IsOnSurface = (_currentSurface.Distance <= _groundedSurfaceDist || (ptOnCollider - CenterOfMass).sqrMagnitude >= (pt - CenterOfMass).sqrMagnitude) && _currentSurface.surfaceCollider;
            _currentSurface.IsSurfaceStable = _currentSurface.Angle <= _controller.slopeLimit && _currentSurface.IsOnSurface && _currentSurface.PointOffset <= (_controller.radius);
            _currentSurface.IsWalkableStep = Vector3.Distance(transform.position, ptToStepTo) <= _controller.stepOffset;
            _currentSurface.NoGravityForce = (_currentSurface.IsOnSurface && _currentSurface.Distance < NO_GRAVITY_SURFACE_DIST && _currentSurface.PointOffset <= _controller.radius * INNER_RADIUS_DETECTION_SCALE) 
                || _controller.isGrounded;
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
                PulseDebug.DrawCircle(pt, 0.15f, _currentSurface.Normal, Color.red);
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
            if (_currentSurface.lastSurfaceCollider != _currentSurface.surfaceCollider)
            {
                //switch surface
                _posMarker?.SetParent(_currentSurface.surfaceCollider ? _currentSurface.surfaceCollider.transform : transform);
                _posMarker.position = _currentSurface.surfaceCollider ? _currentSurface.Point : transform.position;
                _posMarker.rotation = transform.rotation;
                _lastSurfacePos = _posMarker.position;
                _lastSurfaceRot = _posMarker.rotation;
                moveVector = Vector3.zero;
            }
            //still on the same surface
            else if (_currentSurface.surfaceCollider != null)
            {
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
                    _posMarker.position = _currentSurface.Point;
            }
            else
            {
                moveVector = Vector3.zero;
                _posMarker.position = transform.position;
                _posMarker.rotation = transform.rotation;
            }

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
            if ((Vector3.Angle(_currentSurface.NormalLarge, transform.up) < _controller.slopeLimit && _currentSurface.IsOnSurface)
                || (Mathf.Abs(_gravityScale) > 0 && !_currentSurface.IsOnSurface && _currentSurface.IsOnSurfaceLarge && _currentSurface.Angle > _controller.slopeLimit))
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
            return clamp * delta;
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
                if (_userMoveVector.sqrMagnitude <= _controller.minMoveDistance && _currentPhysicSpace == PhysicSpace.inAir)
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
            if (!_crouchState && _controller.height <= 1 && Physics.SphereCast(CenterOfMass, _controller.radius, transform.up, out var hit, (_charHeight * 0.5f) * 1.01f))
            {
                internalCrouchState = true;
            }
            _controller.center = internalCrouchState ? new Vector3(0, (_charHeight * 0.25f), 0) : new Vector3(0, (_charHeight * 0.5f), 0);
            _controller.height = internalCrouchState ? (_charHeight * 0.5f) : _charHeight;
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
                _controller.center = new Vector3(0, (_charHeight * 0.5f), 0);
                _controller.radius = _charWidth;
                _controller.skinWidth = 0.001f;
                _controller.slopeLimit = 60;
                _controller.stepOffset = 0.45f;
                _controller.height = _charHeight;
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

        public void FixedUpdate()
        {
            float delta = Time.fixedDeltaTime;
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
            EvaluateCrouchState();

            //Compute moition
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