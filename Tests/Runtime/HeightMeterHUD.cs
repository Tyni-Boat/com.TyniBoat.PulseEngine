using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PulseEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using PulseEngine.UI;

public class HeightMeterHUD : MonoBehaviour, IPulseBaseHUD
{
    public Image filler;
    public float maxHeight = 5;


    public async Task Hide()
    {
        await MainThread.Execute(() => gameObject.SetActive(false));
    }

    public async Task Show()
    {
        await MainThread.Execute(() => gameObject.SetActive(true));
    }

    public void UpdateHeight(float value)
    {
        if (!filler)
            return;
        float val = Mathf.InverseLerp(0.0f, maxHeight, value);
        filler.fillAmount = val;
    }
}
