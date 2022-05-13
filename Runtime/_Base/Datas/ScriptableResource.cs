using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine
{


    /// <summary>
    /// The base type of all serialized data type
    /// </summary>
    [System.Serializable]
    public abstract class ScriptableResource : ScriptableObject
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        [SerializeField]
        private int _id;
        [SerializeField]
        private string _name;
        [SerializeField]
        private string _description;

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        public int Id { get => _id; }
        public string Name { get => _name; }
        public string Description { get => _description; }

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