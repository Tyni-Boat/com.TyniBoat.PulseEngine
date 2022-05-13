using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PulseEngine
{

    /// <summary>
    /// Sixaxis directions
    /// </summary>
    public enum Direction
    {
        none,
        forward,
        back,
        left,
        right,
        up,
        down,
    }

    /// <summary>
    /// The physic Spaces
    /// </summary>
    public enum PhysicSpace
    {
        unSpecified,
        inAir,
        onGround,
        inFluid,
        onWall
    }

    /// <summary>
    /// Materials
    /// </summary>
    public enum Materials
    {
        none,
        flesh,
        bone,
        iron,
        steel,
        wood,
        paper,
    }


}