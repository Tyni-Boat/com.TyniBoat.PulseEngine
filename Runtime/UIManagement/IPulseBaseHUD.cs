using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine.UI
{
    /// <summary>
    /// The HUD interface
    /// </summary>
    public interface IPulseBaseHUD
    {
        Task Show();
        Task Hide();
    }
}