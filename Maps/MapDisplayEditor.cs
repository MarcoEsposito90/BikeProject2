using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MapDisplay))]
public class MapDisplayEditor : Editor {

    public override void OnInspectorGUI()
    {
        MapDisplay mapDisplay = (MapDisplay)target;

        if (DrawDefaultInspector() && mapDisplay.autoUpdate)
            mapDisplay.drawNoiseMap();

        if (GUILayout.Button("Generate"))
            mapDisplay.drawNoiseMap();
    }
}
