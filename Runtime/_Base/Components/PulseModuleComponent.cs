using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Base class of any pulse module component.
    /// </summary>
    [System.Serializable]
    public abstract class PulseModuleComponent : MonoBehaviour
    {

        [SerializeField]
        private List<PulseModuleFeature> _features = new List<PulseModuleFeature>();


        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
        protected virtual void OnDestroyed() { }


        public void AddFeature<T>() where T : PulseModuleFeature, new()
        {
            var instance = new T();
            instance.SetParent(this);
            _features.Add(instance);
        }

        public void RemoveFeature<T>() where T : PulseModuleFeature, new()
        {
            int index = _features.FindIndex(f => f.GetType() == typeof(T));
            if (index >= 0)
            {
                _features.RemoveAt(index);
            }
        }

        public bool TryGetFeature<T>(out T result) where T : PulseModuleFeature { result = default; return default; }

        public T GetFeature<T>() where T : PulseModuleFeature { return default; }

        public T GetFeatureAt<T>(int index) where T : PulseModuleFeature { return default; }

        public void ReplaceFeature<T, Q>() where T : PulseModuleFeature where Q : PulseModuleFeature, new() { }

        public void SwapFeatures<T, Q>() where T : PulseModuleFeature where Q : PulseModuleFeature { }


        private void Start()
        {
            OnStart();
            _features?.ForEach(x => x?.OnInit());
        }

        private void OnEnable()
        {
            OnEnabled();
            _features?.ForEach(x => x?.OnActivate());
        }

        private void Update()
        {
            _features?.ForEach(x => x?.OnUpdate());
            OnUpdate();
        }

        private void FixedUpdate()
        {
            _features?.ForEach(x => x?.OnFixedUpdate());
            OnFixedUpdate();
        }

        private void OnDisable()
        {
            _features?.ForEach(x => x?.OnDesactivate());
            OnDisabled();
        }
    }
}
