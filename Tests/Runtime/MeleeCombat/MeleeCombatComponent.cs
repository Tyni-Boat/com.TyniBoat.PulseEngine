using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.MeleeCombat
{
    /// <summary>
    /// Component in charge of melee combat.
    /// </summary>
    public class MeleeCombatComponent : PulseModuleComponent
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        public List<MeleeWeapon> weaponList;

        private Collider _currenteWeaponCollider;

        [SerializeField] private GameObject _currenteWeaponShapeInstance;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public MeleeWeapon CurrentWeapon { get; internal set; }

        public bool CurrenteWeaponEquiped { get; internal set; }

        #endregion

        #region Public Functions ######################################################

        public void EnableWeapon(int id, Animator animator = null)
        {
            CurrentWeapon = weaponList?.Where(w => w.Id == id).FirstOrDefault();
            _currenteWeaponShapeInstance = Instantiate(CurrentWeapon?._weaponPrefab);
            _currenteWeaponCollider = _currenteWeaponShapeInstance?.GetComponent<Collider>();
            if(_currenteWeaponCollider)
                _currenteWeaponCollider.isTrigger = true;
            UnEquipWeapon(animator);
        }

        public void DisableWeapon()
        {
            CurrentWeapon = null;
            if (_currenteWeaponShapeInstance)
                Destroy(_currenteWeaponShapeInstance);
            _currenteWeaponShapeInstance = null;
            _currenteWeaponCollider = null;
        }

        public void EquipWeapon(Animator animator)
        {
            if (CurrentWeapon == null)
                return;
            Transform parent = animator ? animator.GetBoneTransform(CurrentWeapon._equipParent) : transform;
            if (parent == null)
                return;
            if (_currenteWeaponShapeInstance == null)
                return;
            _currenteWeaponShapeInstance.transform.SetParent(parent);
            CurrentWeapon._equipPlace.SetOnTransform(_currenteWeaponShapeInstance.transform);
            CurrenteWeaponEquiped = true;
        }

        public void UnEquipWeapon(Animator animator)
        {
            if (CurrentWeapon == null)
                return;
            Transform parent = animator ? animator.GetBoneTransform(CurrentWeapon._unEquipParent) : transform;
            if (parent == null)
                return;
            if (_currenteWeaponShapeInstance == null)
                return;
            _currenteWeaponShapeInstance.transform.SetParent(parent);
            CurrentWeapon._unEquipPlace.SetOnTransform(_currenteWeaponShapeInstance.transform);
            CurrenteWeaponEquiped = false;
        }

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}