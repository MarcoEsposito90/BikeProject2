using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public class RoadsGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    //[Range(0, 200)]
    //public int sinuosity;

    public ControlPoint.Neighborhood neighborhood;

    [Range(1, 6)]
    public int largeRoads;
    private float roadWidth;

    [Range(0, 6)]
    public int croassRoadsDimension;
    private float distanceFromCrossroad;

    [Range(1, 4)]
    public int baseSegmentDimension;
    private float segmentLength;

    [Range(1, 6)]
    public int sinuosity;
    private float tangentRescale;

    //[Range(10, 50)]
    //public int roadsFlattening;

    [Range(1.0f, 2.0f)]
    public float maximumSegmentLength;
    private float maxLength;

    [Range(0.0f, 0.5f)]
    public float minimumSegmentLength;
    private float minLength;

    [Range(0.0f, 0.5f)]
    public float minimumRoadsHeight;

    [Range(0.5f, 1.0f)]
    public float maximumRoadsHeight;

    [Range(0.0f, 10.0f)]
    public float terrainOffset;

    public EndlessRoadsGenerator parent;

    private Graph<Vector2, ControlPoint> controlPointsGraph;
    private Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve> curves;
    private bool initialized;
    private object synchVariable;
    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        initialized = false;
        synchVariable = new object();
        controlPointsGraph = new Graph<Vector2, ControlPoint>();
        curves = new Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve>();

        roadWidth = largeRoads * 0.25f;
        distanceFromCrossroad = croassRoadsDimension * 1.0f;
        segmentLength = 0.5f * baseSegmentDimension;
        tangentRescale = 0.25f * sinuosity - 0.25f;
    }


    void Update()
    {
    }

    #endregion


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- METHODS -----------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region METHODS

    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINTS -------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CONTROL_POINTS

    public void sendNewControlPoint(ControlPoint cp)
    {
        ThreadStart ts = delegate
        {
            lock (synchVariable)
            {
                addControlPoint(cp);
            }
        };

        Thread t = new Thread(ts);
        t.Start();
    }

    /* -------------------------------------------------------------------------------------- */
    private void addControlPoint(ControlPoint cp)
    {
        if (!initialized)
        {
            maxLength = maximumSegmentLength * cp.AreaSize;
            minLength = minimumSegmentLength * cp.AreaSize;
            initialized = true;
        }

        // add point to graph --------------------------------------
        Graph<Vector2, ControlPoint>.GraphItem gi = controlPointsGraph.createItem(cp.gridPosition, cp);

        // check if the point is not on water or on mountains ------
        float noiseValue = NoiseGenerator.Instance.getNoiseValue(
            1,
            gi.item.position.x,
            gi.item.position.y);

        bool linkable = noiseValue < maximumRoadsHeight && noiseValue > minimumRoadsHeight;
        cp.linkable = linkable;
        if (!linkable)
            return;

        //Debug.Log("point " + cp.gridPosition + " is linkable");

        // if the point is linkable to others, we will get here ----

        // create links (if possible) ------------------------------
        foreach (Vector2 target in cp.getNeighborsGridPositions(neighborhood))
        {
            if (controlPointsGraph.containsItem(target))
            {
                ControlPoint toLink = controlPointsGraph[target].item;
                if (!toLink.linkable)
                    continue;

                float dist = cp.distance(toLink);
                if (dist < maxLength && dist > minLength)
                {
                    //Debug.Log("creating link: " + cp.gridPosition + " - " + toLink.gridPosition);
                    if (!checkLinkFeasibility(cp, toLink))
                        continue;

                    Graph<Vector2, ControlPoint>.Link l =
                        controlPointsGraph.createLink(cp.gridPosition, toLink.gridPosition);
                }
            }
        }


        // create new curves, where it is possible ------------------
        foreach (Graph<Vector2, ControlPoint>.Link l in controlPointsGraph.links)
        {
            lock (curves)
            {
                if (curves.ContainsKey(l))
                    continue;
            }

            //Debug.Log("scanning link " + l.from.item.position + " - " + l.to.item.position);

            ControlPoint from = l.from.item;
            ControlPoint to = l.to.item;

            bool canCreateCurve = true;
            foreach (Vector2 target in from.getNeighborsGridPositions(neighborhood))
                if (!controlPointsGraph.containsItem(target))
                {
                    canCreateCurve = false;
                    break;
                }

            if (!canCreateCurve)
                continue;

            //Debug.Log("control1 passed");

            foreach (Vector2 target in to.getNeighborsGridPositions(neighborhood))
                if (!controlPointsGraph.containsItem(target))
                {
                    canCreateCurve = false;
                    break;
                }

            //Debug.Log("control2 passed? " + canCreateCurve);

            if (canCreateCurve)
                createCurve(l);

        }

        // we have now to create the crossroads meshes
        foreach (Graph<Vector2, ControlPoint>.GraphItem it in controlPointsGraph.nodes.Values)
        {
            //Dictionary<ControlPoint, Graph<Vector2,ControlPoint>.Link> points = new Dictionary<ControlPoint, Graph<Vector2, ControlPoint>.Link>();
            List<Graph<Vector2, ControlPoint>.Link> incomingCurves = new List<Graph<Vector2, ControlPoint>.Link>();
            foreach (Graph<Vector2, ControlPoint>.Link link in it.links)
            {
                if (!curves.ContainsKey(link))
                    continue;

                incomingCurves.Add(link);
            }
         
               
        }
    }




    /* -------------------------------------------------------------------------------------- */
    private bool checkLinkFeasibility(ControlPoint a, ControlPoint b)
    {
        int numberOfChecks = 10;
        for (int i = 1; i < numberOfChecks; i++)
        {
            Vector2 checkPos = a.position + (b.position - a.position) / (float)numberOfChecks;
            float n = NoiseGenerator.Instance.getNoiseValue(1, checkPos.x, checkPos.y);
            if (n > maximumRoadsHeight || n < minimumRoadsHeight)
                return false;
        }

        return true;
    }



    /* -------------------------------------------------------------------------------------- */
    public void removeControlPoint(ControlPoint cp)
    {
        // TODO
    }

    #endregion

    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVES ---------------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CURVES

    private void createCurve(Graph<Vector2, ControlPoint>.Link link)
    {

        //Debug.Log("creating curve " + link.from.item.gridPosition + " - " + link.to.item.position);
        Vector2 startTangent = getTangent(link.from, link);
        Vector2 endTangent = getTangent(link.to, link);

        BezierCurve c = new BezierCurve(
            link.from.item.position,
            startTangent,
            link.to.item.position,
            endTangent);

        lock (curves)
        {
            curves.Add(link, c);
        }

        RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateMeshData(
            c,
            roadWidth,
            distanceFromCrossroad,
            segmentLength);
        Road.RoadData data = new Road.RoadData(rmd, link, c);
        parent.roadsResultsQueue.Enqueue(data);
    }

    /* -------------------------------------------------------------------------------------- */
    private Vector2 getTangent(Graph<Vector2, ControlPoint>.GraphItem point, Graph<Vector2, ControlPoint>.Link exclude)
    {
        List<Vector2> linksPositions = new List<Vector2>();

        foreach (Graph<Vector2, ControlPoint>.Link l in point.links)
        {
            if (l.Equals(exclude))
                continue;

            ControlPoint cp = null;
            if (l.from.Equals(point))
                cp = l.to.item;
            else
                cp = l.from.item;

            linksPositions.Add(cp.position);
        }

        Graph<Vector2, ControlPoint>.GraphItem toward = null;
        if (exclude.from.Equals(point))
            toward = exclude.to;
        else
            toward = exclude.from;

        Vector2 tangent = point.item.getAverageVector(
            linksPositions,
            false,
            false,
            true,
            toward.item.position);

        return tangent * tangentRescale + point.item.position;
    }

    #endregion

    #endregion  // METHODS

}
