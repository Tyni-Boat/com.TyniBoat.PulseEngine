using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseEngine
{

    public class NodeTreeView : GraphView
    {

        public new class UxmlFactory : UxmlFactory<NodeTreeView, GraphView.UxmlTraits> { }

        private BaseTreeBehaviourData _tree;
        public Type ControlledType { get; internal set; }
        public Action<NodeView> OnSelectNode { get; set; }

        public NodeTreeView()
        {
            //background
            Insert(0, new GridBackground());

            //Manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/PulseEngine/_Core/Editor/NodeGraph/BaseNodeTreeEditor.uss");
            styleSheets.Add(styleSheet);
        }

        public NodeView GetNodeView(BaseBehaviourNode node)
        {
            return GetNodeByGuid(node.GUID) as NodeView;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            //base.BuildContextualMenu(evt);
            var types = TypeCache.GetTypesDerivedFrom(ControlledType);
            foreach (var type in types)
            {
                evt.menu.AppendAction($"Create {type.Name} node", a => CreateNode(type, evt.localMousePosition));
            }
        }

        public void PopulateTree(BaseTreeBehaviourData tree)
        {
            _tree = tree;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;
            //Create nodes
            _tree.AllNodes?.ForEach(n => CreateNodeView(n));
            //Create Edges
            _tree.AllNodes?.ForEach(n =>
            {
                _tree.GetChildrens(n)?.ForEach(c =>
                {
                    var parent = GetNodeView(n);
                    var child = GetNodeView(c);

                    Edge edge = parent.OutPut.ConnectTo(child.Input);
                    AddElement(edge);
                });
            });
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                //Its a node?
                graphViewChange.elementsToRemove.ForEach(item =>
                {
                    NodeView node = item as NodeView;
                    if (node != null && _tree != null)
                    {
                        _tree.DeleteNode(node.Node);
                    }
                    Edge edge = item as Edge;
                    if (edge != null)
                    {
                        NodeView parent = edge.output.node as NodeView;
                        NodeView child = edge.input.node as NodeView;
                        if (parent != null && child != null && _tree != null)
                        {
                            _tree.RemoveChild(parent.Node, child.Node);
                        }
                    }
                });
            }
            if (graphViewChange.edgesToCreate != null)
            {
                //Its an edge?
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    NodeView parent = edge.output.node as NodeView;
                    NodeView child = edge.input.node as NodeView;
                    if (parent != null && child != null && _tree != null)
                    {
                        _tree.AddChild(parent.Node, child.Node);
                    }
                });
            }
            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
            {
                return endPort.direction != startPort.direction && endPort.node != startPort.node;
            }).ToList();
        }

        public void CreateNode(Type t, Vector2 position)
        {
            if (t == null || _tree == null)
                return;
            var node = _tree.CreateNode(t);
            node.NodePosition = position;
            CreateNodeView(node);
        }

        public void CreateNodeView(BaseBehaviourNode node)
        {
            NodeView nodev = new NodeView(node);
            nodev.OnSelect = OnSelectNode;
            AddElement(nodev);
        }
    }
}