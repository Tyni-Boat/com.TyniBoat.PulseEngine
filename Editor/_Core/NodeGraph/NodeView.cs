using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace PulseEngine
{
    public class NodeView : Node
    {
        private Port _input;
        private Port _outPut;
        private BaseBehaviourNode _node;

        public Action<NodeView> OnSelect { get; set; }
        public BaseBehaviourNode Node { get => _node; }
        public Port Input { get => _input; }
        public Port OutPut { get => _outPut; }

        public NodeView(BaseBehaviourNode node)
        {
            _node = node;
            title = node.name;

            viewDataKey = node.GUID;
            style.left = node.NodePosition.x;
            style.top = node.NodePosition.y;

            CreateInputPorts();
            CreateOutputPorts();
        }

        private void CreateInputPorts()
        {
            _input = InstantiatePort(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Single, typeof(bool));
            _input.portName = "In";
            inputContainer.Add(_input);
        }

        private void CreateOutputPorts()
        {
            _outPut = InstantiatePort(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(bool));
            _outPut.portName = "Out";
            outputContainer.Add(_outPut);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.NodePosition = new Vector2(newPos.xMin, newPos.yMin);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnSelect?.Invoke(this);
        }

    }
}