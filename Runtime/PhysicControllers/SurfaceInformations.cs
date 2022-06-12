using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine.CharacterControl
{

    [System.Serializable]
    public struct SurfaceInformations
    {
        public SurfaceType SurfaceType;
        [System.NonSerialized] public Collider surfaceCollider;
        [System.NonSerialized] public Collider lastSurfaceCollider;
        public Vector3 Point;
        public Vector3 PointNoOffset;
        public Vector3 PointLocalSurfaceSpace;
        public Vector3 Normal;
        public Vector3 NormalDetection;
        public Vector3 SurfaceClampForce;
        public float Angle;
        public float AngleDetection;
        public float Distance;
        public bool IsOnSurface;
        public bool IsSurfaceStable;

        public static SurfaceInformations NullSurface => new SurfaceInformations { SurfaceType = SurfaceType.none };

    }
}