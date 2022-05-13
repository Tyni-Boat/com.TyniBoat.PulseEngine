using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Debug Gizmo
    /// </summary>
    public class GizmoDebug : MonoBehaviour
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        private List<KeyValuePair<Action, float>> _debugGizmos = new List<KeyValuePair<Action, float>>();
        private List<KeyValuePair<Action, float>> _waitingList = new List<KeyValuePair<Action, float>>();

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Draw a gizmo function for a certain duration.
        /// </summary>
        /// <param name="_gizmoAction"></param>
        /// <param name="duration"></param>
        public void DrawDebugGizmo(Action _gizmoAction, float duration)
        {
            _waitingList.Add(new KeyValuePair<Action, float>(_gizmoAction, duration));
        }

        public static void DrawGizmoWireCube(Vector3 position, Quaternion rotation, Vector3 size)
        {
            Mesh debugCube = new Mesh();
            debugCube.vertices = new Vector3[8]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),//0
                new Vector3(0.5f, -0.5f, -0.5f),//1
                new Vector3(0.5f, 0.5f, -0.5f),//2
                new Vector3(-0.5f, 0.5f, -0.5f),//3
                new Vector3(-0.5f, 0.5f, 0.5f),//4
                new Vector3(0.5f, 0.5f, 0.5f),//5
                new Vector3(0.5f, -0.5f, 0.5f),//6
                new Vector3(-0.5f, -0.5f, 0.5f),//7
            };
            debugCube.triangles = new int[36]
            {
                0, 2, 1, //face front
			    0, 3, 2,
                2, 3, 4, //face top
			    2, 4, 5,
                1, 2, 5, //face right
			    1, 5, 6,
                0, 7, 4, //face left
			    0, 4, 3,
                5, 4, 7, //face back
			    5, 7, 6,
                0, 6, 7, //face bottom
			    0, 1, 6
            };
            debugCube.Optimize();
            debugCube.RecalculateNormals();
            Gizmos.DrawWireMesh(debugCube, position, rotation, size);
        }

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void OnDrawGizmos()
        {
            if (_debugGizmos.Count > 0)
            {
                for (int i = _debugGizmos.Count - 1; i >= 0; i--)
                {
                    _debugGizmos[i].Key?.Invoke();
                    float countDown = _debugGizmos[i].Value;
                    countDown -= Time.deltaTime;
                    if (countDown <= 0)
                    {
                        _debugGizmos.RemoveAt(i);
                        continue;
                    }
                    _debugGizmos[i] = new KeyValuePair<Action, float>(_debugGizmos[i].Key, countDown);
                }
            }
            if (_waitingList.Count > 0)
            {
                _debugGizmos.AddRange(_waitingList);
                _waitingList.Clear();
            }
        }

        #endregion
    }

}