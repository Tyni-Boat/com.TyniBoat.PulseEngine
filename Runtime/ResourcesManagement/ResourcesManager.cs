using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace PulseEngine.Resources
{

    /// <summary>
    /// Manage the save and lod of assets at editor and runtime.
    /// </summary>
    public static class ResourcesManager
    {
        #region Constants #############################################################

        public const string CONFIGS_PATH = "Assets/PulseEngineConfigs";

        #endregion

        //Editor Only Functions
        #region Editor #############################################################

#if UNITY_EDITOR

        #region Public Functions ######################################################

        /// <summary>
        /// Load or create the pulse editor config file.
        /// </summary>
        /// <returns></returns>
        public static PulseConfigs LoadConfigs()
        {
            if (!AssetDatabase.IsValidFolder(CONFIGS_PATH))
            {
                Tools.CreatePath(CONFIGS_PATH);
            }
            string fileName = "PulseConfigs";
            PulseConfigs configFile = AssetDatabase.LoadAssetAtPath<PulseConfigs>($"{CONFIGS_PATH}/{fileName}.asset");
            if (configFile == null)
            {
                configFile = ScriptableObject.CreateInstance<PulseConfigs>();
                AssetDatabase.CreateAsset(configFile, $"{CONFIGS_PATH}/{fileName}.asset");
                AssetDatabase.SaveAssets();
            }
            return configFile;
        }

        /// <summary>
        /// Load a resource synchronously from it's ID in the Editor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T LoadAsset<T>(int id) where T : ScriptableResource
        {
            var configFile = LoadConfigs();
            if (configFile == null)
                return default;
            if (string.IsNullOrEmpty(configFile.ResourcesBasePath))
                return default;
            if (!AssetDatabase.IsValidFolder($"{configFile.ResourcesBasePath}/{typeof(T).Name}"))
            {
                Debug.LogWarning($"Invalid folder : {configFile.ResourcesBasePath}/{typeof(T).Name}");
                return default;
            }
            string assetPath = $"{configFile.ResourcesBasePath}/{typeof(T).Name}/{typeof(T).Name}_{id}.asset";
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        /// <summary>
        /// Save a resource synchronously to the Asset Database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path where the file will be stored</param>
        /// <returns></returns>
        public static bool SaveAsset<T>(this T obj) where T : ScriptableResource
        {
            var configFile = LoadConfigs();
            if (configFile == null)
                return false;
            if (string.IsNullOrEmpty(configFile.ResourcesBasePath))
                return false;
            if (!AssetDatabase.IsValidFolder($"{configFile.ResourcesBasePath}/{typeof(T).Name}"))
            {
                Tools.CreatePath($"{configFile.ResourcesBasePath}/{typeof(T).Name}");
            }

            //register the good Id
            string[] allPaths = AssetDatabase.GetAllAssetPaths().Where(path => path.Contains($"{configFile.ResourcesBasePath}/{typeof(T).Name}"))?.ToArray();
            if (allPaths != null)
            {
                for (int i = 0; i < allPaths.Length; i++)
                {
                    var assetFileAtPath = AssetDatabase.LoadAssetAtPath<T>(allPaths[i]);

                    if (assetFileAtPath == null)
                        continue;
                    if (assetFileAtPath.Id >= obj.Id)
                    {
                        obj.WriteField("_id", assetFileAtPath.Id + 1);
                    }
                }
            }

            //save
            string assetPath = $"{configFile.ResourcesBasePath}/{typeof(T).Name}/{typeof(T).Name}_{obj.Id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.SaveAssets();
                return true;
            }
            asset = obj;
            AssetDatabase.SaveAssets();
            return true;
        }

        #endregion

#endif

        #endregion

        //Runtime Functions
        #region Runtime   #############################################################

        #region Public Functions ######################################################

        /// <summary>
        /// Load a resource synchronously from it's ID, from the default path or from custom path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T LoadResource<T>(int id, string customPath = "") where T : ScriptableResource
        {
            T res = default;
            string path = string.IsNullOrEmpty(customPath) ? $"{typeof(T).Name}/{typeof(T).Name}_{id}" : customPath;
            res = UnityEngine.Resources.Load<T>(path);
            return res ? ScriptableResource.Instantiate(res) : default;
        }

        /// <summary>
        /// Load Resource Asynchronously from it's ID, from the default path or from custom path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<T> LoadResourceAsync<T>(int id, string customPath = "") where T : ScriptableResource
        {
            bool done = false;
            T res = default;
            string path = string.IsNullOrEmpty(customPath) ? $"{typeof(T).Name}/{typeof(T).Name}_{id}" : customPath;
            await MainThread.Execute(() =>
            {
                var handler = UnityEngine.Resources.LoadAsync<T>(path);
                handler.completed += (o) =>
                {
                    done = true;
                    if (handler.asset != null)
                        res = ScriptableResource.Instantiate(handler.asset as T);
                };
            });
            await MainThread.WaitUntil(() => done);
            return res;
        }

        /// <summary>
        /// Save a resource synchronously to Disk
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path where the file will be stored</param>
        /// <returns></returns>
        public static bool SaveResource<T>(this T obj, string path, string fileName = "") where T : ScriptableResource { return default; }

        /// <summary>
        /// Save Resource Asynchronously to disk
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<bool> SaveResourceAsync<T>(this T obj, string path, string fileName = "") where T : ScriptableResource { return default; }

        #endregion

        #endregion
    }

}