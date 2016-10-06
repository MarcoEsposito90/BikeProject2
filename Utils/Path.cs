using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Path {

    private List<BezierCurve> segments;
    private ControlPoint start;
    private ControlPoint end;

    public Path()
    {
        segments = new List<BezierCurve>();
    }

    public void addSegment(BezierCurve segment)
    {
        segments.Add(segment);
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- STATIC METHODS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region STATIC_METHODS


    public static List<BezierCurve> calculatePaths(Dictionary<Vector2,Quadrant> quadrants, Vector2 chunkPosition, int roadsDensity)
    {
        List<BezierCurve> localCurves = new List<BezierCurve>();
        
        

        
        return localCurves;
    }


    /* ------------------------------------------------------------------------------------------------- */
    private static bool isMissingLinks(List<ControlPoint> points)
    {
        
        foreach(ControlPoint p in points)
        {
            if (p.NumberOfPaths == 0)
                return true;
        }

        return false;
    }

    #endregion
}

