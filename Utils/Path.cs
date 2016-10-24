using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Path
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

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

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONSTRUCTORS

    public Path()
    {
        points = new List<ControlPoint>();
        curves = new List<BezierCurve>();
    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINTS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONTROL_POINTS

    public void addControlPoint(ControlPoint point)
    {
        points.Add(point);
        point.incrementPaths();
    }

    /* ------------------------------------------------------------------------------------------------- */
    public void insertControlPoint(ControlPoint point, int index)
    {
        if (index == points.Count)
            points.Add(point);
        else
            points.Insert(index, point);

        point.incrementPaths();
    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CURVES

    public void createCurves(float sinuosity)
    {
        float radius = sinuosity;

        for (int i = 1; i < points.Count; i++)
        {
            ControlPoint startTangent = null;
            ControlPoint endTangent = null;

            // 1) Calculate startTangent -----------------------------------------------------
            int indexOffset = (i - 1) == 0 ? 1 : 2;
            
            float xDist = (points[i].position.x - points[i - indexOffset].position.x);
            float yDist = (points[i].position.y - points[i - indexOffset].position.y);
            float angle = Mathf.Atan(yDist / xDist);

            float x = points[i - 1].position.x + (radius * Mathf.Cos(angle));
            float y = points[i - 1].position.y + (radius * Mathf.Sin(angle));
            Vector2 pos = new Vector2(x, y);
            startTangent = new ControlPoint(pos, ControlPoint.Type.Tangent);

            // 2) Calculate endTangent -------------------------------------------------------
            indexOffset = i == (points.Count - 1) ? 0 : 1;

            xDist = (points[i + indexOffset].position.x - points[i - 1].position.x);
            yDist = (points[i + indexOffset].position.y - points[i - 1].position.y);
            angle = Mathf.Atan(yDist / xDist);

            x = points[i - 1].position.x + radius * Mathf.Cos(angle);
            y = points[i - 1].position.y + radius * Mathf.Sin(angle);
            pos = new Vector2(x, y);
            endTangent = new ControlPoint(pos, ControlPoint.Type.Tangent);

            BezierCurve curve = new BezierCurve(points[i - 1], startTangent, points[i], endTangent);
            curves.Add(curve);
        }
    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- STATIC METHODS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region STATIC_METHODS

    #region PATH_COMPUTING

    public static List<Path> calculatePaths(MapSector chunk, float segmentMaximumLength, float sinuosity)
    {
        List<Path> localPaths = new List<Path>();
        Quadrant next = null;

        // 1) create paths linking control points inside the chunk ----------------------
        while ((next = isMissingLinks(chunk)) != null)
        {
            // A) until all control points are not linked, create a new path ------------
            Path path = new Path();
            bool pathCompleted = false;

            while (!pathCompleted)
            {
                // B) keep on adding points to the path until you find -------------------
                path.addControlPoint(next.roadsControlPoint);
                next = findNearestNeighbor(next, chunk, path, segmentMaximumLength);

                if (next == null)
                    pathCompleted = true;
            }

            localPaths.Add(path);
        }

        // 2) once all points inside the chunk are linked, let's link the to the ones outside ----
        linkWithExternal(chunk, localPaths, segmentMaximumLength);

        // 3) once all paths are defined, compute the curves linking each point couple ------------
        foreach (Path p in localPaths)
            p.createCurves(sinuosity);

        return localPaths;
    }


    /* ------------------------------------------------------------------------------------------------- */
    private static void linkWithExternal(MapSector chunk, List<Path> paths, float segmentMaximumLength)
    {
        bool debug = chunk.position.Equals(Vector2.zero);

        // for each quadrant inside the chunk ... ----------------------------------------------
        for (int i = 0; i < chunk.subdivisions; i++)
        {
            for (int j = 0; j < chunk.subdivisions; j++)
            {
                // if it's not a boundary quadrant, skip ---------------------------------------
                bool proceed = (j == 0 || j == chunk.subdivisions - 1 || i == 0 || i == chunk.subdivisions - 1);
                if (!proceed)
                    continue;

                Quadrant quadrant = chunk.quadrants[new Vector2(i, j)];
                List<ControlPoint> toBeLinked = new List<ControlPoint>();

                // otherwise, search the closest outern point on its neighbor quadrants --------
                for (int k = i - 1; k <= i + 1; k++)
                    for (int m = j - 1; m <= j + 1; m++)
                    {
                        // if it's not a quadrant outside the chunk, ignore it
                        bool isIntern = (k < 0 || k >= chunk.subdivisions || m < 0 || m >= chunk.subdivisions);
                        if (!isIntern)
                            continue;

                        bool isCorner = (k == -1 && m == -1) ||
                                        (k == -1 && m == chunk.subdivisions) ||
                                        (k == chunk.subdivisions && m == -1) ||
                                        (k == chunk.subdivisions && m == chunk.subdivisions);
                        if (isCorner)
                            continue;

                        // compute normalized coordinates of quadrant -------------
                        float quadrantX = (chunk.position.x - 0.5f + (1.0f / (float)chunk.subdivisions) * k);
                        float quadrantY = (chunk.position.y - 0.5f + (1.0f / (float)chunk.subdivisions) * m);
                        Vector2 quadrantCoordinates = new Vector2(quadrantX, quadrantY);
                        int quadrantWidth = (int)(chunk.size / (float)chunk.subdivisions);
                        int quadrantHeight = (int)(chunk.size / (float)chunk.subdivisions);

                        // calculate the outern control point ----------------------
                        ControlPoint candidate = Quadrant.computeControlPoint(quadrantCoordinates, quadrantWidth, quadrantHeight);

                        // if it's the nearest, we got it --------------------------
                        float distance = Vector2.Distance(candidate.position, quadrant.roadsControlPoint.position);
                        if (distance < segmentMaximumLength * quadrantWidth)
                            toBeLinked.Add(candidate);

                    }

                if (toBeLinked.Count == 0)
                    break;

                // at this point, we have to understant in which path the outern points should be included ------
                foreach (ControlPoint cp in toBeLinked)
                {
                    Path newPath = new Path();
                    newPath.addControlPoint(cp);
                    newPath.addControlPoint(quadrant.roadsControlPoint);
                    paths.Add(newPath);
                }

            }
        }


    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UTILITIES ------------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UTILITIES

    /* ------------------------------------------------------------------------------------------------- */
    private static Quadrant findNearestNeighbor(Quadrant q, MapSector chunk, Path path, float segmentMaximumLength)
    {
        Quadrant neighbor = null;

        int localX = (int)q.localPosition.x;
        int localY = (int)q.localPosition.y;
        ControlPoint point = q.roadsControlPoint;
        float currentMinimumDistance = segmentMaximumLength * q.width;

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
    private static Quadrant isMissingLinks(MapSector chunk)
    {

        foreach (Quadrant q in chunk.quadrants.Values)
        {
            if (q.roadsControlPoint.NumberOfPaths == 0)
                return q;
        }

        return null;
    }

    #endregion  // UTILITIES

    #endregion
}

