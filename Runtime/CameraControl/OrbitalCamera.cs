using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CameraController
{

    /// <summary>
    /// Represent a camera tha t orbit around a target.
    /// </summary>

    public class OrbitalCamera : MonoBehaviour
    {
        #region Constants #############################################################

        #endregion

        #region Statics   #############################################################

        #endregion

        #region vars ###########################################################

        [Header("Dependancies")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _target;
        private Transform _panControl;
        private Transform _tiltControl;
        private Vector2 _cameraInputs;

        [Header("Position Params")]
        [SerializeField] private Vector2 _distance = new Vector2(2, 5);
        [SerializeField] private float _height = 1;
        [SerializeField] private float _followSpeed = 15;

        [Header("Rotation Params")]
        [SerializeField] private float _panSpeed = 5;
        [SerializeField] private Vector2 _tiltAngle = new Vector2(-30, 45);
        [SerializeField] private float _tiltSpeed = 3.5f;
        [SerializeField] private float _acceleration = 2;
        [SerializeField] private bool _inverseTilt = true;
        [SerializeField] private bool _inversePan = false;

        [Header("Depenetration")]
        [SerializeField] private CameraDepenetrationStategy _depenetrationStategy;
        [SerializeField] private LayerMask _obstructionMask;
        [SerializeField] private float _depenetrationSphereRadius = 0.25f;
        [SerializeField] private float _depenetrationSpeed = 2;

        [Header("Debugging")]
        [SerializeField] private bool _debug;

        private float _accModifier = 0;
        private List<Collider> _obstructionColliders = new List<Collider>();
        private List<RaycastHit> _raycastListCache = new List<RaycastHit>();
        private RaycastHit[] _raycastCache = new RaycastHit[8];

        #endregion

        #region Properties ############################################################

        public bool IsInitialized => _camera && _panControl && _tiltControl;
        public Transform Target { get => _target; set => _target = value; }
        public Vector2 CameraInputs { get => _cameraInputs; set => _cameraInputs = value; }

        #endregion

        #region Events ############################################################

        public event EventHandler<Collider> OnObstructionBegin;
        public event EventHandler<RaycastHit> OnObstructionStay;
        public event EventHandler<Collider> OnObstructionEnd;

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        /// <summary>
        /// Initialise the component
        /// </summary>
        private void Initialize()
        {
            if (!_camera)
                _camera = Camera.main;
            _panControl = new GameObject("PanControl").transform;
            _panControl.SetParent(transform);
            _tiltControl = new GameObject("TiltControl").transform;
            _tiltControl.SetParent(_panControl);
            if (_tiltControl != null)
            {
                _camera.transform.SetParent(_tiltControl);
                _camera.transform.localPosition = Vector3.zero;
                _camera.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Update the look aceleration
        /// </summary>
        /// <param name="delta"></param>
        private void UpdateAcceleration(float delta)
        {
            if (_cameraInputs.sqrMagnitude > 0)
                _accModifier += delta * _acceleration;
            else
                _accModifier = 0;
            _accModifier = Mathf.Clamp01(_accModifier);
        }

        /// <summary>
        /// Follow the target
        /// </summary>
        /// <param name="delta"></param>
        private void FollowTarget(float delta)
        {
            if (!IsInitialized || !_target)
                return;
            transform.position = Vector3.Lerp(transform.position, _target.position, delta * _followSpeed);
        }

        /// <summary>
        /// Rotate the camera on Y axis
        /// </summary>
        /// <param name="delta"></param>
        private void PanCamera(float delta)
        {
            if (!IsInitialized)
                return;
            _panControl.localPosition = new Vector3(0, _height, 0);
            float angle = (_panControl.localRotation.eulerAngles.y * Mathf.Deg2Rad);
            if (_inversePan)
                angle -= _panSpeed * delta * _cameraInputs.x * _accModifier;
            else
                angle += _panSpeed * delta * _cameraInputs.x * _accModifier;
            _panControl.localEulerAngles = new Vector3(0, angle * Mathf.Rad2Deg, 0);
        }

        /// <summary>
        /// Rotate the camera on X axis with limits
        /// </summary>
        /// <param name="delta"></param>
        private void TiltCamera(float delta)
        {
            if (!IsInitialized)
                return;
            _tiltControl.localPosition = new Vector3(0, 0, 0);
            _camera.transform.LookAt(_tiltControl);
            float angle = (_tiltControl.localRotation.eulerAngles.x * Mathf.Deg2Rad);
            if (angle > Mathf.PI)
                angle -= 2 * Mathf.PI;
            if (_inverseTilt)
                angle -= _tiltSpeed * delta * _cameraInputs.y * _accModifier;
            else
                angle += _tiltSpeed * delta * _cameraInputs.y * _accModifier;
            angle = Mathf.Clamp(angle, _tiltAngle.x * Mathf.Deg2Rad, _tiltAngle.y * Mathf.Deg2Rad);
            _tiltControl.localEulerAngles = new Vector3(angle * Mathf.Rad2Deg, 0, 0);
        }

        /// <summary>
        /// Evaluate obtructions
        /// </summary>
        /// <param name="delta"></param>
        private void ComputeDepenetration(float delta)
        {
            _raycastListCache.Clear();
            if (!IsInitialized)
                return;
            if (_depenetrationSphereRadius <= 0)
            {
                _camera.transform.localPosition = new Vector3(0, 0, -_distance.y);
                return;
            }

            //Get the obstructions
            var hitCount = Physics.SphereCastNonAlloc(_tiltControl.position, _depenetrationSphereRadius, -_tiltControl.forward, _raycastCache, _distance.y, _obstructionMask, QueryTriggerInteraction.Ignore);
            _raycastListCache.AddRange(_raycastCache);
            _raycastListCache.Sort((a, b) => a.distance.CompareTo(b.distance));
            bool hitOneCollider = hitCount > 0 && _raycastListCache[0].collider != null;

            if (!hitOneCollider)
            {
                _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, new Vector3(0, 0, -_distance.y), delta * _depenetrationSpeed);
                return;
            }

            //Remove collider that don't obstruct anymore
            for(int i = _obstructionColliders.Count - 1; i >= 0; i--)
            {
                int index = _raycastListCache.FindIndex(ray => ReferenceEquals(ray.collider, _obstructionColliders[i]));
                if (index < 0)
                    continue;
                OnObstructionEnd?.Invoke(this, _obstructionColliders[i]);
                _obstructionColliders.RemoveAt(i);
            }

            //Refresh colliders that keep obstruction and Add collider that start to obstruct
            for(int i = 0; i < _raycastListCache.Count; i++)
            {
                int index = _obstructionColliders.FindIndex(col => ReferenceEquals(col.GetComponent<Collider>(), _obstructionColliders[i]));
                if (index < 0)
                    continue;
                OnObstructionEnd?.Invoke(this, _obstructionColliders[i]);
                _obstructionColliders.RemoveAt(i);
            }

            var hit = _raycastListCache[0];

            if (_debug)
            {
                PulseDebug.DrawRLine(_tiltControl.position, hit.point, Color.white);
                PulseDebug.DrawCircle(hit.point, _depenetrationSphereRadius, hit.normal, Color.white);
            }

            switch (_depenetrationStategy)
            {
                case CameraDepenetrationStategy.zoomIn:
                    {
                        Vector3 projection = Vector3.Project(hit.point - _tiltControl.position, -_tiltControl.forward);
                        PulseDebug.DrawRLine(_tiltControl.position, _tiltControl.position + projection, Color.red);
                        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, new Vector3(0, 0, -projection.magnitude - _depenetrationSphereRadius), delta * _depenetrationSpeed);
                    }
                    break;
                case CameraDepenetrationStategy.normalOffset:
                    {
                        Vector3 offsetPt = hit.point + hit.normal * _depenetrationSphereRadius;
                        _camera.transform.position = Vector3.Lerp(_camera.transform.position, offsetPt, delta * _depenetrationSpeed);

                    }
                    break;
            }

            //The camera is too close.
            if (Vector3.Distance(_camera.transform.position, _tiltControl.position) < _distance.x)
            {
                _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, new Vector3(0, 0, -_distance.x), delta * _depenetrationSpeed * 2);
            }

        }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void OnEnable()
        {
            if (!IsInitialized)
                Initialize();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            UpdateAcceleration(delta);
            FollowTarget(delta);
            PanCamera(delta);
            TiltCamera(delta);
            ComputeDepenetration(delta);
        }

        #endregion
    }
}