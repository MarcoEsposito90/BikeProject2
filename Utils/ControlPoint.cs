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

    public float distance(ControlPoint other)
    {
        return Vector2.Distance(this.position, other.position);
    }



    public class NearestPointComparer : IComparer<ControlPoint>
    {
        public ControlPoint center;

        public NearestPointComparer(ControlPoint center)
        {
            this.center = center;
        }

        public int Compare(ControlPoint x, ControlPoint y)
        {
            float distanceX = Vector2.Distance(center.position, x.position);
            float distanceY = Vector2.Distance(center.position, y.position);

            if (distanceX < distanceY) return -1;
            else if (distanceX == distanceY) return 0;
            return 1;
        }
    }

}
