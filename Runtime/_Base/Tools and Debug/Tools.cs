using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PulseEngine
{

    public static class Tools
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

        #region Runtime Tools ######################################################

        /// <summary>
        /// Return lower or upper value depending on the comparison between value and thresholdValue.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="lowerValue"></param>
        /// <param name="upperValue"></param>
        /// <param name="thresholdValue"></param>
        /// <returns></returns>
        public static float ThresholdSwithcher(float value, float lowerValue, float upperValue, float thresholdValue)
        {
            return value < thresholdValue ? lowerValue : upperValue;
        }

        /// <summary>
        /// Add an animation pose playable to the target at input index
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="target"></param>
        /// <param name="inputIndex"></param>
        public static void AddAnimationPosePlayable(PlayableGraph graph, Playable target, int inputIndex)
        {

            //Animation Pose
            Type t = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var q = assemblies[i].GetType("UnityEngine.Animations.AnimationPosePlayable");
                if (q != null)
                {
                    t = q;
                    break;
                }
            }

            //Animation Pose
            if (t != null)
            {
                var method = t.GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    var pose = method.Invoke(null, new object[] { graph });
                    PlayableHandle handle = (PlayableHandle)pose.GetType().GetMethod("GetHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.Invoke(pose, new object[] { });
                    var constructor = typeof(Playable).GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(PlayableHandle) }, null);
                    Playable posePlayable = (Playable)constructor?.Invoke(new object[] { handle });
                    graph.Connect(posePlayable, 0, target, inputIndex);
                }
            }
        }

        /// <summary>
        /// Compare 2 float and return true if their values are similar with precision as the number of digits after coma.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool Approximatetly(float a, float b, int precision = 1)
        {
            float multiplier = Mathf.Pow(10, precision);
            int aI = Mathf.FloorToInt(a * multiplier);
            int bI = Mathf.FloorToInt(b * multiplier);
            return aI == bI;
        }

        /// <summary>
        /// Return a number between 0 and 1, corresponding to ax+b or -ax+b depending of whether or not x > peak
        /// </summary>
        /// <param name="x"></param>
        /// <param name="peak"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float PeakCurve(float x, float peak, float min, float max)
        {
            //logical protections
            if (min >= max) return 0;
            //choose curve
            if (x < peak)
                return Mathf.InverseLerp(min, peak, x);
            else
                return 1 - Mathf.InverseLerp(peak, max, x);
        }


        #endregion

        #region Editor Tools #####################################################

#if UNITY_EDITOR


        /// <summary>
        /// Create a path
        /// </summary>
        /// <param name="path"></param>
        public static void CreatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            var pathParts = path.Split('/');
            if (pathParts.Length <= 0)
                return;
            if (pathParts[0].ToUpper() != "ASSETS")
                return;
            string combinedPath = pathParts[0];
            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextCombinedPath = combinedPath + "/" + pathParts[i];
                if (!AssetDatabase.IsValidFolder(nextCombinedPath))
                {
                    AssetDatabase.CreateFolder(combinedPath, pathParts[i]);
                    AssetDatabase.SaveAssets();
                }
                combinedPath = nextCombinedPath;
            }
        }



        /// <summary>
        /// Get serializables properties of a type.
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        /// <param name="target"></param>
        /// <param name="serializedObject"></param>
        /// <param name="determineType"></param>
        /// <returns></returns>
        public static List<SerializedProperty> GetTypeSerializedProperties<Q>(Q target, SerializedObject serializedObject, bool determineType = false) where Q : class
        {
            Q obj = target as Q;

            if (obj == null || serializedObject == null)
                return null;

            List<SerializedProperty> result = new List<SerializedProperty>();
            FieldInfo[] fields;
            if (determineType)
                fields = obj.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
            else
                fields = typeof(Q).GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
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

#endif

        #endregion

        #region Jobs      #############################################################

        #endregion
    }


}