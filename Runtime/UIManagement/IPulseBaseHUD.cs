using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine
{

    public interface IPulseBaseHUD
    {
        Task Show();
        Task Hide();
    }
}