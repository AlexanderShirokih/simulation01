using UnityEditor;
using UnityEngine;

namespace Test
{
    [CustomEditor(typeof(BiomeTest))]
    public class BiomesGradientMaxPicker : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Pick"))
                ((BiomeTest) target).Pick();
        }
    }
}