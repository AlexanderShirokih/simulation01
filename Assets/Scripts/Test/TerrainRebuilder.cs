using Simulation01.Terrain;
using UnityEditor;
using UnityEngine;

namespace Test
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainRebuilder : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var myTerrainGenerator = (TerrainGenerator) target;
            if (GUILayout.Button("Rebuild"))
            {
                myTerrainGenerator.Rebuild();
            }
        
        }
    }
}