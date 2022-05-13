using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PulseEngine.PhysicSystem
{
    /// <summary>
    /// Add force to event
    /// </summary>
    [CreateAssetMenu(menuName = "Add Force Event", fileName = "ForceEvent")]
    public class AddForceEvent : AnimancerEvent
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        public float intensity = 5;

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
            if (emitter != null)
            {
                if (emitter.TryGetComponent<PhysicSystemComponent>(out var physicSystemComponent))
                {
                    float currentScale = physicSystemComponent.GravityMultiplier;
                    physicSystemComponent.GravityMultiplier = 0;
                    physicSystemComponent.AddForce(intensity, -physicSystemComponent.CurrentGravityDirection, physicSystemComponent.CurrentPhysicSpace != PhysicSpace.onGround, ForceMode.Impulse);
                    physicSystemComponent.StartCoroutine(RestoreGravityScale(physicSystemComponent, 0.25f, currentScale));

                }
            }
        }

        IEnumerator RestoreGravityScale(PhysicSystemComponent phy, float time, float value)
        {
            yield return new WaitForSeconds(time);
            if (phy != null)
                phy.GravityMultiplier = value;
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