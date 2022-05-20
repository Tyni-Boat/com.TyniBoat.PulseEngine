using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine.UI
{
    /// <summary>
    /// The windows interface
    /// </summary>
    public interface IPulseBaseWindow
    {
        Task Open(params object[] options);
        Task Close(bool destroy = false, params object[] options);
    }
}