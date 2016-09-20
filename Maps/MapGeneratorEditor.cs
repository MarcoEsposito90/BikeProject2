using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{

    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;
        mapGenerator.GenerateMap(0, 0);

        if (DrawDefaultInspector() && mapGenerator.autoUpdate)
            mapGenerator.GenerateMap(0, 0);

        if (GUILayout.Button("Generate"))
            mapGenerator.GenerateMap(0, 0);
    }
}
