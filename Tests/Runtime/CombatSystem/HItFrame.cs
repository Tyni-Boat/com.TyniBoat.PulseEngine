using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    [System.Serializable]
    public class HItFrame
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

        [field: SerializeField] public float ImpactTime { get; set; }
        [field: SerializeField] public float EndTime { get; set; }
        [field: SerializeField] public int HitEveryXFrames { get; set; }
        [field: SerializeField] public Direction ImpactDirection { get; set; }
        [field: SerializeField] public HitIntensity ImpactIntensity { get; set; }
        [field: SerializeField] public HumanBodyBones SourceBone { get; set; }
        [field: SerializeField] public float ImpactDamagesmultiplier { get; set; } = 1;
        [field: SerializeField] public Vector3 BoxOffset { get; set; }
        [field: SerializeField] public Vector3 BoxSize { get; set; } = Vector3.one;
        [field: SerializeField] public bool UseWeaponCollider { get; set; }

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Get the total amount of damages based on character stats and the skill stats.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public float CalculateDamages(Character character)
        {
            //TODO: change this
            return ImpactDamagesmultiplier;
        }

        /// <summary>
        /// Return true if there is an impact this frame
        /// </summary>
        /// <param name="normalizedTime"></param>
        /// <returns></returns>
        public bool ImpactFrame(float normalizedTime, int frameCount)
        {
            //Check if we are in range
            if (normalizedTime < ImpactTime || normalizedTime > EndTime)
                return false;
            //Is it a multiframe hit?
            if (HitEveryXFrames > 0)
                return frameCount % HitEveryXFrames == 0;
            return true;
        }

        #endregion

        #region Private Functions #####################################################

        #endregion
    }

}