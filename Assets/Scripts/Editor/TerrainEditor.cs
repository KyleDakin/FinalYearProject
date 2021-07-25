using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

[CustomEditor(typeof(TerrainGeneration))]
public class TerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGeneration terrainGeneration = (TerrainGeneration) target;

        if (DrawDefaultInspector())
        {
            if(terrainGeneration.autoUpdate)
            {
                terrainGeneration.Generate();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            terrainGeneration.Generate();
        }
        
    }
}
