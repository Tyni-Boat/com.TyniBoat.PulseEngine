using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace PulseEngine.CharacterControl
{

    public class PhysicCharacterController3d : BaseCharacterController
    {
        #region Constants ##########################################################################################################################

        const float MAX_SURFACE_DETECTION_DISTANCE = 25;

        #endregion

        #region Variables ##########################################################################################################################

        [Header("Self Configs")]
        [SerializeField] private float _radiusDetectionScale = 0.75f;
        [SerializeField] private float _height = 1.8f;
        [SerializeField] private float _width = 0.37f;
        [SerializeField] private float _stepOffset = 0.25f;
        [SerializeField] private float _slopeAngle = 60;

        //privates ////////////////////////////////////////////////////////////////////////////////////////

        ///components
        private CharacterController _controller;
        private ParentConstraint _constraint;

        ///State
        private bool _crouchState;
        private float _charHeightMin = 1f;
        private float _charHeightCurrent = 1.8f;

        #endregion

        #region Cache   ##########################################################################################################################

        private Collider[] _colliderCache = new Collider[8];
        private List<Collider> _childColliders = new List<Collider>();

        #endregion

        #region Inner Types ######################################################################################################################

        #endregion

        #region Properties ########################################################################################################################

        /// <summary>
        /// The current surface the controller in on
        /// </summary>
        public SurfaceInformations CurrentSurface => _currentSurface;

        /// <summary>
        /// The center of mass of the controller in world space
        /// </summary>
        public Vector3 CenterOfMass { get => (transform.position + _controller.center); }

        /// <summary>
        /// Is the controller in crouched state?
        /// </summary>
        public bool IsCrouch { get; private set; }

        /// <summary>
        /// The event called when the surface contact changed.
        /// </summary>
        public event EventHandler<bool> OnSurfaceContactChanged;

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
            if (GravityZone == null)
                return 0;
            _gravityAcc = 0;
            float Voy = Mathf.Sqrt(2 * GravityMagnitude * (height - 0));
            float fTime = Voy / GravityMagnitude;
            _userJumpVector = -GravityZone.GravityDirection * Voy + _momemtum;
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

        public void AdjustShape(float height, float width, float stepOffset)
        {
            if (!_controller)
                return;
            _height = height;
            _width = width;
            _stepOffset = stepOffset;
            _controller.height = _height;
            _controller.radius = _width;
            _controller.center = new Vector3(0, height * 0.5f + stepOffset, 0);
        }

        #endregion

        #region Private Functions ##########################################################################################################

        private void CheckSurface(Vector3 direction, Action<bool> OnSurfaceDetection = null, Action<bool> OnSurfaceContact = null)
        {
            if (_currentPhysicSpace != PhysicSpace.unSpecified)
                return;
            if (direction == Vector3.zero)
                direction = -Vector3.up;
            direction.Normalize();
            bool wereOnSurface = _currentSurface.IsOnSurfaceLarge;
            bool wereOnSurfaceContact = _currentSurface.IsOnSurface;

            Vector3 pt = transform.position;
            Vector3 pt_large = transform.position;
            Vector3 normal = -direction.normalized;
            Vector3 normal_large = -direction.normalized;
            Vector3 offsetVec = Vector3.zero;
            Vector3 surfaceDepenetrationVector = Vector3.zero;
            float charHeight = (_controller.height * 0.5f) + (2 * _stepOffset);
            float maxDistance = MAX_SURFACE_DETECTION_DISTANCE;
            float hitDistance = float.MaxValue;
            Collider surfaceCol = null;
            Collider surfaceCol_large = null;

            //Heights
            if (Physics.Raycast(CenterOfMass, direction, out var heightHit, MAX_SURFACE_DETECTION_DISTANCE, _layerMask, QueryTriggerInteraction.Ignore))
            {
                maxDistance = Vector3.Project(heightHit.point - CenterOfMass, direction).magnitude - ((_controller.height * 0.5f) + _stepOffset);

                //large
                if (Physics.SphereCast(CenterOfMass, _controller.radius * _radiusDetectionScale, direction, out var bigHit, charHeight - _stepOffset * 0.98f, _layerMask, QueryTriggerInteraction.Ignore))
                {
                    pt_large = bigHit.point;
                    normal_large = bigHit.normal;
                    surfaceCol_large = bigHit.collider;
                    offsetVec = Vector3.ProjectOnPlane(bigHit.point - CenterOfMass, direction);

                    //Hit
                    if (Physics.Raycast(CenterOfMass, direction, out var mediumHit, charHeight - _stepOffset, _layerMask, QueryTriggerInteraction.Ignore))
                    {
                        pt = mediumHit.point;
                        normal = mediumHit.normal;
                        surfaceCol = mediumHit.collider;
                        Vector3 vecOffset = pt - transform.position;
                        hitDistance = vecOffset.magnitude * Mathf.Sign(Vector3.Dot(direction, vecOffset));
                        surfaceDepenetrationVector = -direction;
                    }
                }
            }

            //Evaluate step height
            Vector3 ptOnCollider = _controller.PointOnSurface(pt);
            Vector3 ptToStepTo = transform.position - offsetVec;

            //Height from surface
            _surfaceDistance = maxDistance;

            //Larges
            _currentSurface.IsOnSurfaceLarge = surfaceCol_large;
            _currentSurface.surfaceColliderDetection = surfaceCol_large;
            _currentSurface.NormalDetection = normal_large;
            _currentSurface.PointDetection = pt_large;
            _currentSurface.AngleDetection = 90 - Mathf.Acos(Vector3.Dot(-direction, normal_large)) * Mathf.Rad2Deg;

            //colliders
            _currentSurface.surfaceCollider = surfaceCol;

            //mediums
            _currentSurface.Point = pt;
            _currentSurface.Normal = normal;

            //offset
            _currentSurface.OffsetDetection = offsetVec;

            //distances and angles
            _currentSurface.Distance = hitDistance;
            _currentSurface.Angle = 90 - Mathf.Acos(Vector3.Dot(-direction, normal)) * Mathf.Rad2Deg;

            //
            _currentSurface.IsOnSurface = hitDistance <= _stepOffset && surfaceCol;
            _currentSurface.IsSurfaceStable = _currentSurface.IsOnSurface && _currentSurface.Angle >= _slopeAngle;
            _currentSurface.IsWalkableStep = offsetVec != Vector3.zero && Vector3.Project(pt_large - transform.position, direction).magnitude < _stepOffset && _currentSurface.AngleDetection >= _slopeAngle
                && Vector3.Dot(transform.forward, Vector3.ProjectOnPlane(pt_large - transform.position, direction).normalized) > 0;

            //
            _currentSurface.DepenetrationDir = surfaceDepenetrationVector * Mathf.Abs(hitDistance);

            _currentPhysicSpace = _currentSurface.IsOnSurface || _currentSurface.IsWalkableStep ? PhysicSpace.onGround : _currentPhysicSpace;
            if (_currentPhysicSpace == PhysicSpace.onGround)
                _lastPhysicSpace = _currentPhysicSpace;

            //detect surface events
            if (wereOnSurface != _currentSurface.IsOnSurfaceLarge)
            {
                OnSurfaceDetection?.Invoke(_currentSurface.IsOnSurfaceLarge);
            }
            //detect contact events
            if (wereOnSurfaceContact != _currentSurface.IsOnSurface)
            {
                OnSurfaceContact?.Invoke(_currentSurface.IsOnSurface);
            }

            //Debug
            if (_debug)
            {
                if (_currentSurface.IsOnSurface) PulseDebug.DrawCircle(pt, 0.15f, _currentSurface.Normal, Color.red);
                if (_currentSurface.IsOnSurfaceLarge) PulseDebug.DrawCircle(pt_large, 0.15f, _currentSurface.NormalDetection, Color.white);
                PulseDebug.DrawCircle(ptOnCollider, 0.1f, _currentSurface.Normal, Color.black);
                if (_currentSurface.IsOnSurfaceLarge) PulseDebug.DrawCircle(pt_large - offsetVec, _controller.radius * _radiusDetectionScale, transform.up, Color.yellow);
                _currentSurface.surfaceSnapper?.Debug(Color.magenta);
            }
        }
        private void CheckAir(float delta)
        {
            if (_currentPhysicSpace != PhysicSpace.unSpecified)
                return;
            if (_airTime < _airTimeToBeInAir && _lastPhysicSpace == PhysicSpace.onGround)
            {
                _currentPhysicSpace = _lastPhysicSpace;
                return;
            }
            _currentPhysicSpace = PhysicSpace.inAir;
            _lastPhysicSpace = PhysicSpace.inAir;
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
                _currentSurface.surfaceCollider.attachedRigidbody?.WakeUp();
                _currentSurface.surfaceCollider.attachedRigidbody?.AddForceAtPosition(_controller.velocity, transform.position, ForceMode.Force);
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
        private Vector3 MovingSurfaceOps(Vector3 direction, ref Quaternion rotation)
        {
            Vector3 moveVector = Vector3.zero;
            _constraint.translationAtRest = transform.position;
            _constraint.constraintActive = _userMoveVector.sqrMagnitude <= 0;
            if (_currentSurface.surfaceColliderDetection)
            {
                Transform oldSrc = null;
                if (_constraint.sourceCount > 0)
                    oldSrc = _constraint.GetSource(0).sourceTransform;
                var constraint = new ConstraintSource();
                constraint.weight = 1;
                constraint.sourceTransform = _currentSurface.surfaceColliderDetection.transform;
                if (_constraint.sourceCount <= 0)
                {
                    _constraint.AddSource(constraint);
                }
                else
                {
                    _constraint.SetSource(0, constraint);
                }
                Matrix4x4 matrix = Matrix4x4.TRS(constraint.sourceTransform.position, constraint.sourceTransform.rotation, Vector3.one);
                var localOffset = matrix.inverse.MultiplyPoint3x4(_currentSurface.PointDetection - _currentSurface.OffsetDetection);
                if (oldSrc != _currentSurface.surfaceColliderDetection.transform || !_constraint.constraintActive)
                {
                    _constraint.SetTranslationOffset(0, localOffset);
                }
                if (!_constraint.constraintActive)
                {
                    moveVector = Vector3.ProjectOnPlane(_currentSurface.surfaceSnapper.GetTranslation(_currentSurface), direction);
                }
                else
                {
                    _currentSurface.surfaceSnapper.GetTranslation(_currentSurface);
                }
            }
            else
            {
                if (_constraint.sourceCount > 0)
                    _constraint.RemoveSource(0);
                _currentSurface.surfaceSnapper.GetTranslation(_currentSurface);
            }
            //Vector3 axis = GravityZone == null ? transform.up : GravityZone.GravityDirection;
            //var rot = _currentSurface.surfaceSnapper.GetTorque(transform.rotation, axis);

            ////Follow movement of surface
            //rotation *= rot;

            //_currentSurface.surfaceSnapper.LateUpdate(_currentSurface);
            return moveVector;
        }
        private Vector3 ClampOnGround(float delta)
        {
            if (_userJumpVector.sqrMagnitude > 0)
                return Vector3.zero;

            if (_currentSurface.Distance < 0)
                return Vector3.Lerp(transform.position, _currentSurface.PointDetection - _currentSurface.OffsetDetection, delta * _surfaceSnapSpeed) - transform.position;
            else
                return (_currentSurface.PointDetection - _currentSurface.OffsetDetection) - transform.position;
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
            if (_currentSurface.IsOnSurface)
            {
                MovementNormal = Vector3.Dot(clamp.normalized, Vector3.ProjectOnPlane(_currentSurface.PointDetection - transform.position, transform.up).normalized) > 0.33f
                    ? _currentSurface.NormalDetection : _currentSurface.Normal;
            }
            clamp = Vector3.ProjectOnPlane(_userMoveVector, MovementNormal);
            if (_jumpRequestTimeToPeak > 0)
            {
                _jumpRequestTimeToPeak -= delta;
                if (_jumpRequestTimeToPeak <= 0 && _currentPhysicSpace == PhysicSpace.onGround)
                    _userJumpVector = Vector3.zero;
            }
            Vector3 finalMoveVec = clamp * delta;

            return finalMoveVec;
        }
        private Vector3 ComputeGravity(float delta)
        {
            if (GravityZone == null)
                GravityZone = GravityZone3d.Earth;
            GravityZone?.UpdateZone(this);
            _airTime += delta;
            if (_currentPhysicSpace != PhysicSpace.inAir)
                _airTime = 0;
            switch (_currentPhysicSpace)
            {
                case PhysicSpace.unSpecified:
                    break;
                case PhysicSpace.inAir:
                    _gravityAcc += delta;
                    break;
                case PhysicSpace.onGround:
                    if (_currentSurface.IsSurfaceStable || _currentSurface.IsWalkableStep)
                        _gravityAcc = 0;
                    else
                        _gravityAcc += delta;
                    break;
                case PhysicSpace.inFluid:
                    break;
                case PhysicSpace.onWall:
                    break;
            }
            Vector3 gravityNoScale = GravityZone.GravityDirection * GravityMagnitude;
            Vector3 gravity = gravityNoScale * _gravityAcc;
            if ((!_currentSurface.IsSurfaceStable && _currentSurface.IsOnSurface)
                || (!_currentSurface.IsWalkableStep && _currentSurface.IsOnSurfaceLarge && _currentSurface.Normal != _currentSurface.NormalDetection))
            {
                gravity = Vector3.ProjectOnPlane(gravity, _currentSurface.NormalDetection);
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
        private void EvaluateCrouchState()
        {
            if (!_controller)
                return;
            bool internalCrouchState = _crouchState;
            if (!_crouchState && _controller.height <= 1 && Physics.SphereCast(CenterOfMass, _controller.radius, transform.up, out var hit, (_height * 0.5f) * 1.01f))
            {
                internalCrouchState = true;
            }
            _controller.center = internalCrouchState ? new Vector3(0, (_height * 0.25f), 0) : new Vector3(0, (_height * 0.5f), 0);
            _controller.height = internalCrouchState ? (_height * 0.5f) : _height;
            IsCrouch = internalCrouchState;
        }
        private void OnJump(Vector3 jumpVector)
        {
            if (_currentSurface.IsOnSurface)
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
                AdjustShape(_height, _width, _stepOffset);
                _controller.stepOffset = 0;
                _controller.hideFlags = HideFlags.HideInInspector;
            }
            if (_constraint == null)
            {
                _constraint = gameObject.AddComponent<ParentConstraint>();
                _constraint.constraintActive = false;
                //_constraint.hideFlags = HideFlags.HideInInspector;
                _constraint.translationAtRest = transform.position;
                _constraint.rotationAxis = Axis.None;
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
                    for (int j = 0; j < _childColliders.Count; j++)
                    {
                        Physics.IgnoreCollision(_childColliders[i], _childColliders[j], true);
                    }
                }
            }

            //Physic related evaluations
            //spatial evals
            CheckSurface(GravityZone == null ? Vector3.zero : GravityZone.GravityDirection, OnSurfaceDetected, OnSurfaceContact);
            CheckAir(delta);
            //shape evals

            //Compute motion
            //finalVel += _momemtum;
            finalVel += ComputeInputVelocity(delta);
            //finalVel += _userJumpVector * delta;
            finalVel += ComputeGravity(delta);
            finalVel += ClampOnGround(delta);

            //surface momentum
            Quaternion rot = transform.rotation;
            finalVel += MovingSurfaceOps(GravityZone == null ? transform.up : GravityZone.GravityDirection, ref rot);
            transform.rotation = rot;

            //Apply motion
            if (_ignoreCollision)
            {
                _controller.enabled = false;
                transform.position += finalVel;
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