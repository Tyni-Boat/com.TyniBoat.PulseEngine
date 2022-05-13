using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine
{
    /// <summary>
    /// Manage user interfaces.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Constants #############################################################

        public const string CLASSIC_UI_PATH = "classicUI";
        public const string TOOLKIT_UI_PATH = "XmlUI";

        #endregion

        #region Variables #############################################################

        private static UIManager _instance;

        [SerializeField] private Transform _worlToUILayer;
        [SerializeField] private Transform _windowsLayer;
        [SerializeField] private Transform _hudsLayer;
        [SerializeField] private Transform _overlayEffectsLayer;
        [SerializeField] private Transform _loadingWheel;

        private List<object> _worldUIMarkers = new List<object>();

        private List<IPulseBaseWindow> _windowsList = new List<IPulseBaseWindow>();

        private List<IPulseBaseHUD> _loadedHUDs = new List<IPulseBaseHUD>();

        private List<object> _uiEffects = new List<object>();

        #endregion

        #region Statics   ##########################################################################################################################

        #region World To UI  **********************************************************

        public static T PlaceUIMarker<T>(string name, Vector3 worldCoordinates)
        {
            if (_instance == null || _instance._worlToUILayer == null)
                return default;
            throw new NotImplementedException();
        }

        public static bool UpdateUIMarker<T>(string name, Vector3 worldCoordinates)
        {
            if (_instance == null || _instance._worlToUILayer == null)
                return default; throw new NotImplementedException();
        }

        public static bool DeleteUIMarker<T>(string name, Vector3 worldCoordinates)
        {
            if (_instance == null || _instance._worlToUILayer == null)
                return default; throw new NotImplementedException();
        }

        #endregion

        #region Windows  **********************************************************

        public static async Task<T> OpenWindow<T>(params object[] openningParams) where T : class, IPulseBaseWindow
        {
            if (_instance == null || _instance._windowsLayer == null)
                return default;
            var alreadyOpenned = GetWindow<T>();
            if (alreadyOpenned != null)
                return alreadyOpenned;
            //create window from storage
            var win_goPrefab = ResourcesManager.LoadResource<UIAsset>(0, $"{CLASSIC_UI_PATH}/Windows/{typeof(T).Name}_WIN")?.UIObject;
            if (win_goPrefab == null)
                return default;
            var win_go = Instantiate(win_goPrefab, _instance._windowsLayer);
            var win = win_go.GetComponent<T>();
            if (win == null)
                return default;
            //set other windows as inactives
            if (_instance._windowsList != null)
            {
                for (int i = 0; i < _instance._windowsList.Count; i++)
                {
                    await _instance._windowsList[i].Close();
                }
            }
            else
            {
                _instance._windowsList = new List<IPulseBaseWindow>();
            }
            //add to list
            _instance._windowsList.Add(win);
            //Activate layer
            _instance._windowsLayer.gameObject.SetActive(true);
            //Show loading
            if (_instance._loadingWheel)
            {
                _instance._loadingWheel.gameObject.SetActive(true);
            }
            //set this window as active
            await win.Open(openningParams);
            //Mask loading
            if (_instance._loadingWheel)
            {
                _instance._loadingWheel.gameObject.SetActive(false);
            }
            return win;
        }
        public static T GetWindow<T>() where T : class, IPulseBaseWindow
        {
            if (_instance == null || _instance._windowsLayer == null)
                return default;
            var alreadyOpenned = OpennedWindow as T;
            if (alreadyOpenned != null)
                return alreadyOpenned;
            if (_instance._windowsList == null)
                return default;
            int windowIndex = _instance._windowsList.FindIndex(o => o.GetType() == typeof(T));
            if (windowIndex >= 0)
                alreadyOpenned = _instance._windowsList[windowIndex] as T;
            if (alreadyOpenned != null)
                return alreadyOpenned;
            return default;
        }
        public static async Task<bool> CloseWindow<T>(params object[] closingParams) where T : class, IPulseBaseWindow
        {
            if (_instance == null || _instance._windowsLayer == null)
                return default;
            var alreadyOpenned = GetWindow<T>();
            if (alreadyOpenned != null)
            {
                //wait window animation if it is current window
                await alreadyOpenned.Close(OpennedWindow == alreadyOpenned, closingParams);
                //remove from list
                if (_instance._windowsList.Contains(alreadyOpenned))
                    _instance._windowsList.Remove(alreadyOpenned);
                //
                if(OpennedWindow != null)
                {
                    await OpennedWindow.Open();
                }
                else
                {
                    _instance._windowsLayer.gameObject.SetActive(false);
                }
            }
            return default;
        }

        #endregion

        #region HUds  **********************************************************

        public static async Task<T> ShowHUD<T>(params object[] openningParams) where T : class, IPulseBaseHUD
        {
            if (_instance == null || _instance._hudsLayer == null)
                return default;
            var alreadyOpenned = GetHUD<T>();
            if (alreadyOpenned != null)
            {
                await alreadyOpenned.Show();
                return alreadyOpenned;
            }
            //create hud from storage
            var hud_goPrefab = ResourcesManager.LoadResource<UIAsset>(0, $"{CLASSIC_UI_PATH}/Huds/{typeof(T).Name}_HUD")?.UIObject;
            if (hud_goPrefab == null)
                return default;
            var hud_go = Instantiate(hud_goPrefab, _instance._hudsLayer);
            var hud = hud_go.GetComponent<T>();
            if (hud == null)
                return default;
            //add to list
            _instance._loadedHUDs.Add(hud);
            //set this hud as active
            hud_go.SetActive(true);
            //wait window animation
            await hud.Show();
            return hud;
        }
        public static T GetHUD<T>() where T : class ,IPulseBaseHUD
        {
            if (_instance == null || _instance._hudsLayer == null)
                return default;
            if (_instance._windowsList == null)
                return default;
            object alreadyOpenned = null;
            int hudIndex = _instance._loadedHUDs.FindIndex(o => o.GetType() == typeof(T));
            if (hudIndex >= 0)
                alreadyOpenned = _instance._loadedHUDs[hudIndex] as T;
            if (alreadyOpenned != null)
                return alreadyOpenned as T;
            return default;
        }
        public static async Task<bool> HideHUD<T>(bool unloadHUD = false, params object[] closingParams) where T : class, IPulseBaseHUD
        {
            if (_instance == null || _instance._hudsLayer == null)
                return default;
            var alreadyOpenned = GetHUD<T>();
            if (alreadyOpenned != null)
            {
                //remove from list
                if (unloadHUD)
                {
                    if (_instance._loadedHUDs.Contains(alreadyOpenned))
                        _instance._loadedHUDs.Remove(alreadyOpenned);
                }
                //wait hud animation if it is current window
                await alreadyOpenned.Hide();
            }
            return default;
        }

        #endregion

        #region UI Effects  **********************************************************

        public static bool PlayUIEffect<T>(string name, Func<bool> stopCondition, Action<Transform> OnUpdate = null, Action<Transform> OnEnd = null)
        {
            if (_instance == null || _instance._overlayEffectsLayer == null)
                return default; throw new NotImplementedException();
        }
        public static bool StopUIEffect<T>(string name, bool invokeEndAction = false)
        {
            if (_instance == null || _instance._overlayEffectsLayer == null)
                return default; throw new NotImplementedException();
        }

        #endregion

        #endregion

        #region Inner Types ######################################################################################################################

        #endregion

        #region Properties ########################################################################################################################

        public static IPulseBaseWindow OpennedWindow
        {
            get
            {
                if (_instance == null || _instance._windowsLayer == null)
                    return null;
                if (_instance._windowsList == null || _instance._windowsList.Count <= 0)
                    return null;
                return _instance._windowsList[_instance._windowsList.Count - 1];
            }
        }
        public static int WindowStackCount
        {
            get
            {
                if (_instance == null || _instance._windowsLayer == null)
                    return 0;
                if (_instance._windowsList == null)
                    return 0;
                return _instance._windowsList.Count;
            }
        }


        #endregion

        #region Public Functions ############################################################################################################

        #endregion

        #region Private Functions ##########################################################################################################

        #endregion

        #region Jobs      ##########################################################################################################################

        #endregion

        #region MonoBehaviours ################################################################################################################


        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            if (_instance != this)
            {
                if(_instance == null)
                {
                    _instance = this;
                    _instance._windowsLayer?.gameObject.SetActive(false);
                }
                else
                {
                    GameObject.Destroy(gameObject);
                    return;
                }
            }
            _windowsLayer.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_loadingWheel && _loadingWheel.gameObject.activeSelf)
            {
                _loadingWheel.Rotate(Vector3.forward * Time.deltaTime * 45);
            }
        }

        #endregion
    }
}