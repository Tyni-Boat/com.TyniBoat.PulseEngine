using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PulseEngine.Animancer
{

    /// <summary>
    /// Represent a motion in the state machine.
    /// </summary>
    public class AnimaMotionNode : IEquatable<AnimaMotionNode>, IEquatable<AnimaMotion>
    {
        #region Properties ############################################################################## 

        public Hash128 Hash { get; private set; }
        public Func<bool> Condition { get; private set; }
        public Func<Vector2> ControlParameters { get; private set; }
        public AnimaMotion Motion { get; private set; }
        public AnimationMixerPlayable MotionPlayable { get; private set; }
        public AvatarMask MotionMask { get; private set; }
        public int PlayablePortIndex { get; set; } = -1;
        public int Priority { get; set; }
        public float AnimDuration { get; internal set; }
        public float AnimExpiration { get; set; }
        public bool LoopMotion { get; set; }

        #endregion

        #region Functions ############################################################################## 

        /// <summary>
        /// Reset the motion play node.
        /// </summary>
        public void Reset()
        {
            Motion = null;
            AnimDuration = 0;
            Condition = null;
            Hash = default;
            MotionPlayable = default;
            PlayablePortIndex = -1;
        }

        private bool InitMotion(PlayableGraph _playableGraph, AnimaMotion motion, AvatarMask mask = null)
        {
            if (!_playableGraph.IsValid() || motion == null)
                return false;

            Reset();
            motion.GenereateHash();

            //Animation Mixers
            var clipNodes = new AnimationClipPlayable[motion.Clips.Length];
            for (int i = 0; i < motion.Clips.Length; i++)
            {
                if (motion.Clips[i] == null)
                    continue;
                clipNodes[i] = AnimationClipPlayable.Create(_playableGraph, motion.Clips[i]);
                if (motion.Clips[i].length > AnimDuration)
                    AnimDuration = motion.Clips[i].length;
            }

            //Mixers
            var animMixer = AnimationMixerPlayable.Create(_playableGraph, clipNodes.Length + 1);

            for (int i = 0; i < clipNodes.Length; i++)
            {
                //connect child mixers to main mixers
                _playableGraph.Connect(clipNodes[i], 0, animMixer, i);
            }

            Tools.AddAnimationPosePlayable(_playableGraph, animMixer, clipNodes.Length);

            //Weight setting 2
            animMixer.SetInputWeight(0, 1);

            MotionPlayable = animMixer;
            Motion = motion;
            Hash = motion.hash;
            MotionMask = mask;
            return true;
        }

        #endregion

        #region Statics ############################################################################## 

        /// <summary>
        /// Create a motion play node on the graph.
        /// </summary>
        /// <param name="motion">the motion data</param>
        /// <param name="graph">the graph</param>
        /// <param name="condition">the added condition</param>
        /// <param name="parameters">the blend parameters</param>
        /// <param name="mask">the avatar mask</param>
        /// <returns></returns>
        public static AnimaMotionNode CreateMotionPlay(AnimaMotion motion, PlayableGraph graph, Func<bool> condition = null, Func<Vector2> parameters = null, AvatarMask mask = null)
        {
            if (motion == null || !graph.IsValid())
                return null;
            AnimaMotionNode motionPlay = new AnimaMotionNode();
            if (motionPlay.InitMotion(graph, motion, mask))
            {
                motionPlay.Condition = condition;
                motionPlay.ControlParameters = parameters;
                return motionPlay;
            }
            return null;
        }

        #endregion

        #region Interfaces overrides ############################################################################## 

        public bool Equals(AnimaMotionNode other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return Hash == other.Hash;
        }

        public bool Equals(AnimaMotion other)
        {
            if (ReferenceEquals(other, null))
                return false;
            other.GenereateHash();
            return Hash == other.hash;
        }

        public bool Equals(object other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (other is AnimaMotionNode)
                return Equals((AnimaMotionNode)other);
            return false;
        }

        public static bool operator ==(AnimaMotionNode a, AnimaMotionNode b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;
            return a.Equals(b);
        }

        public static bool operator !=(AnimaMotionNode a, AnimaMotionNode b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return false;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return true;
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"AnimMotionPlay[motion={Motion?.Name}; hash={Hash}; duration={AnimDuration}]";
        }

        #endregion
    }
}
