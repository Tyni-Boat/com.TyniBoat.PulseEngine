using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Used to expose a node to been shown in a bahaviour graph of type T
    /// </summary>
    public class ExposedBehaviourNodeAttribute : Attribute
    {
        public ExposedBehaviourNodeAttribute(Type t)
        {

        }
    }

}