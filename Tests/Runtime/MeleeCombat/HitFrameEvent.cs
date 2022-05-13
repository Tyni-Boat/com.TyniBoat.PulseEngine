using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.MeleeCombat
{
    /// <summary>
    /// Event trigger hit make an attack connect
    /// </summary>
    [CreateAssetMenu(menuName = "New Hit Contact Event", fileName = "HitFrame")]
    public class HitFrameEvent : AnimancerEvent
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        public Direction _hitDirection;

        public float _hitDamageMultiplier = 1;

        public Vector3 _boxSize = Vector3.one;

        public Vector3 _boxOffset = Vector3.zero;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        public override void Process(AnimancerMachine emitter, float delta, float normalizedTime)
        {
            if (emitter == null)
                return;
            Vector3 center = emitter.transform.position;
            if (emitter.TryGetComponent<Rigidbody>(out Rigidbody body))
            {
                center = body.worldCenterOfMass;
            }
            center += _boxOffset + emitter.transform.forward * _boxSize.z * 0.5f;
            List<Collider> overlaps = new List<Collider>(Physics.OverlapBox(center, _boxSize, emitter.transform.rotation));
            overlaps?.ForEach(o =>
            {
                if (o.gameObject != emitter.gameObject)
                    o.SendMessage("GetHit", new Vector3((int)_hitDirection, _hitDamageMultiplier), SendMessageOptions.DontRequireReceiver);
            });
        
        PulseDebug.DrawCube(center, _boxSize, emitter.transform.rotation, overlaps.Count > 0 ? Color.magenta : Color.white);
        }

    #endregion

    #region Private Functions #####################################################

    #endregion

    #region Jobs      #############################################################

    #endregion

    #region MonoBehaviours ########################################################

    #endregion
}

}