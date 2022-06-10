using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{


    /// <summary>
    /// The Paramaters of a transform.
    /// </summary>
    [System.Serializable]
    public struct TransformParams
    {
        public Vector3 position;
        public Vector3 orientation;
        public Vector3 scale;

        public Vector3 oldPosition;
        public Quaternion rotation;

        /// <summary>
        /// Extract transform params from a transform t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static TransformParams FromTransform(Transform t)
        {
            if (t == null)
                return default;
            return new TransformParams { position = t.localPosition, orientation = t.localRotation.eulerAngles, scale = t.localScale, rotation = t.localRotation };
        }

        /// <summary>
        /// Set this transformParams on the transforn t
        /// </summary>
        /// <param name="t"></param>
        public void SetOnTransform(Transform t)
        {
            if (t == null)
                return;
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
        }

        public static TransformParams Null => new TransformParams { orientation = default, position = default, rotation = default, scale = default, oldPosition = default };
    }

}