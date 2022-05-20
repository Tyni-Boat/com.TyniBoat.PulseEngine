using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{

    [System.Serializable]
    public class SurfaceSnapper
    {
        [SerializeField] private Transform _posMarker;
        [SerializeField] private Vector3 _lastSurfacePos;
        [SerializeField] private Quaternion _lastSurfaceRot;


        public Quaternion GetTorque(Quaternion rot, Vector3 axis)
        {
            if (true)
                return Quaternion.identity;
            Vector3 lastOrientation = Vector3.ProjectOnPlane(_lastSurfaceRot * Vector3.forward, axis);
            Vector3 orientation = Vector3.ProjectOnPlane(_posMarker.rotation * Vector3.forward, axis);
            float angle = Vector3.SignedAngle(lastOrientation, orientation, axis);
            Quaternion rotDiff = Quaternion.AngleAxis(angle, axis);
            //_posMarker.rotation = rot;
            return rotDiff;
        }

        public Vector3 GetTranslation(SurfaceInformations surface)
        {
            if (surface == null)
                return Vector3.zero;

            if (_posMarker == null)
            {
                _posMarker = new GameObject($"~SurfaceSnapper").transform;
            }
            if (!surface.IsOnSurfaceLarge)
            {
                _posMarker.position = surface.PointDetection - surface.OffsetDetection;
                _posMarker.SetParent(null);
                _lastSurfacePos = _posMarker.position;
                return Vector3.zero;
            }
            if (_posMarker.parent != surface.surfaceColliderDetection.transform)
                _posMarker.position = surface.PointDetection - surface.OffsetDetection;

            _posMarker.SetParent(surface.surfaceColliderDetection.transform);
            Vector3 move = _posMarker.position - _lastSurfacePos;
            _posMarker.position = surface.PointDetection - surface.OffsetDetection;
            _lastSurfacePos = _posMarker.position;
            return move;
        }

        public void Debug(Color col)
        {
            if (!_posMarker)
                return;
            PulseDebug.DrawCircle(_posMarker.position, 0.25f, Vector3.up, col);
            PulseDebug.DrawRay(_posMarker.position, _posMarker.forward * 0.25f, col);
        }
    }
}