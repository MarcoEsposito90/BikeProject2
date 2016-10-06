using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControlPoint{

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

    public Vector2 localQuadrantPosition
    {
        get;
        private set;
    }

    public Vector2 quadrantPosition
    {
        get;
        private set;
    }

    public int NumberOfPaths
    {
        get;
        private set;
    }

    public Quadrant parent
    {
        get;
        private set;
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public ControlPoint(Vector2 position, Type type, Vector2 localQuadrantPosition, Vector2 quadrantPosition, Quadrant parent)
    {
        this.position = position;
        this.type = type;
        this.localQuadrantPosition = localQuadrantPosition;
        this.quadrantPosition = quadrantPosition;
        this.parent = parent;
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public void incrementPaths()
    {
        NumberOfPaths++;
    }
}
