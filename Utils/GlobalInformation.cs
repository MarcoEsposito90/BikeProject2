using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalInformation {

    /* ---------------------------------------------------------------------- */
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


    /* ---------------------------------------------------------------------- */
    Dictionary<string, object> datas;


    private GlobalInformation()
    {
        datas = new Dictionary<string, object>();
    }

    public void addData(string key, object obj)
    {
        lock (datas)
        {
            datas.Add(key, obj);
        }
    }

    public object getData(string key)
    {
        lock (datas)
        {
            if (!datas.ContainsKey(key))
                return null;

            return datas[key];
        }
    }
}
