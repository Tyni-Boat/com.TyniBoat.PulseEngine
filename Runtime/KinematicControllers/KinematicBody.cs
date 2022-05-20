using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{
    /// <summary>
    /// Represent a Kinematic body
    /// </summary>
    [Serializable]
    public struct KinematicBody
    {
        public KinematicBodyType type;
        [HideInInspector] public Vector3 centerOfMass;
        public Vector3 boxExtends;
        [HideInInspector] public Vector3 capsuleSecondaryCenter;
        [HideInInspector] public Vector3 capsulePrimaryCenter;
        [HideInInspector] public Vector3 gameObjectPosition;
        [HideInInspector] public Vector3 gameObjectScale;
        [HideInInspector] public Quaternion gameObjectRotation;
        public float capsuleRadius;
        public float capsuleHeight;
        public float skinDepth;
        public float slopeMaxAngle;
        public float stepMaxHeight;

        [NonSerialized] private Collider _collider;

        public void Update(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            gameObjectPosition = position;
            gameObjectRotation = rotation;
            gameObjectScale = scale;
            capsulePrimaryCenter = position + (rotation * Vector3.up).normalized * (capsuleRadius + slopeMaxAngle);
            capsuleSecondaryCenter = capsulePrimaryCenter + (rotation * Vector3.up).normalized * (capsuleHeight - capsuleRadius * 2);
            centerOfMass = position + (rotation * Vector3.up).normalized * (type == KinematicBodyType.capsule ? (capsuleHeight * 0.5f + slopeMaxAngle) : (boxExtends.y * 0.5f + slopeMaxAngle));
            skinDepth = Mathf.Clamp(skinDepth, 0, capsuleRadius * 0.5f);
        }

        public void Update(ref Collider collider, GameObject gameObject)
        {
            if (_collider != null)
            {
                //store space infos
                Vector3 gameObjectPosition = collider.transform.position;
                Vector3 gameObjectScale = collider.transform.localScale;
                Quaternion gameobjectRotation = collider.transform.rotation;

                //Update Shape values
                Update(gameObjectPosition, gameObjectRotation, gameObjectScale);;
            }

            //Select good collider
            switch (type)
            {
                case KinematicBodyType.capsule:
                    {
                        if (!(collider is CapsuleCollider) || !_collider)
                        {
                            GameObject.Destroy(collider);
                            collider = gameObject.AddComponent<CapsuleCollider>();
                            collider.hideFlags = HideFlags.HideInInspector;
                        }
                        var capsule = collider as CapsuleCollider;
                        if (capsule)
                        {
                            capsule.center = gameObject.transform.InverseTransformPoint(centerOfMass);
                            capsule.radius = capsuleRadius - skinDepth;
                            capsule.height = capsuleHeight - (skinDepth * 2);
                        }
                    }
                    break;
                case KinematicBodyType.box:
                    {
                        if (!(collider is BoxCollider) || !_collider)
                        {
                            GameObject.Destroy(collider);
                            collider = gameObject.AddComponent<BoxCollider>();
                            collider.hideFlags = HideFlags.HideInInspector;
                        }
                        var box = collider as BoxCollider;
                        if (box)
                        {
                            box.center = gameObject.transform.InverseTransformPoint(centerOfMass);
                            box.size = new Vector3(boxExtends.x - skinDepth, boxExtends.y - skinDepth, boxExtends.z - skinDepth);
                        }
                    }
                    break;
            }

            //
            _collider = collider;
        }

        public float GetWidth()
        {
            switch (type)
            {
                case KinematicBodyType.capsule:
                    return capsuleRadius;
                case KinematicBodyType.box:
                    return boxExtends.x;
            }

            return 0;
        }

        public bool RayCastBody(Vector3 direction, LayerMask mask, float maxDist, out RaycastHit castHit)
        {
            castHit = default;

            switch (type)
            {
                case KinematicBodyType.capsule:
                    {
                        Vector3 dir = (capsuleSecondaryCenter - capsulePrimaryCenter).normalized;
                        bool result = Physics.CapsuleCast(capsulePrimaryCenter + dir * skinDepth, capsuleSecondaryCenter - dir * skinDepth, capsuleRadius - (skinDepth * 2), direction.normalized, out castHit, maxDist, mask);
                        return result;
                    }
                case KinematicBodyType.box:
                    {
                        float nDepth = skinDepth * 2;
                        Vector3 size = new Vector3(boxExtends.x - nDepth, boxExtends.y - nDepth, boxExtends.z - nDepth);
                        bool result = Physics.BoxCast(centerOfMass, size * 0.5f, direction.normalized, out castHit, gameObjectRotation, maxDist, mask);;
                        return result;
                    };
            }

            return false;
        }

        public int TestOverlaps(int layerMask, ref Collider[] overlapResults)
        {
            switch (type)
            {
                case KinematicBodyType.capsule:
                    return Physics.OverlapCapsuleNonAlloc(capsulePrimaryCenter, capsuleSecondaryCenter, capsuleRadius, overlapResults, layerMask);
                case KinematicBodyType.box:
                    return Physics.OverlapBoxNonAlloc(centerOfMass, boxExtends * 0.5f, overlapResults, gameObjectRotation, layerMask);
            }
            return 0;
        }

        public Vector3 ClosestPointOnSurface(Vector3 point)
        {
            switch (type)
            {
                case KinematicBodyType.capsule:
                    {
                        Vector3 shapeDir = (capsuleSecondaryCenter - capsulePrimaryCenter).normalized;
                        float innerHeight = (capsuleHeight - (capsuleRadius * 2)) * 0.5f;
                        Vector3 dir = (point - centerOfMass);
                        Vector3 heightProj = Vector3.Project(dir, shapeDir);
                        //in the hemispheres
                        if (heightProj.magnitude > innerHeight)
                        {
                            Vector3 hemiSphereCenter = Vector3.Dot(heightProj.normalized, shapeDir) > 0 ? capsuleSecondaryCenter : capsulePrimaryCenter;
                            Vector3 hDir = (point - hemiSphereCenter);
                            return hemiSphereCenter + hDir.normalized * capsuleRadius;
                        }
                        //in the cylinder
                        else
                        {
                            return centerOfMass + heightProj + (Vector3.ProjectOnPlane(dir, shapeDir).normalized * capsuleRadius);
                        }
                    }
                case KinematicBodyType.box:
                    if (_collider)
                    {
                        var transformMatrix = Matrix4x4.TRS(gameObjectPosition, gameObjectRotation, gameObjectScale);
                        Vector3 pointDir = point - centerOfMass;
                        Vector3 localCenter = _collider.transform.InverseTransformPoint(centerOfMass);
                        Vector3 localDir = _collider.transform.InverseTransformDirection(pointDir);
                        Vector3 localPt = localCenter + localDir;
                        localPt = new Vector3(
                        Mathf.Clamp(localPt.x, -boxExtends.x * 0.5f, boxExtends.x * 0.5f),
                        Mathf.Clamp(localPt.y, 0, boxExtends.y),
                        Mathf.Clamp(localPt.z, -boxExtends.z * 0.5f, boxExtends.z * 0.5f));

                        PulseDebug.DrawRLine(point, _collider.transform.TransformPoint(localPt), Color.white);
                        return _collider.transform.TransformPoint(localPt);
                    }
                    break;
            }
            return point;
        }

        public void Debug(Quaternion rot, Color col, bool gizmo = false)
        {
            if (gizmo)
                Gizmos.color = col;
            switch (type)
            {
                case KinematicBodyType.capsule:
                    if (gizmo)
                    {
                        Gizmos.DrawWireSphere(capsulePrimaryCenter, capsuleRadius);
                        Gizmos.DrawWireSphere(capsuleSecondaryCenter, capsuleRadius);
                    }
                    else
                        PulseDebug.DrawCapsule(capsulePrimaryCenter, capsuleSecondaryCenter, rot, capsuleRadius, col);
                    break;
                case KinematicBodyType.box:
                    if (gizmo)
                    {
                        Gizmos.DrawWireCube(centerOfMass, boxExtends);
                    }
                    else
                        PulseDebug.DrawCube(centerOfMass, boxExtends, rot, col);
                    break;
            }
        }
    }

}