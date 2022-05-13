using PulseEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HealthHudTest : MonoBehaviour, IPulseBaseHUD
{
    public Image fillImage;

    public async Task Hide()
    {
        await MainThread.Execute(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public async Task Show()
    {
        await MainThread.Execute(() =>
        {
            gameObject.SetActive(true);
        });
    }

    public void UpdateHealth(float newValue)
    {
        if (fillImage == null)
            return;
        newValue = Mathf.Clamp(newValue / 100, 0, 1);
        fillImage.fillAmount = newValue;
    }
}
