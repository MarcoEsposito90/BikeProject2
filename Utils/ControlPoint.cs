using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ControlPoint {

    public enum Type { Center, Tangent };


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public Vector2 position
    {
        get;
        private set;
    }

    public Type type
    {
        get;
        private set;
    }

    public int NumberOfPaths
    {
        get;
        private set;
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public ControlPoint(Vector2 position, Type type)
    {
        this.position = position;
        this.type = type;
        this.NumberOfPaths = 0;
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public void incrementPaths()
    {
        NumberOfPaths++;
    }
    
}
