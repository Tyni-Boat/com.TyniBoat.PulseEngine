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
        public Vector3 rotation;
        public Vector3 scale;

        /// <summary>
        /// Extract transform params from a transform t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static TransformParams FromTransform(Transform t)
        {
            if (t == null)
                return default;
            return new TransformParams { position = t.localPosition, rotation = t.localRotation.eulerAngles, scale = t.localScale };
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
            t.localRotation = Quaternion.Euler(rotation);
            t.localScale = scale;
        }
    }

}