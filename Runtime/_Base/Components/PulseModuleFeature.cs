using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{
    /// <summary>
    /// Base class of pulse modules feature components
    /// </summary>
    public abstract class PulseModuleFeature
    {
        #region Variables ##############################################

        private bool _enabled;
        private bool _isInitialized;
        protected MonoBehaviour _parent;

        #endregion

        #region Properties ##############################################

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (!_isInitialized)
                    OnInit();
                if (value)
                    OnActivate();
                else
                    OnDesactivate();
                _enabled = value;
            }
        }

        #endregion

        #region Publics Methods ###########################################
        //
        internal void SetParent(MonoBehaviour mono)
        {
            _parent = mono;
        }

        //portal functions
        public virtual void OnInit() { }
        public virtual void OnActivate() { }
        public virtual void OnDesactivate() { }

        //Loop functions
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnLateUpdate() { }

        //physic events
        public virtual void OnCollisionStart(Collider collider, Collision collision = null) { }
        public virtual void OnCollisionPersist(Collider collider, Collision collision = null) { }
        public virtual void OnCollisionEnds(Collider collider, Collision collision = null) { }

        //Rendering event
        public virtual void OnVisibilityChanged(bool isVisible) { }

        //Debugging
        public virtual void OnDrawGizmo(bool selected) { }


        #endregion

        #region Private Methods ###########################################

        #endregion
    }
}
