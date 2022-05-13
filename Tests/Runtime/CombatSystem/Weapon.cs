using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine.CombatSystem
{

    public class Weapon : PulseObject, IInteractableObject
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private int _dataID = -1;
        [SerializeField]
        private WeaponDatas _data;

        private Collider _detectionCollider;
        private Collider _collisionCollider;
        private Rigidbody _rigidbody;
        private Character _owner;
        private Renderer _renderer;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public TransformParams InteractableTransformParams => TransformParams.FromTransform(transform);

        protected override ScriptableResource Data => _data;

        public Collider CollisionCollider { get => _collisionCollider; }

        public Vector3 WeaponSize { get; private set; }

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// To Equip this weapon on the character
        /// </summary>
        /// <param name="character"></param>
        public void EquipWeapon(Character character)
        {
            if (character == null)
                return;
            if (_data == null)
                return;
            if (CollisionCollider)
                CollisionCollider.enabled = false;
            Transform parent = character.GetBone(_data.EquipBone);
            transform.SetParent(parent);
            _data.EquipParams.SetOnTransform(transform);
            if (character.OverrideAnimations<State_Attack>(null) != null)
            {
                var states = character.OverrideAnimations<State_Attack>(_data.AnimatorOverrideController, true);
                if (_data.Attacks != null)
                {
                    for (int i = 0; i < states.Length; i++)
                    {
                        var stateAnimNames = states[i].StateAnimName.Split('|');
                        for (int j = 0; j < stateAnimNames.Length; j++)
                        {
                            if (_data.Attacks.TryGetValue(stateAnimNames[j], out var atkParam))
                            {
                                states[i].InjectParams(atkParam);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void Interract(GameObject other)
        {
            if (other == null)
                return;
            if (other.TryGetComponent<Character>(out var character))
            {
                _owner = character;
                EquipWeapon(character);
                character.CanInteractWith(this, false);
                character.SetCurrentWeapon(this);
            }
            else
            {
                PulseDebug.Log($"{name} named {_data.Name}, interracted with {other.name}");
            }
        }

        public void OnInteractability(GameObject other, bool interactability)
        {
            throw new System.NotImplementedException();
        }

        public bool CanInteract(GameObject other)
        {
            if (other == null)
                return false;
            if (!_detectionCollider)
                return false;
            if (!_detectionCollider.enabled)
                return false;
            var otherColliders = other.GetComponentsInChildren<Collider>();
            if (otherColliders.Length <= 0)
                return false;
            bool oneOrMoreTouches = false;
            for (int i = 0; i < otherColliders.Length; i++)
            {
                if (otherColliders[i].bounds.Intersects(_detectionCollider.bounds))
                {
                    oneOrMoreTouches = true;
                    break;
                }
            }
            return oneOrMoreTouches;
        }

        /// <summary>
        /// To Unequip this weapon on the character
        /// </summary>
        /// <param name="character"></param>
        public void UnequipWeapon(Character character) { }

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            var colliders = GetComponents<Collider>();
            if (colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (_detectionCollider && CollisionCollider)
                        break;
                    if (colliders[i].isTrigger)
                        _detectionCollider = colliders[i];
                    else
                        _collisionCollider = colliders[i];
                }
            }
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                WeaponSize = _renderer.bounds.size;
            }
        }

        void Update()
        {
            if (!_owner)
            {
                if (_detectionCollider)
                    PulseDebug.DrawCircle(transform.position, _detectionCollider.bounds.extents.magnitude, Vector3.up, Color.grey);
            }
            //if (_renderer != null)
            //{
            //    PulseDebug.DrawCube(_renderer.bounds.center, WeaponSize, transform.rotation, Color.black);
            //}
        }

        void FixedUpdate()
        {
            if (_rigidbody)
            {
                if (_detectionCollider)
                    _detectionCollider.enabled = _rigidbody.IsSleeping() && !_owner;

                _rigidbody.isKinematic = _owner;
                //if (_collisionCollider)
                //    _collisionCollider.enabled = _rigidbody.IsSleeping();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Character>(out var character))
            {
                character.CanInteractWith(this, true);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<Character>(out var character))
            {
                character.CanInteractWith(this, false);
            }
        }

        #endregion
    }

}