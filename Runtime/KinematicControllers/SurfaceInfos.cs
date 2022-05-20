using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CharacterControl
{
    /// <summary>
    /// Informations about a suface
    /// </summary>
    [Serializable]
    public class SurfaceInfos
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        [field:SerializeField] public Collider surfaceCollider { get; set; }
        [field:SerializeField] public Collider lastSurfaceCollider { get; set; }
        [field:SerializeField] public Vector3 Point { get; set; }
        [field:SerializeField] public Vector3 Normal { get; set; }
        [field:SerializeField] public Vector3 NormalLarge { get; set; }
        [field:SerializeField] public Vector3 TrueNormal { get; set; }
        [field:SerializeField] public Vector3 CentralNormal { get; set; }
        [field:SerializeField] public float LargeAngle { get; set; }
        [field:SerializeField] public float InnerAngle { get; set; }
        [field:SerializeField] public float Distance { get; set; }
        public Vector3 OffsetNormal { get; set; }
        public float PointOffset { get; set; }
        [field:SerializeField] public bool IsOnSurfaceLarge { get; set; }
        [field:SerializeField] public bool IsOnSurface { get; set; }
        [field:SerializeField] public bool IsSurfaceStable { get; set; }
        [field:SerializeField] public bool NoGravityForce { get; set; }
        public bool IsWalkableStep { get; set; }

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}