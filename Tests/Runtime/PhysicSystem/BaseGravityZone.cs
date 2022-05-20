using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{
    /// <summary>
    /// The base type of all gravity zone in 2d and 3d spaces
    /// </summary>
    [System.Serializable]
    public class BaseGravityZone: IEquatable<BaseGravityZone>
    {
        [field: SerializeField] public GravityZoneType ZoneType { get; set; }
        [field: SerializeField] public Vector3 GravityDirection { get; set; }
        [field: SerializeField] public Vector3 ZonePosition { get; set; }
        [field: SerializeField] public float FixedGravityScale { get; set; }
        [field: SerializeField] public float GravityScale { get; set; }
        [field: SerializeField] public AnimationCurve GravityScaleOverDistance { get; set; }

        public bool Equals(BaseGravityZone other)
        {
            if(ReferenceEquals(null, other) && ReferenceEquals(null, this)) return true;
            if(ReferenceEquals(null, other) && ZoneType == GravityZoneType.none) return true;
            if(ReferenceEquals(null, this) && other.ZoneType == GravityZoneType.none) return true;
            return ReferenceEquals(this, other);
        }

        public static bool operator==(BaseGravityZone a,BaseGravityZone b) { return a.Equals(b); }
        public static bool operator!=(BaseGravityZone a,BaseGravityZone b) { return !a.Equals(b); }

        public void UpdateZone( BaseCharacterController controller)
        {
            if (controller == null)
                return;
            switch (ZoneType)
            {
                case GravityZoneType.converging:
                    GravityDirection = ZonePosition - controller.transform.position;
                    GravityScale = ComputeScaleOverDistance(controller.transform.position);
                    break;
                case GravityZoneType.diverding:
                    GravityDirection = controller.transform.position - ZonePosition;
                    GravityScale = ComputeScaleOverDistance(controller.transform.position);
                    break;
                default:
                    GravityScale = FixedGravityScale;
                    break;
            }
            GravityDirection.Normalize();
        }

        private float ComputeScaleOverDistance(Vector3 point)
        {
            if (GravityScaleOverDistance == null)
                return FixedGravityScale;
            float distance = Vector3.Distance(ZonePosition, point);
            return GravityScaleOverDistance.Evaluate(distance);
        }

    }
}