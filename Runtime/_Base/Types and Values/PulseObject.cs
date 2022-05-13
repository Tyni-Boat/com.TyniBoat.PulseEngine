using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine
{
    public abstract class PulseObject : MonoBehaviour
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private List<PulseMiniBehaviour> _miniBehaviours = new List<PulseMiniBehaviour> ();

        private List<PulseMiniBehaviour> _childrenBehaviours = new List<PulseMiniBehaviour> ();

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        protected virtual ScriptableResource Data { get; }

        #endregion

        #region Public Functions ######################################################

        public bool TryGetMiniBehaviour<T>(out T result) where T : PulseMiniBehaviour
        {
            int index = _childrenBehaviours.FindIndex(beh => { return beh.GetType() == typeof(T); });
            if (index >= 0)
            {
                result = _childrenBehaviours[index] as T;
                return true;
            }
            result = null;
            return false;
        }

        public T GetMiniBehaviour<T>() where T : PulseMiniBehaviour
        {
            int index = _childrenBehaviours.FindIndex(beh => { return beh.GetType() == typeof(T); });
            if (index >= 0)
                return _childrenBehaviours[index] as T;
            return null;
        }

        public void AddMiniBehaviour<T>(T miniBehaviour) where T : PulseMiniBehaviour
        {
            if (miniBehaviour == null)
                return;
            int index = _childrenBehaviours.FindIndex(beh => { return beh.GetType() == typeof(T); });
            if (index>= 0)
                return;
            miniBehaviour.SetParent(this);
            miniBehaviour.Enabled = true;
            _childrenBehaviours.Add(miniBehaviour);
        }

        public void RemoveMiniBehaviour<T>() where T : PulseMiniBehaviour, new()
        {
            int index = _childrenBehaviours.FindIndex(beh => { return beh.GetType() == typeof(T); });
            if (index < 0)
                return;
            _childrenBehaviours.RemoveAt(index);
        }

        #endregion

        #region Private Functions #####################################################

        private void CloneBehaviours()
        {
            _childrenBehaviours.Clear();
            for(int i = 0; i < _miniBehaviours.Count; i++)
            {
                AddMiniBehaviour(ScriptableObject.Instantiate(_miniBehaviours[i]));
            }
        }

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################


        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void Update()
        {
            if (_miniBehaviours.Count > _childrenBehaviours.Count)
                CloneBehaviours();

            for (int i = 0; i < _childrenBehaviours.Count; i++)
            {
                if (_childrenBehaviours[i] != null && _childrenBehaviours[i].Enabled)
                    _childrenBehaviours[i].OnUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            for (int i = 0; i < _childrenBehaviours.Count; i++)
            {
                if (_childrenBehaviours[i] != null && _childrenBehaviours[i].Enabled)
                    _childrenBehaviours[i].OnFixedUpdate();
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            for (int i = 0; i < _childrenBehaviours.Count; i++)
            {
                if (_childrenBehaviours[i] != null && _childrenBehaviours[i].Enabled)
                    _childrenBehaviours[i].OnDrawGizmo(false);
            }
        }

        protected virtual void OnDrawGizmos()
        {
            for (int i = 0; i < _childrenBehaviours.Count; i++)
            {
                if (_childrenBehaviours[i] != null && _childrenBehaviours[i].Enabled)
                    _childrenBehaviours[i].OnDrawGizmo(true);
            }
        }

        #endregion
    }

}