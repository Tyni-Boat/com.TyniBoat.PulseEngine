using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 LinearVelocity { get; set; }
    public Quaternion AngularVelocity { get; set; }

    [SerializeField] private bool _debug;
    private Vector3 _lastPosition;
    private Vector3 _lastFwd;

    // Start is called before the first frame update
    void OnEnable()
    {
        _lastPosition = transform.position;
        _lastFwd = transform.forward;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 velocity = transform.position - _lastPosition;
        _lastPosition = transform.position;
        Quaternion angular = Quaternion.FromToRotation(_lastFwd, transform.forward);
        _lastFwd = transform.forward;
        LinearVelocity = velocity;
        AngularVelocity = angular;
        if (_debug)
        {
            PulseEngine.PulseDebug.DrawRay(transform.position, velocity, Color.magenta);
        }
    }
}
