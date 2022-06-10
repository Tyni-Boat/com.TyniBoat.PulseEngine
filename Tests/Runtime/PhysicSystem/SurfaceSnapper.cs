using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{

    [System.Serializable]
    public class SurfaceSnapper
    {
        public Vector3 LinearVelocity => _moveDirection;
        public Quaternion AngularVelocity { get; private set; }


        public Transform activePlatform;
        [SerializeField] private Vector3 _moveDirection;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private Vector3 _activeLocalPlatformPoint;
        [SerializeField] private Vector3 _activeGlobalPlatformPoint;
        [SerializeField] private Quaternion _activeGlobalPlatformRotation;
        [SerializeField] private Quaternion _activeLocalPlatformRotation;


        public void Update(Transform surface, Vector3 position, Quaternion rotation)
        {
            //Update
            if (activePlatform != null)
            {
                Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(_activeLocalPlatformPoint);
                _moveDirection = newGlobalPlatformPoint - _activeGlobalPlatformPoint;
                _activeGlobalPlatformPoint = newGlobalPlatformPoint;
                if (_moveDirection.sqrMagnitude > 0)
                {
                    //LinearVelocity = _moveDirection;
                }
                if (activePlatform)
                {
                    // Support moving platform rotation
                    Quaternion newGlobalPlatformRotation = activePlatform.rotation * _activeLocalPlatformRotation;
                    Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_activeGlobalPlatformRotation);
                    // Prevent rotation of the local up vector
                    rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;
                    AngularVelocity = rotationDiff;

                    UpdateMovingPlatform(position, rotation);
                }
            }
            else
            {
                if (_moveDirection.sqrMagnitude > 0)
                {
                    _moveDirection = Vector3.zero;
                }
            }

            //Detection
            if (activePlatform != surface)
            {
                Reset();
                activePlatform = surface;
                if (surface)
                    UpdateMovingPlatform(position, rotation, true);
            }
        }

        public void Reset()
        {
            activePlatform = null;
            AngularVelocity = Quaternion.Euler(Vector3.zero);
            _offset = Vector3.zero;
            _moveDirection = Vector3.zero;
        }


        private void UpdateMovingPlatform(Vector3 pos, Quaternion rot, bool updateLocal = false)
        {
            if (updateLocal)
            {
                _activeLocalPlatformPoint = activePlatform.InverseTransformPoint(pos);
                _activeGlobalPlatformPoint = pos;
            }
            // Support moving platform rotation
            _activeGlobalPlatformRotation = rot;
            _activeLocalPlatformRotation = Quaternion.Inverse(activePlatform.rotation) * rot;
        }
    }
}