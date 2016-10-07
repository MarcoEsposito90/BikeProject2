using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Path
{

    #region ATTRIBUTES

    public List<ControlPoint> points
    {
        get; private set;
    }
    public List<BezierCurve> curves
    {
        get; private set;
    }

    #endregion

    public Path()
    {
        points = new List<ControlPoint>();
        curves = new List<BezierCurve>();
    }

    public void addControlPoint(ControlPoint point)
    {
        points.Add(point);
    }

    public void createCurves()
    {
        for (int i = 0; i < points.Count - 1; i++)
            curves.Add(new BezierCurve(points[i], points[i], points[i + 1], points[i + 1]));
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- STATIC METHODS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region STATIC_METHODS


    public static List<Path> calculatePaths(MapChunk chunk)
    {
        List<Path> localPaths = new List<Path>();

        Path path = new Path();
        bool pathCompleted = false;

        Quadrant next = chunk.quadrants[new Vector2(0, 0)];
        path.addControlPoint(next.roadsControlPoint);

        while (!pathCompleted)
        {
            next = findNearestNeighbor(next, chunk, path);

            if (next == null)
            {
                pathCompleted = true;
                break;
            }

            path.addControlPoint(next.roadsControlPoint);
            next.roadsControlPoint.incrementPaths();
        }

        path.createCurves();
        localPaths.Add(path);

        return localPaths;
    }

    /* ------------------------------------------------------------------------------------------------- */
    private static Quadrant findNearestNeighbor(Quadrant q, MapChunk chunk, Path path)
    {
        Quadrant neighbor = null;

        int localX = (int)q.localPosition.x;
        int localY = (int)q.localPosition.y;
        ControlPoint point = q.roadsControlPoint;
        float currentMinimumDistance = 1.5f * q.width;

        for (int i = 0; i < chunk.subdivisions; i++)
            for (int j = 0; j < chunk.subdivisions; j++)
            {
                if (i == localX && j == localY)
                    continue;

                Quadrant candidateQuadrant = chunk.quadrants[new Vector2(i, j)];
                ControlPoint candidatePoint = candidateQuadrant.roadsControlPoint;

                if (path.points.Contains(candidatePoint))
                    continue;

                float distance = Vector2.Distance(candidatePoint.position, point.position);

                if (distance < currentMinimumDistance)
                    neighbor = candidateQuadrant;
            }

        return neighbor;
    }


    /* ------------------------------------------------------------------------------------------------- */
    private static bool isMissingLinks(List<ControlPoint> points)
    {

        foreach (ControlPoint p in points)
        {
            if (p.NumberOfPaths == 0)
                return true;
        }

        return false;
    }

    #endregion
}

