using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Animations;
using System.Reflection;


namespace PulseEngine.CombatSystem
{

    [CustomEditor(typeof(BaseState), true)]
    [CanEditMultipleObjects]
    public class BaseStateInspector : Editor
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        private List<SerializedProperty> GetTypeVars<T>(T target, SerializedObject serializedObject, bool determineType = false) where T : class
        {
            T obj = target as T;

            if (obj == null || serializedObject == null)
                return null;

            List<SerializedProperty> result = new List<SerializedProperty>();
            FieldInfo[] fields;
            if (determineType)
                fields = obj.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
            else
                fields = typeof(T).GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields.Length > 0)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (!field.IsPublic)
                    {
                        var attributes = new List<CustomAttributeData>(field.CustomAttributes);
                        bool notAValidType = true;
                        for (int j = 0; j < attributes.Count; j++)
                        {
                            var a = attributes[j];
                            if (a.AttributeType == typeof(SerializeField))
                            {
                                notAValidType = false;
                            }
                            if (a.AttributeType == typeof(HideInInspector))
                            {
                                notAValidType = true;
                            }
                        }
                        if (notAValidType)
                            continue;
                    }
                    var s = serializedObject.FindProperty(field.Name);
                    if (s != null)
                        result.Add(s);
                }
            }
            return result;
        }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################


        public override void OnInspectorGUI()
        {
            AnimatorState state = Selection.activeObject as AnimatorState;
            BaseState baseState = target as BaseState;
            if (state != null && baseState)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField("State Name", state.name);
                typeof(BaseState).GetField("_stateName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, state.name);
                BlendTree tree = state.motion as BlendTree;
                if (tree != null)
                {
                    typeof(BaseState).GetField("_isBlendTree", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, true);
                    string[] animsNames = new string[tree.children.Length];
                    for (int i = 0; i < tree.children.Length; i++)
                    {
                        EditorGUILayout.LabelField($"State Blend Tree Anim {i + 1}", tree.children[i].motion?.name);
                        animsNames[i] = tree.children[i].motion?.name;
                    }
                    typeof(BaseState).GetField("_stateBlendTreeAnimNames", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, animsNames);
                }
                else
                {
                    typeof(BaseState).GetField("_isBlendTree", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, false);
                    EditorGUILayout.LabelField("State Anim", state.motion?.name);
                    typeof(BaseState).GetField("_stateAnimName", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, state.motion?.name);
                }
                EditorGUILayout.EndVertical();
            }
            if (target.GetType().BaseType == typeof(BaseState))
            {
                EditorGUILayout.BeginVertical("GroupBox");
                DrawProperties(GetTypeVars(baseState, serializedObject));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginVertical("GroupBox");
            DrawProperties(GetTypeVars(target, serializedObject, true));
            EditorGUILayout.EndVertical();
        }

        private void DrawProperties(List<SerializedProperty> baseTypeVars)
        {
            if (baseTypeVars == null)
                return;
            for (int i = 0; i < baseTypeVars.Count; i++)
            {
                if (baseTypeVars[i] != null)
                {
                    EditorGUILayout.PropertyField(baseTypeVars[i]);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

}