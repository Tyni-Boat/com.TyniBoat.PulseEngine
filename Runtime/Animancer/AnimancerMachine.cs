using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


namespace PulseEngine.Animancer
{
    /// <summary>
    /// Represent an anima state machine.
    /// </summary>
    public class AnimancerMachine : MonoBehaviour
    {
        #region Constants #############################################################

        internal const float DEFAULT_TRANSITION_TIME = 0.15f;

        #endregion

        #region Variables #############################################################

        /// <summary>
        /// The animator avatar to use.
        /// </summary>
        [SerializeField] private Avatar _avatar;

        /// <summary>
        /// The update mode to use.
        /// </summary>
        [SerializeField] private AnimatorUpdateMode _updateMode;

        /// <summary>
        /// active the dubug mode?
        /// </summary>
        [SerializeField] private bool _debug;


        /// <summary>
        /// the animator used by the playable graph
        /// </summary>
        private Animator _animator;

        /// <summary>
        /// The playable graph.
        /// </summary>
        private PlayableGraph _playableGraph;

        /// <summary>
        /// The main mixer connecting full body layer and maskable layers
        /// </summary>
        private AnimationLayerMixerPlayable _masterMixer;

        /// <summary>
        /// the full body animation layer
        /// </summary>
        private AnimationLayerMixerPlayable _fullBodyLayerMixer;

        /// <summary>
        /// The animation output on this graph.
        /// </summary>
        private AnimationPlayableOutput _outPut;

        /// <summary>
        /// The list of motion waiting to be looped on full body layer.
        /// </summary>
        private List<AnimaMotionNode> _loopAnimQueue = new List<AnimaMotionNode>();

        /// <summary>
        /// The list of motion waiting to be played once on full body layer.
        /// </summary>
        private List<AnimaMotionNode> _pendingAnimQueue = new List<AnimaMotionNode>();

        /// <summary>
        /// the motion currently be playing on the full body layer.
        /// </summary>
        private AnimaMotionNode _currentPlayMotion;

        /// <summary>
        /// the motion lastly be playing on the full body layer while in transition.
        /// </summary>
        private AnimaMotionNode _lastPlayMotion;

        /// <summary>
        /// is the full body layer in transition?
        /// </summary>
        private bool _inTransition;

        /// <summary>
        /// the current trasition speed.
        /// </summary>
        private float _transitionSpeed;

        /// <summary>
        /// The speed of the animation.
        /// </summary>
        private float _machineTimeScale = 1;

        /// <summary>
        /// the full body layer blend parameter.
        /// </summary>
        private Vector2 _blendValues;

        /// <summary>
        /// The list of active mask layers.
        /// </summary>
        private List<AnimaMaskLayer> _maskLayers = new List<AnimaMaskLayer>();


        #endregion

        #region Properties ############################################################

        /// <summary>
        /// Active when the machine got initialized well.
        /// </summary>
        public bool IsReady
        {
            get => _playableGraph.IsValid() && _masterMixer.IsValid() && _fullBodyLayerMixer.IsValid() && _currentPlayMotion != null && _lastPlayMotion != null;
        }

        /// <summary>
        /// The animation velocity.
        /// </summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>
        /// The currently playing animation hash
        /// </summary>
        public Hash128 CurrentMotionHash
        {
            get
            {
                if (_currentPlayMotion == null)
                    return new Hash128();
                return _currentPlayMotion.Hash;
            }
        }

        /// <summary>
        /// Active when there is no motion playing or the current motion can now transition to another one
        /// </summary>
        public bool CurrentMotionCanTransition
        {
            get
            {
                if (_currentPlayMotion == null || !_currentPlayMotion.MotionPlayable.IsValid())
                {
                    return true;
                }
                float currTime = (float)_currentPlayMotion.MotionPlayable.GetTime();
                float trTime = _currentPlayMotion.Motion.TransitionTime > 0 ? _currentPlayMotion.Motion.TransitionTime : DEFAULT_TRANSITION_TIME;
                return currTime >= (_currentPlayMotion.AnimDuration - trTime);
            }
        }

        /// <summary>
        /// active when there is no motion playing or the playing motion ended.
        /// </summary>
        public bool CurrentMotionFinnished
        {
            get
            {
                if (_currentPlayMotion == null || !_currentPlayMotion.MotionPlayable.IsValid())
                {
                    return true;
                }
                float currTime = (float)_currentPlayMotion.MotionPlayable.GetTime();
                return currTime >= _currentPlayMotion.AnimDuration;
            }
        }

        /// <summary>
        /// Control the time scale of the animation.
        /// </summary>
        public float MachineTimeScale { get => _machineTimeScale; set => _machineTimeScale = value; }

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Play motion on the full body while condition is meet, with a low priority (0).
        /// </summary>
        /// <param name="motion">The motion to play</param>
        /// <param name="condition">the condition to meet</param>
        /// <param name="parameters">the blend parameters</param>
        /// <returns>true if the motion was successfully added to play loop queue</returns>
        public bool PlayWhile(AnimaMotion motion, Func<bool> condition, Func<Vector2> parameters = null)
        {
            if (motion == null || condition == null || _loopAnimQueue == null)
                return false;
            var animPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, condition, parameters);
            if (animPlay == null)
                return false;
            animPlay.Priority = 0;
            if (_loopAnimQueue.Contains(animPlay))
                return false;
            animPlay.LoopMotion = true;
            _loopAnimQueue.Add(animPlay);
            return true;
        }

        /// <summary>
        /// Play motion on the full body when condition is meet, with a medium priority (1) and an expiration time.
        /// </summary>
        /// <param name="motion">motion to play</param>
        /// <param name="condition">condition to meet</param>
        /// <param name="parameters">blend parameters</param>
        /// <param name="expiration">extiration time in seconds</param>
        /// <returns>true if the motion was successfully added to play once queue</returns>
        public bool PlayOnceWhen(AnimaMotion motion, Func<bool> condition, Func<Vector2> parameters = null, float expiration = -1)
        {
            if (motion == null || condition == null || _pendingAnimQueue == null)
                return false;
            var animPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, condition, parameters);
            if (animPlay == null)
                return false;
            animPlay.Priority = 1;
            animPlay.AnimExpiration = expiration;
            _pendingAnimQueue.Add(animPlay);
            return true;
        }

        /// <summary>
        /// Play motion on the full body, with a high priority (2).
        /// </summary>
        /// <param name="motion">motion to play</param>
        /// <param name="parameters">blend parameters</param>
        /// <returns>true if the motion was successfully set as current motion</returns>
        public bool PlayOnce(AnimaMotion motion, Func<Vector2> parameters = null, bool bypassTransition = false, uint elevatedPriority = 0)
        {
            if (motion == null)
                return false;
            var motionPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, null, parameters);
            if (motionPlay == null)
                return false;
            motionPlay.Priority = 2 + (int)elevatedPriority;
            return Internal_Play(motionPlay, bypassTransition);
        }


        /// <summary>
        /// Play motion on the mask while condition is meet, with a low priority (0).
        /// </summary>
        /// <param name="motion">The motion to play</param>
        /// <param name="condition">the condition to meet</param>
        /// <param name="parameters">the blend parameters</param>
        /// <returns>true if the motion was successfully added to mask play loop queue</returns>
        public bool MaskPlayWhile(AnimaMotion motion, Func<bool> condition, AvatarMask mask, Func<Vector2> parameters = null)
        {
            if (!IsReady)
                return false;
            if (motion == null || condition == null || mask == null)
                return false;
            var animPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, condition, parameters, mask);
            if (animPlay == null)
                return false;
            animPlay.Priority = 0;
            var mask_hash = AnimaMaskLayer.CalculateMaskHash(mask);
            if (_maskLayers.IndexOfItem(m => m.MaskHash == mask_hash, out int index))
            {
                return _maskLayers[index].AddToLoopQueue(animPlay);
            }
            else
            {
                var maskLayer = AnimaMaskLayer.CreateMaskLayer(_playableGraph, _masterMixer, mask);
                if (maskLayer == null)
                    return false;
                maskLayer.AddToLoopQueue(animPlay);
                _maskLayers.Add(maskLayer);
            }
            return true;
        }

        /// <summary>
        /// Play motion on the mask when condition is meet, with a medium priority (1) and an expiration time.
        /// </summary>
        /// <param name="motion">motion to play</param>
        /// <param name="condition">condition to meet</param>
        /// <param name="parameters">blend parameters</param>
        /// <param name="expiration">extiration time in seconds</param>
        /// <returns>true if the motion was successfully added to mask play once queue</returns>
        public bool MaskPlayOnceWhen(AnimaMotion motion, Func<bool> condition, AvatarMask mask, Func<Vector2> parameters = null, float expiration = -1)
        {
            if (!IsReady)
                return false;
            if (motion == null || condition == null || mask == null)
                return false;
            var animPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, condition, parameters, mask);
            if (animPlay == null)
                return false;
            animPlay.Priority = 1;
            animPlay.AnimExpiration = expiration;
            var mask_hash = AnimaMaskLayer.CalculateMaskHash(mask);
            if (_maskLayers.IndexOfItem(m => m.MaskHash == mask_hash, out int index))
            {
                return _maskLayers[index].AddToPendingQueue(animPlay);
            }
            else
            {
                var maskLayer = AnimaMaskLayer.CreateMaskLayer(_playableGraph, _masterMixer, mask);
                if (maskLayer == null)
                    return false;
                maskLayer.AddToPendingQueue(animPlay);
                _maskLayers.Add(maskLayer);
            }
            return true;
        }

        /// <summary>
        /// Play motion on mask, with a high priority (2).
        /// </summary>
        /// <param name="motion">motion to play</param>
        /// <param name="parameters">blend parameters</param>
        /// <returns>true if the motion was successfully set as his mask layer current motion</returns>
        public bool MaskPlayOnce(AnimaMotion motion, AvatarMask mask, Func<Vector2> parameters = null)
        {
            if (!IsReady)
                return false;
            if (motion == null || mask == null)
                return false;
            var animPlay = AnimaMotionNode.CreateMotionPlay(motion, _playableGraph, null, parameters, mask);
            if (animPlay == null)
                return false;
            animPlay.Priority = 2;
            var mask_hash = AnimaMaskLayer.CalculateMaskHash(mask);
            if (_maskLayers.IndexOfItem(m => m.MaskHash == mask_hash, out int index))
            {
                return _maskLayers[index].ImmediatePlay(animPlay);
            }
            else
            {
                var maskLayer = AnimaMaskLayer.CreateMaskLayer(_playableGraph, _masterMixer, mask);
                if (maskLayer == null)
                    return false;
                maskLayer.ImmediatePlay(animPlay);
                _maskLayers.Add(maskLayer);
            }
            return true;
        }



        /// <summary>
        /// Get a layer from his mask.
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public AnimaMaskLayer GetMask(AvatarMask mask)
        {
            if (mask == null)
                return null;
            Hash128 avatarMaskHash = AnimaMaskLayer.CalculateMaskHash(mask);
            if (avatarMaskHash == default)
                return null;
            int index = _maskLayers.FindIndex(a => a.MaskHash == avatarMaskHash);
            if (index >= 0)
            {
                var maskLayer = _maskLayers[index];
                return maskLayer;
            }
            return null;
        }

        /// <summary>
        /// Is this motion currently playing full body?
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        public bool IsPlayingFullBody(AnimaMotion motion)
        {
            if (motion != null)
            {
                motion.GenereateHash();
                return motion.hash == CurrentMotionHash;
            }
            return false;
        }

        /// <summary>
        /// Is this motion currently playing part of the body?
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        public bool IsPlayingPartBody(AnimaMotion motion, AvatarMask mask)
        {
            if (motion != null && mask != null)
            {
                var maskLayer = GetMask(mask);
                if (maskLayer != null)
                {
                    motion.GenereateHash();
                    return motion.hash == maskLayer.CurrentMotionHash;
                }
            }
            return false;
        }

        #endregion

        #region Private Machine Functions #####################################################

        /// <summary>
        /// Initialize the state machine.
        /// </summary>
        private void InitMachine()
        {
            //Reset
            _currentPlayMotion = new AnimaMotionNode();
            _lastPlayMotion = new AnimaMotionNode();
            ResetCurrentValues();
            ResetLastValues();

            //animator
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_animator == null)
                return;
            _animator.hideFlags = HideFlags.HideInInspector;
            _animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

            //Graph
            _playableGraph = PlayableGraph.Create("Animancer Playable");
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            //Mixers
            _fullBodyLayerMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 1);
            _masterMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 2);

            //Output
            _outPut = AnimationPlayableOutput.Create(_playableGraph, "Animation OutPut", _animator);

            //Connection
            {
                //linking mixers
                _playableGraph.Connect(_fullBodyLayerMixer, 0, _masterMixer, 1);

                //Main mixers to output
                _outPut.SetSourcePlayable(_masterMixer);

                //Adding pose playable
                Tools.AddAnimationPosePlayable(_playableGraph, _fullBodyLayerMixer, 0);
                Tools.AddAnimationPosePlayable(_playableGraph, _masterMixer, 0);
            }

            //Weight setting
            {
                _masterMixer.SetInputWeight(0, 1);
                _masterMixer.SetInputWeight(1, 1);
                if (_fullBodyLayerMixer.GetInputCount() > 0)
                    _fullBodyLayerMixer.SetInputWeight(0, 1);
            }

            //Launch
            _playableGraph.Play();
        }

        /// <summary>
        /// Evaluate the machine state
        /// </summary>
        private void EvaluateMachine(float delta)
        {
            if (!IsReady)
                return;
            _playableGraph.Evaluate(_machineTimeScale * delta);
            _inTransition = TransitionToCurrentState(delta);
            EvaluateFinnishState(delta);
            if (_maskLayers != null)
            {
                for (int i = _maskLayers.Count - 1; i >= 0; i--)
                {
                    if (_maskLayers[i] == null || !_maskLayers[i].IsReady)
                    {
                        _maskLayers.RemoveAt(i);
                        continue;
                    }
                    _maskLayers[i].Evaluate(delta);
                }
            }
        }

        /// <summary>
        /// Dispose the state machine.
        /// </summary>
        private void DisposeMachine()
        {
            if (_maskLayers != null)
            {
                for (int i = _maskLayers.Count - 1; i >= 0; i--)
                {
                    if (_maskLayers[i] == null || !_maskLayers[i].IsReady)
                    {
                        _maskLayers.RemoveAt(i);
                        continue;
                    }
                    _maskLayers[i].DisposeLayer();
                }
            }
            if (_playableGraph.IsValid())
                _playableGraph.Destroy();
            ResetCurrentValues();
            ResetLastValues();
            _currentPlayMotion = null;
            _lastPlayMotion = null;
            _loopAnimQueue?.Clear();
            _pendingAnimQueue?.Clear();
            _maskLayers?.Clear();
        }

        #endregion

        #region Private Main Layer Functions  #############################################################

        /// <summary>
        /// Try to play an animation.
        /// </summary>
        /// <param name="motionPlay"></param>
        /// <returns></returns>
        private bool Internal_Play(AnimaMotionNode motionPlay, bool ignoreTransition = false)
        {
            if (motionPlay == null)
                return false;
            if (!IsReady)
                return false;
            //conditions comparision
            if (motionPlay.Condition != null)
            {
                if (!motionPlay.Condition.Invoke())
                    return false;
                if (_currentPlayMotion != null)
                {
                    if (_currentPlayMotion.Condition != null)
                    {
                        if (_currentPlayMotion.Condition.Invoke())
                        {
                            //Conditions are the same. do something?
                        }
                    }
                }
            }
            if (_currentPlayMotion != null)
            {
                //priority compare
                if (_currentPlayMotion.Motion)
                {
                    int p_Compare = _currentPlayMotion.Priority.CompareTo(motionPlay.Priority);
                    if (p_Compare >= 0)
                    {
                        float currTime = (float)_currentPlayMotion.MotionPlayable.GetTime();
                        float rqTransTime = motionPlay.Motion.TransitionTime > 0 ? motionPlay.Motion.TransitionTime : DEFAULT_TRANSITION_TIME;
                        if (currTime < (_currentPlayMotion.AnimDuration - rqTransTime) && !_currentPlayMotion.LoopMotion)
                        {
                            return false;
                        }
                        //hash comparision
                        if (_currentPlayMotion.Hash == motionPlay.Hash)
                            return false;
                    }
                }
            }
            //look for transition
            if (_inTransition && !ignoreTransition)
                return false;

            //set current to old
            float lastMotionWeight = 0;
            if (_lastPlayMotion.MotionPlayable.IsValid())
            {
                _lastPlayMotion.MotionPlayable.SetDone(true);
            }
            if (_lastPlayMotion.PlayablePortIndex.InInterval(0, _fullBodyLayerMixer.GetInputCount()))
            {
                lastMotionWeight = _fullBodyLayerMixer.GetInputWeight(_lastPlayMotion.PlayablePortIndex);
                _fullBodyLayerMixer.DisconnectInput(_lastPlayMotion.PlayablePortIndex);
            }
            _lastPlayMotion = _currentPlayMotion;

            int portIndex = -1;

            //get free input
            int inputCount = _fullBodyLayerMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                if (!_fullBodyLayerMixer.GetInput(i).IsValid())
                {
                    portIndex = i;
                    break;
                }
            }

            //Connection
            if (portIndex < 0)
            {
                portIndex = _fullBodyLayerMixer.AddInput(motionPlay.MotionPlayable, 0, _currentPlayMotion.Motion ? 0 : 1);
            }
            else
            {
                _fullBodyLayerMixer.ConnectInput(portIndex, motionPlay.MotionPlayable, 0);
            }

            _machineTimeScale = 1;
            _transitionSpeed = 1 / (motionPlay.Motion.TransitionTime > 0 ? motionPlay.Motion.TransitionTime : DEFAULT_TRANSITION_TIME);
            _inTransition = true;
            motionPlay.PlayablePortIndex = portIndex;
            _currentPlayMotion = motionPlay;

            return true;
        }

        /// <summary>
        /// Reset the values of the current animation object and invalidate it.
        /// </summary>
        private void ResetCurrentValues()
        {
            _currentPlayMotion?.Reset();
        }

        /// <summary>
        /// Reset the values of the last animation object and invalidate it.
        /// </summary>
        private void ResetLastValues()
        {
            _lastPlayMotion?.Reset();
        }

        /// <summary>
        /// Evaluate the last animation and dispose of it when it finnished.
        /// </summary>
        /// <param name="delta"></param>
        private void EvaluateFinnishState(float delta)
        {
            if (!IsReady)
                return;
            if (_inTransition)
                return;
            if (!_lastPlayMotion.MotionPlayable.IsValid())
                return;
            if (!_lastPlayMotion.PlayablePortIndex.InInterval(0, _fullBodyLayerMixer.GetInputCount()))
                return;
            if (_lastPlayMotion.MotionPlayable.GetTime() > _lastPlayMotion.AnimDuration)
            {
                _lastPlayMotion.MotionPlayable.SetDone(true);
                _fullBodyLayerMixer.DisconnectInput(_lastPlayMotion.PlayablePortIndex);
                ResetLastValues();
            }
        }

        /// <summary>
        /// Evalauate the current motion parameters
        /// </summary>
        /// <param name="delta"></param>
        private void EvaluateMotionParams()
        {
            if (_currentPlayMotion == null)
                return;
            if (_currentPlayMotion.ControlParameters != null)
                _blendValues = _currentPlayMotion.ControlParameters.Invoke();
            if (_currentPlayMotion.ControlParameters != null && _currentPlayMotion.MotionPlayable.IsValid())
            {
                int inputs = _currentPlayMotion.MotionPlayable.GetInputCount();
                float min = 0;
                float max = inputs - 1;
                if (max <= 1)
                {
                    _currentPlayMotion.MotionPlayable.SetInputWeight(0, 1);
                }
                else
                {
                    for (float i = 0; i < max; i++)
                    {
                        _currentPlayMotion.MotionPlayable.SetInputWeight((int)i, Tools.PeakCurve(_blendValues.x, i / (max - 1), min, (max - 1)));
                    }
                }
            }
        }

        /// <summary>
        /// Make the transition from last animation to current one.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private bool TransitionToCurrentState(float delta)
        {
            if (!IsReady)
                return false;
            if (!_currentPlayMotion.PlayablePortIndex.InInterval(0, _fullBodyLayerMixer.GetInputCount()))
                return false;
            bool transition = false;
            float debugWeight = 0;
            int lastStateIndex = _lastPlayMotion.PlayablePortIndex;
            if (lastStateIndex.InInterval(0, _fullBodyLayerMixer.GetInputCount()))
            {
                float lastStateWeight = _fullBodyLayerMixer.GetInputWeight(lastStateIndex);
                if (lastStateIndex >= _currentPlayMotion.PlayablePortIndex && lastStateWeight > 0)
                {
                    lastStateWeight -= delta * _transitionSpeed;
                    debugWeight += lastStateWeight;
                    transition = true;
                }
                lastStateWeight = Mathf.Clamp01(lastStateWeight);
                _fullBodyLayerMixer.SetInputWeight(lastStateIndex, lastStateWeight);
            }
            if (_fullBodyLayerMixer.GetInputWeight(_currentPlayMotion.PlayablePortIndex) < 1)
            {
                float currentStateWeight = _fullBodyLayerMixer.GetInputWeight(_currentPlayMotion.PlayablePortIndex);
                if (_currentPlayMotion.PlayablePortIndex < lastStateIndex)
                {
                    currentStateWeight = 1;
                }
                else
                {
                    currentStateWeight += delta * _transitionSpeed;
                    debugWeight += currentStateWeight;
                    transition = true;
                }
                currentStateWeight = Mathf.Clamp01(currentStateWeight);
                _fullBodyLayerMixer.SetInputWeight(_currentPlayMotion.PlayablePortIndex, currentStateWeight);
            }
            if (_debug)
            {
                PulseDebug.DrawCircle(transform.position, 0.5f + 1 * debugWeight, transform.up, transition ? Color.red : Color.white);
            }
            return transition;
        }

        /// <summary>
        /// Procedd the queue of animations waition for condition to be played in loop.
        /// </summary>
        private void ProcessAnimLoopQueue()
        {
            if (_loopAnimQueue == null || _loopAnimQueue.Count <= 0)
                return;
            for (int i = _loopAnimQueue.Count - 1; i >= 0; i--)
            {
                if (_loopAnimQueue[i].Condition == null)
                {
                    _loopAnimQueue.RemoveAt(i);
                    continue;
                }
                if (!_loopAnimQueue[i].Condition.Invoke())
                    continue;
                if (_currentPlayMotion != null && _currentPlayMotion == _loopAnimQueue[i])
                    continue;
                Internal_Play(_loopAnimQueue[i]);
                break;
            }
        }

        /// <summary>
        /// Procedd the queue of animations waition for condition to be played once.
        /// </summary>
        /// <param name="delta"></param>
        private void ProcessAnimOnceQueue(float delta)
        {
            if (_pendingAnimQueue == null || _pendingAnimQueue.Count <= 0)
                return;
            for (int i = _pendingAnimQueue.Count - 1; i >= 0; i--)
            {
                //debug
                if (_debug)
                    PulseDebug.DrawCircle(transform.position + transform.up, 0.5f + 0.1f * i, transform.up, Color.cyan);

                //if there is no condition, the queued animation is invalid
                if (_pendingAnimQueue[i].Condition == null)
                {
                    _pendingAnimQueue.RemoveAt(i);
                    continue;
                }
                //if the condition was not meet, skip
                if (!_pendingAnimQueue[i].Condition.Invoke())
                {
                    //if the animation has an expiration delay, decrease it untill the animation expire and will be remove from the queue
                    if (_pendingAnimQueue[i].AnimExpiration > 0)
                    {
                        _pendingAnimQueue[i].AnimExpiration -= delta;
                        if (_pendingAnimQueue[i].AnimExpiration <= 0)
                        {
                            _pendingAnimQueue.RemoveAt(i);
                        }
                    }
                    continue;
                }
                //prevent the queued to be played if the same anim is playing
                if (_currentPlayMotion != null && _currentPlayMotion == _pendingAnimQueue[i])
                    continue;
                //try to play the anim and remove it from the queue
                if (Internal_Play(_pendingAnimQueue[i]))
                {
                    _pendingAnimQueue.RemoveAt(i);
                    break;
                }
                else
                {
                    _pendingAnimQueue.RemoveAt(i);
                    continue;
                }
            }
        }

        /// <summary>
        /// Update the current motion events
        /// </summary>
        /// <param name="delta"></param>
        private void UpdateMotion(float delta)
        {
            if (_currentPlayMotion != null && _currentPlayMotion.Motion != null)
            {
                if (_currentPlayMotion.MotionPlayable.IsValid())
                {
                    if (_currentPlayMotion.Motion.Events != null)
                    {
                        for (int i = 0; i < _currentPlayMotion.Motion.Events.Count; i++)
                        {
                            _currentPlayMotion.Motion.Events[i].Evaluate(this, delta, (float)_currentPlayMotion.MotionPlayable.GetTime(), _currentPlayMotion.AnimDuration);
                        }
                    }
                }
            }
        }

        #endregion

        #region MonoBehaviours ########################################################

        private void Awake()
        {
            if (!_animator)
            {
                _animator = GetComponent<Animator>();
                if (_animator)
                {
                    _avatar = _animator.avatar;
                }
                else
                {
                    _animator = gameObject.AddComponent<Animator>();
                    _animator.avatar = _avatar;
                }
                _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (!_debug)
                    _animator.hideFlags = HideFlags.HideInInspector;
            }
        }

        private void OnEnable()
        {
            InitMachine();
            if (_animator != null)
                _animator.enabled = true;
        }

        private void FixedUpdate()
        {
            if (_updateMode == AnimatorUpdateMode.AnimatePhysics)
            {
                float delta = Time.fixedDeltaTime;
                EvaluateMachine(delta);
            }
        }

        private void Update()
        {
            if (!IsReady)
                return;
            float delta = Time.deltaTime;
            if (_animator)
            {
                _animator.updateMode = _updateMode;
            }

            if (_updateMode == AnimatorUpdateMode.Normal)
            {
                EvaluateMachine(delta);
            }
            //Normal
            ProcessAnimOnceQueue(delta);
            ProcessAnimLoopQueue();
            EvaluateMotionParams();
            UpdateMotion(delta);
            //Mask
            if (_maskLayers != null)
            {
                for (int i = _maskLayers.Count - 1; i >= 0; i--)
                {
                    if (_maskLayers[i] == null || !_maskLayers[i].IsReady)
                    {
                        _maskLayers.RemoveAt(i);
                        continue;
                    }
                    _maskLayers[i].Update(this, delta);
                }
            }
        }

        private void OnAnimatorMove()
        {
            Velocity = Vector3.zero;
            if (_animator == null)
                return;
            if (_currentPlayMotion == null)
                return;
            if (_currentPlayMotion.Motion == null)
                return;
            if (!_currentPlayMotion.Motion.UseRootMotion)
                return;
            Velocity = _animator.velocity * _machineTimeScale;
        }

        private void OnDisable()
        {
            DisposeMachine();
            if (_animator != null)
                _animator.enabled = false;
        }

        #endregion
    }

}