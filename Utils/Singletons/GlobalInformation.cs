﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalInformation
{

    /* ---------------------------------------------------------------------- */
    /* --------------------------- TAGS ------------------------------------- */
    /* ---------------------------------------------------------------------- */

    #region TAGS

    // these string values must be equal to those in project settings
    public static readonly string OBJECT1_TAG = "Object1";
    public static readonly string OBJECT2_TAG = "Object2";
    public static readonly string OBJECT3_TAG = "Object3";
    public static readonly string ROAD_TAG = "Road";
    public static readonly string VILLAGE_TAG = "Village";
    public static readonly string TERRAIN_TAG = "Terrain";

    // the tags must be stored in the array in order of priority (lower first, terrain excluded)
    public static readonly string[] TAGS = { OBJECT1_TAG, OBJECT2_TAG, OBJECT3_TAG, VILLAGE_TAG, ROAD_TAG };

    public static int getPriority(string tag)
    {
        for (int i = 0; i < TAGS.Length; i++)
        {
            if (TAGS[i].Equals(tag))
                return i;
        }

        // the given tag is not in the list
        return -1;
    }

    #endregion

    /* ---------------------------------------------------------------------- */
    /* --------------------------- INSTANCE --------------------------------- */
    /* ---------------------------------------------------------------------- */


    #region INSTANCE

    private static readonly object synchVariable = new object();
    private static GlobalInformation _Instance = null;
    public static GlobalInformation Instance
    {
        get
        {
            lock (synchVariable)
            {
                if (_Instance == null) _Instance = new GlobalInformation();
                return _Instance;
            }

        }
        private set
        {
            lock (synchVariable)
            {
                _Instance = value;
            }
        }
    }

    private GlobalInformation()
    {
        datas = new Dictionary<string, object>();
    }

    #endregion


    /* ---------------------------------------------------------------------- */
    /* --------------------------- METHODS ---------------------------------- */
    /* ---------------------------------------------------------------------- */

    #region METHODS

    Dictionary<string, object> datas;


    /* ---------------------------------------------------------------------- */
    public void addData(string key, object obj)
    {
        lock (datas)
        {
            if (datas.ContainsKey(key))
                datas[key] = obj;
            else
                datas.Add(key, obj);
        }
    }


    /* ---------------------------------------------------------------------- */
    public object getData(string key)
    {
        lock (datas)
        {
            if (!datas.ContainsKey(key))
                return null;

            return datas[key];
        }
    }


    /* ---------------------------------------------------------------------- */
    public float getHeight(Vector2 position)
    {
        float n = NoiseGenerator.Instance.getNoiseValue(1, position.x, position.y);
        return getHeight(n);
    }


    /* ---------------------------------------------------------------------- */
    public float getHeight(float noiseValue)
    {
        AnimationCurve heightCurve = (AnimationCurve)datas[MapDisplay.MESH_HEIGHT_CURVE];
        float mul = (float)datas[MapDisplay.MESH_HEIGHT_MUL];

        lock (heightCurve)
        {
            return heightCurve.Evaluate(noiseValue) * mul;
        }
    }


    #endregion
}
