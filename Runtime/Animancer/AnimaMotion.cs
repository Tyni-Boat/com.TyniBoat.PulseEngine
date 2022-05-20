using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.Animancer
{

    [CreateAssetMenu(fileName = "AnimaClip", menuName = "Animancer/New AnimaClip", order = 1)]
    public class AnimaMotion : ScriptableResource, IEquatable<AnimaMotion>
    {
        #region Constants #############################################################

        #endregion

        #region Properties ############################################################


        [field: SerializeField] public Hash128 hash { get; set; }
        [field: SerializeField] public string Statename { get; set; }
        [field: SerializeField] public AnimationClip[] Clips { get; set; }
        [field: SerializeField] public float TransitionTime { get; set; }
        [field: SerializeField] public int Priority { get; set; }
        [field: SerializeField][HideInInspector] public RuntimeAnimatorController BlendController { get; set; }
        [field: SerializeField] public bool BlendControllerDualParameters { get; set; }
        [field: SerializeField] public bool UseRootMotion { get; set; }
        [field: SerializeField] public bool MotionAlongSurface { get; set; }
        public Action StateEnterAction { get; set; }
        public Action StateExitAction { get; set; }
        public Action<float> UpdateAction { get; set; }


        #endregion

        #region Variables #############################################################

        [SerializeReference] public List<AnimancerEvent> Events = new List<AnimancerEvent>();

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Generate the motion hash
        /// </summary>
        public void GenereateHash()
        {
            var h = new Hash128();
            h.Append(Statename);
            if(Clips != null)
            {
                foreach(var c in Clips)
                {
                    if(c == null) continue; 
                    string name = c.name;
                    h.Append(name);
                }
            }
            h.Append(Priority);
            if (Events != null) {
                foreach (var e in Events) { 
                    if(e == null) continue;
                    h.Append(e.GetType().Name);
                    h.Append(e.StartTime);
                    h.Append(e.EndTime);
                }
            }
            hash = h;
        }

        public bool Equals(AnimaMotion other)
        {
            if (hash != default && other.hash != default)
                return hash == other.hash;
            if (ReferenceEquals(other, null))
                return false;
            return other.Id.Equals(Id);
        }

        public static bool operator ==(AnimaMotion a, AnimaMotion b) => (ReferenceEquals(a,null) ^ ReferenceEquals(b, null)) ? false : (ReferenceEquals(a, null) && ReferenceEquals(b, null) ? true : a.Equals(b));
        public static bool operator !=(AnimaMotion a, AnimaMotion b) => (ReferenceEquals(a, null) ^ ReferenceEquals(b, null)) ? true : (ReferenceEquals(a, null) && ReferenceEquals(b, null) ? false : !a.Equals(b));

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void OnEnable()
        {
            //BlendController = JsonUtility.FromJson<UnityEditor.Animations.AnimatorController>(serializedController);
        }

        #endregion
    }

}