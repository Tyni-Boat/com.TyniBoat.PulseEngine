using PulseEngine;
using PulseEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class WindowTest_3 : MonoBehaviour, IPulseBaseWindow
{
    public Button close;

    public async Task Close(bool destroy = false, params object[] options)
    {
        if (destroy)
            await MainThread.Execute(() => Destroy(gameObject));
        else
            await MainThread.Execute(() => gameObject.SetActive(false));
    }

    public async Task Open(params object[] options)
    {
        await MainThread.Execute(() => gameObject.SetActive(true));
    }
    private void Start()
    {
        if (close) close.onClick.AddListener(() => UIManager.CloseWindow<WindowTest_3>());
    }
}
