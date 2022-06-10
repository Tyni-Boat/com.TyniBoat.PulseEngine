using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{

    public abstract class MovingPlatform : MonoBehaviour
    {

        protected Dictionary<Transform, TransformParams> _LocalPoints = new Dictionary<Transform, TransformParams>();

        public Quaternion UpdatePositionAndGetRotation(Transform tr, Vector3 position, bool updateLocal = false)
        {
            if (tr == null)
                return Quaternion.Euler(Vector3.zero);
            //add the new entry
            if (!_LocalPoints.ContainsKey(tr))
            {
                _LocalPoints.Add(tr, new TransformParams
                {
                    position = transform.InverseTransformPoint(position),
                    orientation = transform.InverseTransformDirection(tr.forward),
                    rotation = Quaternion.LookRotation(tr.forward),
                    scale = tr.localScale,
                    oldPosition = position,
                });
                return Quaternion.Euler(Vector3.zero);
            }

            var entry = _LocalPoints[tr];
            //Compute Rotation
            Vector3 rDiff = (Quaternion.LookRotation(transform.TransformDirection(entry.orientation).normalized).eulerAngles - entry.rotation.eulerAngles);
            rDiff.x = rDiff.z = 0;
            var childRot = Quaternion.Euler(rDiff);
            entry.rotation = Quaternion.LookRotation(transform.TransformDirection(entry.orientation).normalized);
            entry.oldPosition = transform.TransformPoint(entry.position);
            _LocalPoints[tr] = entry;

            //update locals
            if (updateLocal)
            {
                _LocalPoints[tr] = new TransformParams
                {
                    position = transform.InverseTransformPoint(position),
                    orientation = transform.InverseTransformDirection(tr.forward),
                    rotation = Quaternion.LookRotation(tr.forward),
                    scale = tr.localScale,
                    oldPosition = position,
                };
                return Quaternion.Euler(Vector3.zero);
            }
            return childRot;
        }

        public void TakeOff(Transform tr)
        {
            if (tr == null)
                return;
            //add the new entry
            if (_LocalPoints.ContainsKey(tr))
            {
                _LocalPoints.Remove(tr);
            }
        }
    }
}