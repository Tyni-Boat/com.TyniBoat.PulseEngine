using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine.CharacterControl
{

    [System.Serializable]
    public class SurfaceInformations
    {
        public SurfaceSnapper surfaceSnapper =  new SurfaceSnapper();
        public Collider surfaceCollider;
        public Collider surfaceColliderDetection;
        public Vector3 Point;
        public Vector3 PointDetection;
        public Vector3 Normal;
        public Vector3 NormalDetection;
        public Vector3 OffsetDetection;
        public Vector3 DepenetrationDir;
        public float Angle;
        public float AngleDetection;
        public float Distance;
        public float DistanceDetection;
        public bool IsOnSurfaceLarge;
        public bool IsOnSurface;
        public bool IsSurfaceStable;
        public bool IsWalkableStep;
    }
}