using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace PulseEngine.CombatSystem
{


    [System.Serializable]
    public class WeaponDatas : ScriptableResource
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private Weapon _weapon;
        [SerializeField]
        private HumanBodyBones _restBone;
        [SerializeField]
        private TransformParams _restParams;
        [SerializeField]
        private HumanBodyBones _equipBone;
        [SerializeField]
        private TransformParams _equipParams;
        [SerializeField]
        private AnimatorOverrideController _animatorOverrideController;
        [SerializeField]
        private List<AttackParams> _attacks;

        [SerializeField]
        protected Vector2 _damage;

        [System.NonSerialized]
        private Dictionary<string, AttackParams> _tempDico = null;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public AnimatorOverrideController AnimatorOverrideController { get => _animatorOverrideController; }
        public HumanBodyBones RestBone { get => _restBone; }
        public TransformParams RestParams { get => _restParams; }
        public HumanBodyBones EquipBone { get => _equipBone; }
        public TransformParams EquipParams { get => _equipParams; }
        public Dictionary<string, AttackParams> Attacks
        {
            get
            {
                if (_tempDico == null)
                {
                    _tempDico = new Dictionary<string, AttackParams>();
                    for (int i = 0; i < _attacks.Count; i++)
                    {
                        _tempDico.Add(_attacks[i]?.OverrideAnimName, _attacks[i]);
                    }
                }
                if (_tempDico.Count != _attacks.Count)
                {
                    _attacks.Clear();
                    for (int i = 0; i < _tempDico.Count; i++)
                    {
                        _attacks.Add(_tempDico.ElementAt(i).Value);
                    }
                }
                return _tempDico;
            }
        }

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        #endregion
    }

}