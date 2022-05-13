using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace PulseEngine
{

    /// <summary>
    /// Base class for any state machine like serializable asset.
    /// </summary>
    public abstract class BaseTreeBehaviourData : ScriptableResource
    {
        public BaseBehaviourNode RootNode;

        public List<BaseBehaviourNode> AllNodes = new List<BaseBehaviourNode>();

        public List<BaseBehaviourNode> CurrentNodes = new List<BaseBehaviourNode>();

        /// <summary>
        /// Evaluate the tree
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Evaluate(MonoBehaviour stateMachine, float deltaTime)
        {
            if (RootNode == null && AllNodes.Count > 0)
            {
                RootNode = AllNodes[0];
                ResetMachine();
            }
            if (CurrentNodes != null)
            {
                for (int i = 0; i < CurrentNodes.Count; i++)
                {
                    if (CurrentNodes[i] == null)
                        continue;
                    if (CurrentNodes[i].ExecuteNode(stateMachine, this, deltaTime))
                        break;
                }
            }
        }

        public void SetCurrentNodes(params BaseBehaviourNode[] nodes)
        {
            CurrentNodes?.ForEach(node => node?.ResetNode());
            CurrentNodes?.Clear();
            CurrentNodes?.AddRange(nodes);
        }

        public void ResetMachine()
        {
            CurrentNodes?.ForEach(node => node?.ResetNode());
            CurrentNodes?.Clear();
            CurrentNodes?.Add(RootNode);
        }


#if UNITY_EDITOR

        public BaseBehaviourNode CreateNode(Type t)
        {
            var node = ScriptableObject.CreateInstance(t) as BaseBehaviourNode;
            node.name = t.Name;
            node.GUID = GUID.Generate().ToString();
            AllNodes.Add(node);
            AssetDatabase.AddObjectToAsset(node, this);
            AssetDatabase.SaveAssets();
            return node;
        }

        public void DeleteNode(BaseBehaviourNode node)
        {
            if (!AllNodes.Contains(node))
                return;
            AllNodes.Remove(node);
            AssetDatabase.RemoveObjectFromAsset(node);
            AssetDatabase.SaveAssets();
        }

        public void AddChild(BaseBehaviourNode parent, BaseBehaviourNode child)
        {
            parent?.AddChild(child);
        }

        public void RemoveChild(BaseBehaviourNode parent, BaseBehaviourNode child)
        {
            parent?.RemoveChild(child);
        }


        public List<BaseBehaviourNode> GetChildrens(BaseBehaviourNode node)
        {
            return node?.Children;
        }

#endif
    }

}