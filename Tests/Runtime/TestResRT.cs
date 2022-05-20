using PulseEngine.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class TestResRT : MonoBehaviour
{
    #region Constants #############################################################

    #endregion

    #region Variables #############################################################

    string _currentOperationMessage;
    DummyResource _currentResource;

    #endregion

    #region Statics   #############################################################

    #endregion

    #region Inner Types ###########################################################

    #endregion

    #region Properties ############################################################

    #endregion

    #region Public Functions ######################################################

    #endregion

    #region Private Functions #####################################################

    #endregion

    #region Jobs      #############################################################

    #endregion

    #region MonoBehaviours ########################################################

    private async void Start()
    {
        _currentOperationMessage = "Init...";
        await Task.Delay(10000);
        _currentOperationMessage = "Init Done";
    }

    public void OnGUI()
    {
        GUILayout.Label($"Current Operation : {_currentOperationMessage}");
        if (_currentResource != null)
        {
            if (GUILayout.Button("Runtime Save Sync"))
            {
                _currentOperationMessage = _currentResource.SaveResource(Application.persistentDataPath)
                    ? $"Saved Dummy Resource to {Application.persistentDataPath}" : "Failed to save dummy Resource";
            }
            if (GUILayout.Button("Runtime Save Async"))
            {
                _currentOperationMessage = "Saving";
                Task.Run(async () =>
                {
                    _currentOperationMessage = await _currentResource.SaveResourceAsync(Application.persistentDataPath)
                       ? $"Saved Dummy Resource to {Application.persistentDataPath}" : "Failed to save dummy Resource";
                });

            }
        }
        if (GUILayout.Button("Runtime Load Sync"))
        {
            _currentResource = ResourcesManager.LoadResource<DummyResource>(0);
            _currentOperationMessage = _currentResource
                ? $"Loaded Dummy Resource ID {0}" : "Failed to Load dummy Resource";
        }
        if (GUILayout.Button("Runtime Load Async"))
        {
            _currentOperationMessage = "Loading";
            Task.Run(async () =>
            {
                _currentResource = await ResourcesManager.LoadResourceAsync<DummyResource>(0);
                _currentOperationMessage = _currentResource
                    ? $"Loaded Dummy Resource ID {0}" : "Failed to Load dummy Resource";
            });

        }
    }

    #endregion
}

