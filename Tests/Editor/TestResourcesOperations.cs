using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PulseEngine;
using PulseEngine.Resources;

public class TestResourcesOperations: EditorWindow
{
    #region Constants #############################################################

    #endregion

    #region Variables #############################################################

    string _currentOperationMessage;

    #endregion

    #region Statics   #############################################################

    #endregion

    #region Inner Types ###########################################################

    #endregion

    #region Properties ############################################################

    #endregion

    #region Public Functions ######################################################

    [MenuItem("Test/Resources")]
    public static void Open()
    {
        var window = EditorWindow.GetWindow<TestResourcesOperations>();
        window.Show();
    }

    #endregion

    #region Private Functions #####################################################

    #endregion

    #region Jobs      #############################################################

    #endregion

    #region MonoBehaviours ########################################################

    public void OnGUI()
    {
        EditorGUILayout.LabelField($"Current Operation : {_currentOperationMessage}");
        if (GUILayout.Button("LoadConfigFile"))
        {
            _currentOperationMessage = ResourcesManager.LoadConfigs()?.ResourcesBasePath;
        }
        if (GUILayout.Button("Editor Save"))
        {
            _currentOperationMessage = ScriptableResource.CreateInstance<DummyResource>().SaveAsset()? "Saved Dummy asset" : "Failed to save dummy asset";
        }
        if (GUILayout.Button("Editor Load"))
        {
            _currentOperationMessage = ResourcesManager.LoadAsset<DummyResource>(0)?"Dummy asset id 0 loaded" : "Failed to load dummy aset";
        }
        if (GUILayout.Button("Editor Delete"))
        {
            _currentOperationMessage = AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(ResourcesManager.LoadAsset<DummyResource>(0)))?"Dummy asset id 0 Deleted" : "Failed to delete dummy aset";
        }
    }

    #endregion
}

