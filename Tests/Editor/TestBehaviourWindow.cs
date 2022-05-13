using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace PulseEngine
{

    public class TestBehaviourWindow : BaseNodeTreeEditor<BaseBehaviourNode>
    {
        public DebugBehaviourTree tree;

        [MenuItem(PulseConstants.Menu_EDITOR_MENU + "/TestNodeEditor")]
        public static void Open()
        {
            var wnd = GetWindow<TestBehaviourWindow>();
            wnd.titleContent = new GUIContent("TestBehaviourWindow");
        }

        public override void OnGUICreated()
        {
            if (tree != null)
                treeView?.PopulateTree(tree);
        }

        private void OnEnable()
        {
        }
    }
}