using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{
    /// <summary>
    /// 3D space gravity zone
    /// </summary>
    public class GravityZone3d : MonoBehaviour
    {
        public static BaseGravityZone Zero
        {
            get
            {
                var zone = new BaseGravityZone();
                zone.FixedGravityScale = 0;
                zone.GravityDirection = Vector3.zero;
                zone.GravityScaleOverDistance = null;
                zone.ZoneType = GravityZoneType.global;
                return zone;
            }
        }

        public static BaseGravityZone Earth
        {
            get
            {
                var zone = new BaseGravityZone();
                zone.FixedGravityScale = Physics.gravity.magnitude;
                zone.GravityDirection = Physics.gravity.normalized;
                zone.GravityScaleOverDistance = null;
                zone.ZoneType = GravityZoneType.global;
                return zone;
            }
        }

        [SerializeField] private BaseGravityZone _zone;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<BaseCharacterController>(out var controller))
            {
                controller.GravityZone = _zone;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<BaseCharacterController>(out var controller))
            {
                if (controller.GravityZone == _zone)
                    controller.GravityZone = _zone;
            }
        }
    }
}