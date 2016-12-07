using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalInformation {

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
            lock(synchVariable)
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

        return heightCurve.Evaluate(noiseValue) * mul;
    }


    #endregion
}
