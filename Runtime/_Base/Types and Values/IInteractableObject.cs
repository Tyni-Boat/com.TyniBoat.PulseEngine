using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Logic of interactables objects.
    /// </summary>
    public interface IInteractableObject
    {
        void OnInteractability(GameObject other, bool interactability);
        void Interract(GameObject other);
        bool CanInteract(GameObject other);
        TransformParams InteractableTransformParams { get; }
    }
}