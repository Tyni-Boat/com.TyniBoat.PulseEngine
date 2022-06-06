using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{

    [System.Serializable]
    public class SurfaceSnapper
    {
        private Collider _currentSurface;
        Vector3 _lastSurfacePos = Vector3.zero;
        Vector3 _velocity = Vector3.zero;

        public Vector3 CurrentSurfaceVelocity => _currentSurface? _velocity : Vector3.zero;

        public void Update(Collider lastSurface, Collider newSurface)
        {
            if (lastSurface != newSurface)
            {
                if (newSurface)
                    _lastSurfacePos = newSurface.transform.position;
                _currentSurface = newSurface;
                _velocity = Vector3.zero;
                return;
            }
            if (newSurface == null || lastSurface == null)
            {
                _velocity = Vector3.zero;
                if (newSurface)
                    _lastSurfacePos = newSurface.transform.position;
                else
                    _lastSurfacePos = Vector3.zero;
                return;
            }
            if (newSurface == lastSurface)
            {
                if (newSurface == _currentSurface)
                {
                    _velocity = _currentSurface.transform.position - _lastSurfacePos;
                }
                else
                {
                    _currentSurface = newSurface;
                    _velocity = Vector3.zero;
                }
                _lastSurfacePos = newSurface.transform.position;
            }
        }
    }
}