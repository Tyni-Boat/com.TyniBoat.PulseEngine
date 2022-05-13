using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine
{

    public interface IPulseBaseWindow
    {
        Task Open(params object[] options);
        Task Close(bool destroy = false, params object[] options);
    }
}