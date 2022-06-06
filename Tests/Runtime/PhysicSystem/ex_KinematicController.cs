using System;
using System.Collections;
using System.Collections.Generic;
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
        private CharacterController _controller;


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
        public Vector3 CenterOfMass { get => (transform.position + _controller.center); }

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
            if (!_controller)
                return;
            _controller.height = height - _stepOffset;
            _controller.radius = width;
            _controller.center = new Vector3(0, (_controller.height) * 0.5f + _stepOffset, 0);
            _surfaceSnapBufferLenght = _stepOffset;
        }

        #endregion

        #region Private Functions ##########################################################################################################

        /// <summary>
        /// Detect ground
        /// </summary>
        /// <returns></returns>
        private SurfaceInformations CheckGround()
        {
            if (GravityZone == null)
                return SurfaceInformations.NullSurface;

            Vector3 direction = GravityZone.GravityDirection.normalized;
            //Detect ground
            if (_debug)
            {
                PulseDebug.DrawRay(CenterOfMass, direction * (_surfaceSnapBufferLenght + _controller.center.y), Color.green);
            }

            float radius = _controller.radius * 0.85f;
            int collisions = Physics.SphereCastNonAlloc(new Ray(CenterOfMass, direction), radius
                , rayCache, _surfaceSnapBufferLenght + _controller.center.y, _layerMask, QueryTriggerInteraction.Ignore);
            if (collisions > 0)
            {
                if (_debug)
                {
                    for (int i = rayCache.Length - 1; i >= collisions; i--)
                    {
                        PulseDebug.DrawCircle(rayCache[i].point, radius, rayCache[i].normal, Color.white);
                    }
                }
                int closestValidIndex = -1;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < collisions; i++)
                {
                    if (_debug)
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
                    if (_debug)
                        PulseDebug.DrawCircle(rayCache[closestValidIndex].point, radius, rayCache[closestValidIndex].normal, Color.green);
                    Vector3 fullClampForce = Vector3.Project(rayCache[closestValidIndex].point - transform.position, direction);
                    return new SurfaceInformations
                    {
                        surfaceCollider = rayCache[closestValidIndex].collider,
                        Point = rayCache[closestValidIndex].point,
                        Normal = rayCache[closestValidIndex].normal,
                        SurfaceClampForce = Vector3.Dot(fullClampForce, direction) > 0? fullClampForce : fullClampForce * _surfaceSnapForceMultiplier,
                        Angle = rayCache[closestValidIndex].AngleFromHorizontal(-direction),
                        SurfaceType = SurfaceType.Ground,
                    };
                }
            }
            return SurfaceInformations.NullSurface;
        }

        /// <summary>
        /// Apply movement to the controller
        /// </summary>
        /// <param name="move"></param>
        private void MoveController(Vector3 move) => _controller?.Move(move);

        #endregion

        #region Jobs      ##########################################################################################################################

        #endregion

        #region MonoBehaviours ################################################################################################################

        private void OnEnable()
        {
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.slopeLimit = 0;
                _controller.stepOffset = _stepOffset * 0.1f;
                _controller.minMoveDistance = 0;
                _controller.hideFlags = HideFlags.HideInInspector;
            }
            AdjustShape(_height, _width);
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

        public void Update()
        {
            float delta = Time.deltaTime;
            Vector3 finalVel = Vector3.zero;
            _currentPhysicSpace = PhysicSpace.unSpecified;
            if (GravityZone == null)
                GravityZone = GravityZone3d.Earth;

            //Prevent inner collision *****************************************************************************************
            InnerCollision3DState(_controller, false);

            //Physic related evaluations **************************************************************************************
            finalVel += CheckGround().SurfaceClampForce;

            //Compute motion **************************************************************************************************
            finalVel += _userMoveVector * delta;

            //Apply motion ****************************************************************************************************
            MoveController(finalVel);

            //Calculate drag **************************************************************************************************
        }

        #endregion
    }
}