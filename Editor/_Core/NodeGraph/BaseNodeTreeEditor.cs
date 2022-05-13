using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PulseEngine
{

    public abstract class BaseNodeTreeEditor<T> : EditorWindow
    {
        protected NodeTreeView treeView;
        protected NodeTreeItemInspector inspectorView;

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;


            string[] resAssetsGUIDs = AssetDatabase.FindAssets("BaseNodeTreeEditor");
            string[] resAssetsPaths = new string[resAssetsGUIDs.Length];
            for (int i = 0; i < resAssetsGUIDs.Length; i++)
                resAssetsPaths[i] = AssetDatabase.GUIDToAssetPath(resAssetsGUIDs[i]);
            int assetPathIndex = -1;

            // Import UXML
            assetPathIndex = resAssetsPaths.FindIndex(s => s.Contains(".uxml"));
            if (assetPathIndex >= 0)
            {
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resAssetsPaths[assetPathIndex]);
                visualTree.CloneTree(root);
            }

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            assetPathIndex = resAssetsPaths.FindIndex(s => s.Contains(".uss"));
            if (assetPathIndex >= 0)
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(resAssetsPaths[assetPathIndex]);
                root.styleSheets.Add(styleSheet);
            }

            treeView = root.Q<NodeTreeView>();
            if (treeView != null)
            {
                treeView.ControlledType = typeof(T);
                treeView.OnSelectNode = OnNodeSelectionChanged;
            }
            inspectorView = root.Q<NodeTreeItemInspector>();

            OnGUICreated();
        }

        private void OnNodeSelectionChanged(NodeView nodeView)
        {
            inspectorView?.UpdateView(nodeView);
        }

        public abstract void OnGUICreated();

        public void PopulateInspector(BaseBehaviourNode node) { }
    }
}