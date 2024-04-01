using UnityEditor;
using UnityEngine;

namespace SceneBuilder
{
    [CreateAssetMenu(fileName = "SceneBuilderData", menuName = "SAO/Scene Builder Data")]
    public class SceneBuilderData : ScriptableObject
    {
        [SerializeField] public SceneAsset[] scenes;
        
        public int mainSceneIndex;
        public string outputPath;
        public bool unpackPrefabs;
    }

    [CustomEditor(typeof(SceneBuilderData))]
    public class SceneBuilderDataEditor : Editor
    {
        private bool showDefaultInspector;
        
        public override void OnInspectorGUI()
        {
            showDefaultInspector = EditorGUILayout.Toggle("Show Default Inspector", showDefaultInspector);

            if (showDefaultInspector)
            {
                base.OnInspectorGUI();
            }

            SceneBuilderData data = (SceneBuilderData)target;

            EditorGUI.BeginChangeCheck();
            
            //draw inspector for scene list
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            for (int i = 0; i < data.scenes.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(i == data.mainSceneIndex);
                if (GUILayout.Button(i == data.mainSceneIndex ? "Main scene" : "Mark as main scene"))
                {
                    data.mainSceneIndex = i;
                }
                EditorGUI.EndDisabledGroup();

                data.scenes[i] = (SceneAsset) EditorGUILayout.ObjectField(data.scenes[i], typeof(SceneAsset), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    data.scenes[i] = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Scene"))
            {
                ArrayUtility.Add(ref data.scenes, null);
            }
            
            EditorGUILayout.EndVertical();
            
            //draw inspector for output path
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Output Path", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(data.outputPath);
            if (GUILayout.Button("Set Output Path"))
            {
                //save file dialog (.unity extension is required)
                data.outputPath = EditorUtility.SaveFilePanel("Select Output Path", data.outputPath, "", "unity");

                //make sure path is relative to project
                data.outputPath = data.outputPath.Replace(Application.dataPath, "Assets");
            }
            EditorGUILayout.EndVertical();
            
            //draw inspector for unpack prefabs
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Unpack Prefabs", EditorStyles.boldLabel);
            data.unpackPrefabs = EditorGUILayout.ToggleLeft(new GUIContent("Unpack Prefabs", "Will use Instantiate to copy over GameObjects causing all prefabs to be unpacked"), data.unpackPrefabs);
            EditorGUILayout.EndVertical();
            
            //draw inspector for build button
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Scene"))
            {
                SceneBuilder.BuildScene(data);
            }
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                //record changes
                EditorUtility.SetDirty(data);
            }
        }
    }
}