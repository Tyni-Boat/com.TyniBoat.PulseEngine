using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.CharacterControl
{

    public class MovingPlatform3D : MovingPlatform
    {
        public enum TraversalMode
        {
            loop,
            pingPong,
            oneTime,
        }

        [SerializeField] private Rigidbody _rigidBody;
        [SerializeField] private TraversalMode _moveMode;
        [SerializeField] private Vector3 _torque;
        [SerializeField] private float _arrivalDist;
        [SerializeField] private bool _clampMode;
        [SerializeField] private bool _debug;
        [SerializeField] private List<Transform> _path = new List<Transform>();
        private List<Vector3> _pathPoints = new List<Vector3>();

        public Transform pin;

        private int _currentNode = 0;
        private bool _reverse;

        public Vector3 RelativeVelocity(Vector3 point) => _rigidBody ? _rigidBody.GetPointVelocity(point) : Vector3.zero;
        public Vector3 RelativeVelocity(Transform tr) => _rigidBody ? _rigidBody.GetPointVelocity(_LocalPoints[tr].oldPosition) : Vector3.zero;

        private void OnEnable()
        {
            if (_rigidBody == null)
            {
                _rigidBody = GetComponent<Rigidbody>();
            }
            _pathPoints.Clear();
            for (int i = 0; i < _path.Count; i++)
            {
                if (_path[i] == null)
                    continue;
                _pathPoints.Add(_path[i].position);
            }
            _currentNode = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (_debug)
            {
                PulseDebug.DrawPath(_pathPoints.ToArray(), Color.white, Color.white);
                PulseDebug.DrawCircle(transform.position, 5f, transform.up, Color.cyan);
                PulseDebug.DrawRay(transform.position, _rigidBody.velocity, Color.cyan);
                if(pin)
                    PulseDebug.DrawRay(pin.position, _rigidBody.GetPointVelocity(pin.position), Color.yellow);
            }
        }

        private void FixedUpdate()
        {
            if (_rigidBody == null)
                return;
            _rigidBody.AddTorque(_torque - _rigidBody.angularVelocity);
            if (!_currentNode.InInterval(0, _pathPoints.Count))
                return;
            if (_clampMode)
                _rigidBody.velocity = _pathPoints[_currentNode] - transform.position;
            else
                _rigidBody.AddForce((_pathPoints[_currentNode] - transform.position) - _rigidBody.velocity);
            if ((transform.position - _pathPoints[_currentNode]).magnitude <= _arrivalDist) { OnArrival(); }
        }

        private void OnArrival()
        {
            switch (_moveMode)
            {
                case TraversalMode.loop:
                    {
                        _currentNode++;
                        _currentNode %= _pathPoints.Count;
                    }
                    break;
                case TraversalMode.pingPong:
                    {
                        if (_reverse)
                            _currentNode--;
                        else
                            _currentNode++;
                        if (_reverse && _currentNode < 0)
                        {
                            _currentNode = 0;
                            _reverse = false;
                        }
                        else if (!_reverse && _currentNode >= _pathPoints.Count)
                        {
                            _currentNode = _pathPoints.Count - 1;
                            _reverse = true;
                        }
                    }
                    break;
                case TraversalMode.oneTime:
                    {
                        if (_currentNode < _pathPoints.Count)
                            _currentNode++;
                    }
                    break;
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
        }

    }
}