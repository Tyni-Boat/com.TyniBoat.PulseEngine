using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Base behaviour tree node
    /// </summary>
    public abstract class BaseBehaviourNode : ScriptableObject
    {
        public enum BehaviourNodeState
        {
            Waiting,
            Running,
            Success,
            Failure,
            Done
        }


        [SerializeField] [HideInInspector] protected BehaviourNodeState _state;

        [SerializeField] [HideInInspector] protected List<BaseBehaviourNode> _children = new List<BaseBehaviourNode>();

        [SerializeField] [HideInInspector] protected BaseBehaviourNode _parent;

        [SerializeField] [HideInInspector] protected Vector2 _position;

        [SerializeField] [HideInInspector] protected string _guid;

        public BehaviourNodeState NodeState { get => _state; }
        public List<BaseBehaviourNode> Children { get => _children; }
        public BaseBehaviourNode Parent { get => _parent; protected set => _parent = value; }
        public Vector2 NodePosition { get => _position; set => _position = value; }
        protected MonoBehaviour StateMachine { get; private set; }
        public string GUID { get => _guid; set => _guid = value; }

        public bool ExecuteNode(MonoBehaviour stateMachine, BaseTreeBehaviourData tree, float deltaTime)
        {
            StateMachine = stateMachine;
            bool treeHasChanged = false;
            try
            {
                switch (_state)
                {
                    case BehaviourNodeState.Waiting:
                        OnStateEnter();
                        _state = BehaviourNodeState.Running;
                        break;
                    case BehaviourNodeState.Running:
                        OnStateUpdate(deltaTime);
                        _state = ExecutionDone() ? BehaviourNodeState.Success : _state;
                        break;
                    case BehaviourNodeState.Success:
                        _state = BehaviourNodeState.Done;
                        treeHasChanged = OnStateExit(BehaviourNodeState.Success, tree);
                        break;
                    case BehaviourNodeState.Failure:
                        _state = BehaviourNodeState.Done;
                        OnStateExit(BehaviourNodeState.Failure, tree);
                        break;
                    case BehaviourNodeState.Done:
                        break;
                }
            }
            catch (Exception e)
            {
                _state = BehaviourNodeState.Failure;
                throw e;
            }

            return treeHasChanged;
        }

        public void ResetNode()
        {
            _state = BehaviourNodeState.Waiting;
        }

        public void AddChild(BaseBehaviourNode child)
        {
            if (child == null)
                return;
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(BaseBehaviourNode child)
        {
            if (child == null)
                return;
            if (!Children.Contains(child))
                return;
            child.Parent = null;
            Children.Remove(child);
        }

        public abstract void OnStateEnter();

        public abstract bool OnStateExit(BehaviourNodeState state, BaseTreeBehaviourData tree);

        public abstract void OnStateUpdate(float deltaTime);

        public abstract bool ExecutionDone();
    }
}