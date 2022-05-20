using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PulseEngine.Animancer
{
    /// <summary>
    /// Represent an anima state machine maskable layer
    /// </summary>
    public class AnimaMaskLayer
    {
        #region Static Functions #############################################################

        /// <summary>
        /// Create an anima state machine maskable layer.
        /// </summary>
        /// <param name="graph">The playable graph where the layermask will be created.</param>
        /// <param name="parentMixer">The layer mixer on wich the layermask will be connected</param>
        /// <param name="mask">the mask of the layermask</param>
        /// <returns></returns>
        public static AnimaMaskLayer CreateMaskLayer(PlayableGraph graph, AnimationLayerMixerPlayable parentMixer, AvatarMask mask)
        {
            if (!graph.IsValid())
                return null;
            if (!parentMixer.IsValid())
                return null;
            if (mask == null)
                return null;
            var tmp = new AnimaMaskLayer();
            tmp._maskHash = CalculateMaskHash(mask);
            tmp._maskMixer = AnimationLayerMixerPlayable.Create(graph, 1);
            Tools.AddAnimationPosePlayable(graph, tmp._maskMixer, 0);
            int portIndex = -1;
            //get free input
            int inputCount = parentMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                if (!parentMixer.GetInput(i).IsValid())
                {
                    portIndex = i;
                    break;
                }
            }
            //Connection
            if (portIndex < 0)
            {
                portIndex = parentMixer.AddInput(tmp._maskMixer, 0, 0);
            }
            else
            {
                parentMixer.ConnectInput(portIndex, tmp._maskMixer, 0);
            }

            parentMixer.SetLayerMaskFromAvatarMask((uint)portIndex, mask);
            tmp._maskIndex = portIndex;
            tmp._parentMixer = parentMixer;
            tmp._currentMaskPlayMotion = new AnimaMotionNode();
            tmp._lastMaskPlayMotion = new AnimaMotionNode();
            return tmp;
        }

        /// <summary>
        /// Calculate the hash of an avatar mask.
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static Hash128 CalculateMaskHash(AvatarMask mask)
        {
            if (mask == null)
                return new Hash128();
            var hash = new Hash128();
            hash.Append(mask.name);
            for (int i = 0; i < mask.transformCount; i++)
            {
                hash.Append(mask.GetTransformPath(i));
            }
            foreach (AvatarMaskBodyPart part in Enum.GetValues(typeof(AvatarMaskBodyPart)))
            {
                if (part != AvatarMaskBodyPart.LastBodyPart)
                    hash.Append($"{part}-{mask.GetHumanoidBodyPartActive(part)}");
            }
            return hash;
        }

        #endregion

        #region Constants #############################################################

        /// <summary>
        /// the default transition duration.
        /// </summary>
        private float DefaultTransitionTime { get => AnimancerMachine.DEFAULT_TRANSITION_TIME; }

        #endregion

        #region Variables #############################################################

        /// <summary>
        /// The mask mixer
        /// </summary>
        private AnimationLayerMixerPlayable _maskMixer;

        /// <summary>
        /// The parent mixer. the one on wich the mask mixer is connected.
        /// </summary>
        private AnimationLayerMixerPlayable _parentMixer;

        /// <summary>
        /// The layermask input port index in parent mixer.
        /// </summary>
        private int _maskIndex;

        /// <summary>
        /// the mask hash of this layer
        /// </summary>
        private Hash128 _maskHash;

        /// <summary>
        /// The list of motion waiting to be looped on this layer.
        /// </summary>
        private List<AnimaMotionNode> _loopMaskAnimQueue = new List<AnimaMotionNode>();

        /// <summary>
        /// The list of motions waiting to be played once on this layer.
        /// </summary>
        private List<AnimaMotionNode> _pendingMaskAnimQueue = new List<AnimaMotionNode>();

        /// <summary>
        /// the currently playing motion on this layer
        /// </summary>
        private AnimaMotionNode _currentMaskPlayMotion;

        /// <summary>
        /// the last playing motion on this layer while in transition
        /// </summary>
        private AnimaMotionNode _lastMaskPlayMotion;

        /// <summary>
        /// is an transition occuring on this layer?
        /// </summary>
        private bool _inMaskTransition;

        /// <summary>
        /// The speed of the current transition.
        /// </summary>
        private float _maskTransitionSpeed;

        /// <summary>
        /// the blend parameters of the current
        /// </summary>
        private Vector2 _maskBlendValues;


        #endregion

        #region Properties ############################################################

        /// <summary>
        /// Active when the machine got initialized well.
        /// </summary>
        internal bool IsReady
        {
            get => _maskMixer.IsValid() && _currentMaskPlayMotion != null && _lastMaskPlayMotion != null;
        }

        /// <summary>
        /// Active when there is no motion playing or the current mask motion can now transition to another one
        /// </summary>
        public bool CurrentMaskMotionCanTransition
        {
            get
            {
                if (_currentMaskPlayMotion == null || !_currentMaskPlayMotion.MotionPlayable.IsValid())
                {
                    return true;
                }
                float currTime = (float)_currentMaskPlayMotion.MotionPlayable.GetTime();
                float trTime = _currentMaskPlayMotion.Motion.TransitionTime > 0 ? _currentMaskPlayMotion.Motion.TransitionTime : DefaultTransitionTime;
                return currTime >= (_currentMaskPlayMotion.AnimDuration - trTime);
            }
        }

        /// <summary>
        /// active when there is no mask motion playing or the playing motion ended.
        /// </summary>
        public bool CurrentMaskMotionFinnished
        {
            get
            {
                if (_currentMaskPlayMotion == null || !_currentMaskPlayMotion.MotionPlayable.IsValid())
                {
                    return true;
                }
                float currTime = (float)_currentMaskPlayMotion.MotionPlayable.GetTime();
                return currTime >= _currentMaskPlayMotion.AnimDuration;
            }
        }

        /// <summary>
        /// The currently playing animation hash
        /// </summary>
        public Hash128 CurrentMotionHash
        {
            get
            {
                if (_currentMaskPlayMotion == null)
                    return new Hash128();
                return _currentMaskPlayMotion.Hash;
            }
        }

        /// <summary>
        /// Get the mask hash
        /// </summary>
        public Hash128 MaskHash { get => _maskHash; }

        #endregion

        #region Internal Layer Functions #####################################################

        /// <summary>
        /// Evaluate the layer. it must be done on the same function updating his parent state machine.
        /// </summary>
        /// <param name="delta"></param>
        internal void Evaluate(float delta)
        {
            if (!IsReady)
                return;
            _inMaskTransition = TransitionToMaskCurrentState(delta);
            EvaluateMaskFinnishState(delta);
        }

        /// <summary>
        /// Update the layer parameters.
        /// </summary>
        /// <param name="delta"></param>
        internal void Update(AnimancerMachine machine, float delta)
        {
            if (!IsReady)
                return;
            ProcessMaskAnimOnceQueue(delta);
            ProcessMaskAnimLoopQueue();
            EvaluateMaskMotionParams();
            UpdateMotion(machine, delta);
        }

        /// <summary>
        /// Add an motion to the looping animation queue
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        internal bool AddToLoopQueue(AnimaMotionNode motion)
        {
            if (_loopMaskAnimQueue.Contains(motion))
                return false;
            motion.LoopMotion = true;
            _loopMaskAnimQueue.Add(motion);
            return true;
        }

        /// <summary>
        /// Add an motion to the one time animation queue
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        internal bool AddToPendingQueue(AnimaMotionNode motion)
        {
            _pendingMaskAnimQueue.Add(motion);
            return true;
        }

        /// <summary>
        /// Play an animation immediatly on this layer
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        internal bool ImmediatePlay(AnimaMotionNode motion)
        {
            if (motion == null)
                return false;
            return Internal_MaskPlay(motion);
        }

        /// <summary>
        /// Dispose this layer.
        /// </summary>
        internal void DisposeLayer()
        {
            if (_maskMixer.IsValid())
                _maskMixer.SetDone(true);
            if (_parentMixer.IsValid() && _maskIndex.InInterval(0, _parentMixer.GetInputCount()))
                _parentMixer.DisconnectInput(_maskIndex);
            _maskMixer = default;
        }

        #endregion

        #region Private Mask Layer Functions  #############################################################

        /// <summary>
        /// Try to play an animation on the mask layer
        /// </summary>
        /// <param name="motionPlay"></param>
        /// <returns></returns>
        private bool Internal_MaskPlay(AnimaMotionNode motionPlay)
        {
            if (motionPlay == null)
                return false;
            if (motionPlay.MotionMask == null)
                return false;
            if (!IsReady)
                return false;
            if (!_maskMixer.IsValid())
                return false;
            //hash comparision
            if (_currentMaskPlayMotion.Hash == motionPlay.Hash)
                return false;
            //conditions comparision
            if (motionPlay.Condition != null)
            {
                if (!motionPlay.Condition.Invoke())
                    return false;
                if (_currentMaskPlayMotion.Condition != null)
                {
                    if (_currentMaskPlayMotion.Condition.Invoke())
                    {
                        //Conditions are the same. do something?
                    }
                }
            }
            //priority compare
            if (_currentMaskPlayMotion.Motion)
            {
                int p_Compare = _currentMaskPlayMotion.Priority.CompareTo(motionPlay.Priority);
                if (p_Compare > 0)
                {
                    if (!_currentMaskPlayMotion.MotionPlayable.IsValid())
                    {
                        return false;
                    }
                    float currTime = (float)_currentMaskPlayMotion.MotionPlayable.GetTime();
                    float rqTransTime = motionPlay.Motion.TransitionTime > 0 ? motionPlay.Motion.TransitionTime : DefaultTransitionTime;
                    if (currTime < (_currentMaskPlayMotion.AnimDuration - rqTransTime))
                    {
                        return false;
                    }
                }
            }
            //look for transition
            if (_inMaskTransition)
                return false;

            //set current to old
            ReplacePlayMotion();

            int portIndex = -1;

            //get free input
            int inputCount = _maskMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                if (!_maskMixer.GetInput(i).IsValid())
                {
                    portIndex = i;
                    break;
                }
            }

            //Connection
            if (portIndex < 0)
            {
                portIndex = _maskMixer.AddInput(motionPlay.MotionPlayable, 0, _currentMaskPlayMotion.Motion ? 0 : 1);
            }
            else
            {
                _maskMixer.ConnectInput(portIndex, motionPlay.MotionPlayable, 0);
            }

            _parentMixer.SetInputWeight(_maskIndex, 1);
            _maskTransitionSpeed = 1 / (motionPlay.Motion.TransitionTime > 0 ? motionPlay.Motion.TransitionTime : DefaultTransitionTime);
            _inMaskTransition = true;
            motionPlay.PlayablePortIndex = portIndex;
            _currentMaskPlayMotion = motionPlay;

            return true;
        }

        /// <summary>
        /// Set the current motion as the last mootin
        /// </summary>
        private void ReplacePlayMotion()
        {
            if (_lastMaskPlayMotion.MotionPlayable.IsValid())
            {
                _lastMaskPlayMotion.MotionPlayable.SetDone(true);
            }
            if (_lastMaskPlayMotion.PlayablePortIndex.InInterval(0, _maskMixer.GetInputCount()))
            {
                _maskMixer.DisconnectInput(_lastMaskPlayMotion.PlayablePortIndex);
            }
            _lastMaskPlayMotion = _currentMaskPlayMotion;
        }

        /// <summary>
        /// Reset the values of the current Mask animation object and invalidate it.
        /// </summary>
        private void ResetCurrentMaskValues()
        {
            _currentMaskPlayMotion?.Reset();
        }

        /// <summary>
        /// Reset the values of the last mask animation object and invalidate it.
        /// </summary>
        private void ResetLastMaskValues()
        {
            _lastMaskPlayMotion?.Reset();
        }

        /// <summary>
        /// Evaluate the last mask animation and dispose of it when it finnished.
        /// </summary>
        /// <param name="delta"></param>
        private void EvaluateMaskFinnishState(float delta)
        {
            if (!IsReady)
                return;
            if (!_maskMixer.IsValid())
                return;
            if (_inMaskTransition)
                return;
            if (_lastMaskPlayMotion.MotionPlayable.IsValid() && _lastMaskPlayMotion.PlayablePortIndex.InInterval(0, _maskMixer.GetInputCount()))
            {
                if (_lastMaskPlayMotion.MotionPlayable.GetTime() > _lastMaskPlayMotion.AnimDuration)
                {
                    _lastMaskPlayMotion.MotionPlayable.SetDone(true);
                    _maskMixer.DisconnectInput(_lastMaskPlayMotion.PlayablePortIndex);
                    ResetLastMaskValues();
                }
            }
            if (_currentMaskPlayMotion.MotionPlayable.IsValid() && _currentMaskPlayMotion.PlayablePortIndex.InInterval(0, _maskMixer.GetInputCount()))
            {
                if (_currentMaskPlayMotion.MotionPlayable.GetTime() > _currentMaskPlayMotion.AnimDuration)
                {
                    if (!_currentMaskPlayMotion.LoopMotion)
                    {
                        ReplacePlayMotion();
                        _currentMaskPlayMotion = new AnimaMotionNode();
                    }
                }
                if (_currentMaskPlayMotion.LoopMotion && _currentMaskPlayMotion.Condition != null && !_currentMaskPlayMotion.Condition.Invoke())
                {
                    ReplacePlayMotion();
                    _currentMaskPlayMotion = new AnimaMotionNode();
                }
            }
        }

        /// <summary>
        /// Evalauate the current mask motion parameters
        /// </summary>
        /// <param name="delta"></param>
        private void EvaluateMaskMotionParams()
        {
            if (_currentMaskPlayMotion == null)
                return;
            if (_currentMaskPlayMotion.ControlParameters != null)
                _maskBlendValues = _currentMaskPlayMotion.ControlParameters.Invoke();
            if (_currentMaskPlayMotion.ControlParameters != null && _currentMaskPlayMotion.MotionPlayable.IsValid())
            {
                int inputs = _currentMaskPlayMotion.MotionPlayable.GetInputCount();
                float min = 0;
                float max = inputs - 1;
                for (float i = 0; i < max; i++)
                {
                    _currentMaskPlayMotion.MotionPlayable.SetInputWeight((int)i, Tools.PeakCurve(_maskBlendValues.x, i / (max - 1), min, (max - 1)));
                }
            }
        }

        /// <summary>
        /// Make the transition from last mask animation to current one.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private bool TransitionToMaskCurrentState(float delta)
        {
            if (!IsReady)
                return false;
            if (!_maskMixer.IsValid())
                return false;
            bool transition = false;
            float debugWeight = 0;
            int lastStateIndex = _lastMaskPlayMotion.PlayablePortIndex;
            if (lastStateIndex.InInterval(0, _maskMixer.GetInputCount()))
            {
                float lastStateWeight = _maskMixer.GetInputWeight(lastStateIndex);
                if (lastStateIndex >= _currentMaskPlayMotion.PlayablePortIndex && lastStateWeight > 0)
                {
                    lastStateWeight -= delta * _maskTransitionSpeed;
                    debugWeight += lastStateWeight;
                    transition = true;
                }
                lastStateWeight = Mathf.Clamp01(lastStateWeight);
                _maskMixer.SetInputWeight(lastStateIndex, lastStateWeight);
            }
            if (_currentMaskPlayMotion.PlayablePortIndex.InInterval(0, _maskMixer.GetInputCount()) && _maskMixer.GetInputWeight(_currentMaskPlayMotion.PlayablePortIndex) < 1)
            {
                float currentStateWeight = _maskMixer.GetInputWeight(_currentMaskPlayMotion.PlayablePortIndex);
                if (_currentMaskPlayMotion.PlayablePortIndex < lastStateIndex)
                {
                    currentStateWeight = 1;
                }
                else
                {
                    currentStateWeight += delta * _maskTransitionSpeed;
                    debugWeight += currentStateWeight;
                    transition = true;
                }
                currentStateWeight = Mathf.Clamp01(currentStateWeight);
                _maskMixer.SetInputWeight(_currentMaskPlayMotion.PlayablePortIndex, currentStateWeight);
            }
            return transition;
        }

        /// <summary>
        /// Procedd the queue of mask animations waition for condition to be played in loop.
        /// </summary>
        private void ProcessMaskAnimLoopQueue()
        {
            if (_loopMaskAnimQueue == null || _loopMaskAnimQueue.Count <= 0)
                return;
            for (int i = _loopMaskAnimQueue.Count - 1; i >= 0; i--)
            {
                if (_loopMaskAnimQueue[i].Condition == null)
                {
                    _loopMaskAnimQueue.RemoveAt(i);
                    continue;
                }
                if (!_loopMaskAnimQueue[i].Condition.Invoke())
                    continue;
                if (_currentMaskPlayMotion != null && _currentMaskPlayMotion == _loopMaskAnimQueue[i])
                    continue;
                Internal_MaskPlay(_loopMaskAnimQueue[i]);
                break;
            }
        }

        /// <summary>
        /// Procedd the queue of mask animations waiting for condition to be played once.
        /// </summary>
        /// <param name="delta"></param>
        private void ProcessMaskAnimOnceQueue(float delta)
        {
            if (_pendingMaskAnimQueue == null || _pendingMaskAnimQueue.Count <= 0)
                return;
            for (int i = _pendingMaskAnimQueue.Count - 1; i >= 0; i--)
            {
                //if there is no condition, the queued animation is invalid.
                if (_pendingMaskAnimQueue[i].Condition == null)
                {
                    _pendingMaskAnimQueue.RemoveAt(i);
                    continue;
                }
                //if the condition was not meet, skip
                if (!_pendingMaskAnimQueue[i].Condition.Invoke())
                {
                    //if the animation has an expiration delay, decrease it untill the animation expire and will be remove from the queue
                    if (_pendingMaskAnimQueue[i].AnimExpiration > 0)
                    {
                        _pendingMaskAnimQueue[i].AnimExpiration -= delta;
                        if (_pendingMaskAnimQueue[i].AnimExpiration <= 0)
                        {
                            _pendingMaskAnimQueue.RemoveAt(i);
                        }
                    }
                    continue;
                }
                //prevent the queued to be played if the same anim is playing
                if (_currentMaskPlayMotion != null && _currentMaskPlayMotion == _pendingMaskAnimQueue[i])
                    continue;
                //try to play the anim and remove it from the queue
                if (Internal_MaskPlay(_pendingMaskAnimQueue[i]))
                {
                    _pendingMaskAnimQueue.RemoveAt(i);
                    break;
                }
                else
                {
                    _pendingMaskAnimQueue.RemoveAt(i);
                    continue;
                }
            }
        }

        /// <summary>
        /// Update the current mask motion events
        /// </summary>
        /// <param name="delta"></param>
        private void UpdateMotion(AnimancerMachine machine, float delta)
        {
            if (_currentMaskPlayMotion != null && _currentMaskPlayMotion.Motion != null)
            {
                if (_currentMaskPlayMotion.MotionPlayable.IsValid())
                {
                    if (_currentMaskPlayMotion.Motion.Events != null)
                    {
                        for (int i = 0; i < _currentMaskPlayMotion.Motion.Events.Count; i++)
                        {
                            _currentMaskPlayMotion.Motion.Events[i].Evaluate(machine, delta, (float)_currentMaskPlayMotion.MotionPlayable.GetTime(), _currentMaskPlayMotion.AnimDuration);
                        }
                    }
                }
            }
        }

        #endregion
    }
}