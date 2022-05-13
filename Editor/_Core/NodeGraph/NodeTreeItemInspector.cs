using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace PulseEngine
{
    public class NodeTreeItemInspector : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NodeTreeItemInspector, VisualElement.UxmlTraits> { }

        Editor _editor;

        public NodeTreeItemInspector()
        {

        }

        public void UpdateView(NodeView nodeView)
        {
            Clear();

            UnityEngine.Object.DestroyImmediate(_editor);
            _editor = Editor.CreateEditor(nodeView.Node);
            var container = new IMGUIContainer(() => { _editor.OnInspectorGUI(); });
            Add(container);
        }
    }
}