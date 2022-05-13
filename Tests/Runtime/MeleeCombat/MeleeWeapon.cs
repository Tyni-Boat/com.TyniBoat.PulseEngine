using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.MeleeCombat
{
    /// <summary>
    /// Represent a melee combat weapon
    /// </summary>
    [CreateAssetMenu(menuName = "New Melee Weapon", fileName = "MeleeWeapon")]
    public class MeleeWeapon : ScriptableResource
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField] internal GameObject _weaponPrefab;
        [SerializeField] private ScriptableResource _weaponStats;


        [SerializeField] private ScriptableResource _equipMotion;
        [SerializeField] internal HumanBodyBones _equipParent;
        [SerializeField] internal TransformParams _equipPlace;

        [SerializeField] private ScriptableResource _unEquipMotion;
        [SerializeField] internal HumanBodyBones _unEquipParent;
        [SerializeField] internal TransformParams _unEquipPlace;


        [SerializeField] private List<ScriptableResource> _moveSet;

        [SerializeField] private List<ScriptableResource> _attackSet;

        [SerializeField] private List<ScriptableResource> _defenseSet;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public ScriptableResource EquipMotion { get => _equipMotion; }

        public ScriptableResource UnEquipMotion { get => _unEquipMotion; }

        #endregion

        #region Public Functions ######################################################

        public ScriptableResource GetMove(string name) => _moveSet?.Where(m => m.Name == name).FirstOrDefault();
        public ScriptableResource GetAttack(string name) => _attackSet?.Where(m => m.Name == name).FirstOrDefault();
        public ScriptableResource GetDefense(string name) => _defenseSet?.Where(m => m.Name == name).FirstOrDefault();

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}