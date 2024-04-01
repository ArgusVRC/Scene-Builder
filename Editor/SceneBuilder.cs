using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneBuilder
{
    public class SceneBuilder : EditorWindow
    {
        [MenuItem("SAO/Scene Builder")]
        public static void ShowWindow()
        {
            GetWindow<SceneBuilder>("Scene Builder");
        }

        private SceneBuilderData[] datas;
        private SceneBuilderData selectedData;

        private bool loadBuiltSceneOnFinish = false;

        private void OnEnable()
        {
            //find all scene builder data in the assets folder
            string[] assets = AssetDatabase.FindAssets("t:SceneBuilderData");
            datas = assets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SceneBuilderData>).ToArray();

            if (datas.Any())
            {
                selectedData = datas[0];
            }
            
            loadBuiltSceneOnFinish = EditorPrefs.GetBool("SceneBuilderLoadBuiltSceneOnFinish", false);
            
            SceneView.duringSceneGui += OnSceneGUI;
            
            DoSceneCheck();
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (isOutputScene)
            {
                //draw a warning in the scene view if the user is editing the output scene
                Handles.BeginGUI();
                
                Handles.color = Color.red;
                Handles.DrawSolidRectangleWithOutline(new Rect(10, 10, 600, 100), new Color(1, 0, 0, 0.2f), Color.red);
                
                //label with bigger text
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.black;
                style.contentOffset = new Vector2(2, 2);
                style.fontSize = 20;

                GUI.Label(new Rect(20, 20, 580, 80), "You are editing a scene builder output scene!\nNo changes will be retained if you make changes here!", style);

                //drop shadow on text
                style.normal.textColor = Color.white;
                style.contentOffset = new Vector2(0, 0);
                
                GUI.Label(new Rect(20, 20, 580, 80), "You are editing a scene builder output scene!\nNo changes will be retained if you make changes here!", style);
                
                Handles.EndGUI();
            }
        }

        private bool isOutputScene;
        
        public void DoSceneCheck()
        {
            //check if the user is editing the output scene
            var openScenes = EditorSceneManager.GetSceneManagerSetup();

            isOutputScene = false;
            
            if (openScenes.Any(x => x.path == selectedData.outputPath))
            {
                Debug.LogError("You are editing an output scene! Make sure you know what you're doing and don't make any changes you want to persist!");
                isOutputScene = true;
            }
        }
        
        private void OnGUI()
        {
            if(isBuilding) 
            {
                EditorGUILayout.LabelField("Building scene: " + selectedData.name);

                if (!isReady)
                {
                    EditorGUILayout.LabelField("Current task: ", _nextTask);

                    if (GUILayout.Button("Continue"))
                    {
                        isReady = true;
                    }
                }
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            selectedData = (SceneBuilderData)EditorGUILayout.ObjectField("Scene Builder Data", selectedData,
                typeof(SceneBuilderData), false);

            //draw a dropdown for all scene builder data
            int selectedIndex = Array.IndexOf(datas, selectedData);
            selectedIndex = EditorGUILayout.Popup(selectedIndex, Array.ConvertAll(datas, data => data.name), GUILayout.Width(200));

            if (selectedIndex != -1)
                selectedData = datas[selectedIndex];
            EditorGUILayout.EndHorizontal();

            if (!selectedData) return;
            
            // Quick access to scenes in the data
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick load scenes", EditorStyles.boldLabel);
            
            foreach (var selectedSceneData in selectedData.scenes)
            {
                if (!selectedSceneData) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(selectedSceneData.name);
                
                if (GUILayout.Button("Load Scene", GUILayout.Width(150)))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(selectedSceneData), OpenSceneMode.Single);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Output Scene", EditorStyles.boldLabel);
            
            // Check if the output scene path has an associated asset
            SceneAsset outputScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(selectedData.outputPath);

            if (outputScene)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Output Scene: " + outputScene.name);
                if (GUILayout.Button("Load Scene", GUILayout.Width(150)))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(outputScene), OpenSceneMode.Single);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUI.BeginChangeCheck();
            loadBuiltSceneOnFinish = EditorGUILayout.ToggleLeft("Load Built Scene On Finish", loadBuiltSceneOnFinish);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("SceneBuilderLoadBuiltSceneOnFinish", loadBuiltSceneOnFinish);
            }

            bool automaticBuildChanged = EditorGUILayout.ToggleLeft("Automatic build", automaticBuild);
            if (!automaticBuildChanged && automaticBuild)
            {
                //ask the user to confirm if they really want to disable automatic build
                if (EditorUtility.DisplayDialog("Disable automatic build?",
                    "Are you sure you want to disable automatic build? This will require a manual input for every build step and is only really used for debugging.",
                    "Yes", "No"))
                {
                    automaticBuild = false;
                }
            }
            else if (automaticBuildChanged && !automaticBuild) automaticBuild = true;
            
            bool isPathValid = string.IsNullOrWhiteSpace(selectedData.outputPath);
            EditorGUI.BeginDisabledGroup(isPathValid);

            if (GUILayout.Button("Build Scene"))
            {
                BuildScene(selectedData, loadBuiltSceneOnFinish);
            }
            
            EditorGUI.EndDisabledGroup();
        }

        private static bool isBuilding = false;

        public static async void BuildScene(SceneBuilderData data, bool loadBuiltScene = true)
        {
            isBuilding = true;
            
            //ask the user to save the currently opened scene
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                
            //get current scene asset so we can load it later
            SceneAsset currentSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneManager.GetActiveScene().path);

            try
            {
                //load empty scene
                await ShowProgress("Loading empty scene...");

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                //load the main scene
                await ShowProgress("Loading main scene...");

                Dictionary<Scene, SceneBuilderSceneData> sceneData = new Dictionary<Scene, SceneBuilderSceneData>();

                //create a copy of the main scene
                Scene mainScene =
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(data.scenes[data.mainSceneIndex]),
                        OpenSceneMode.Additive);

                List<Scene> targetScenes = new List<Scene>();

                await ShowProgress("Loading additive scenes...");

                //load additive scenes from data
                for (int index = 0; index < data.scenes.Length; index++)
                {
                    if (index == data.mainSceneIndex) continue;

                    SceneAsset scene = data.scenes[index];
                    if (scene == null)
                    {
                        Debug.LogWarning("Scene Builder: Scene is null");
                        continue;
                    }

                    string scenePath = AssetDatabase.GetAssetPath(scene);

                    Debug.Log("Scene Builder: Loading scene " + scenePath);
                    await Task.Delay(100);

                    targetScenes.Add(EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive));
                }

                await ShowProgress("Loading scene data...");

                //load scene data from loaded scenes
                for (int index = 0; index < targetScenes.Count;)
                {
                    Scene scene = targetScenes[index];
                    SceneBuilderSceneData sceneBuilderSceneData = FindObjectsOfType<SceneBuilderSceneData>()
                        .FirstOrDefault(x => x.gameObject.scene == scene);

                    if (sceneBuilderSceneData == null)
                    {
                        Debug.LogWarning("Scene Builder: Scene Builder Scene Data in scene " + scene.name +
                                         " is null, it will not be included in the build");

                        //unload scene
                        EditorSceneManager.CloseScene(scene, true);
                        targetScenes.Remove(scene);
                        continue;
                    }

                    sceneData.Add(scene, sceneBuilderSceneData);
                    index++;
                }

                await ShowProgress("Building scene...");

                Dictionary<Scene, GameObject> rootObjects = new Dictionary<Scene, GameObject>();

                //instantiate copies of target objects from additive scenes into the main scene copy
                foreach (KeyValuePair<Scene, SceneBuilderSceneData> pair in sceneData)
                {
                    SceneManager.SetActiveScene(pair.Key);

                    //instantiate a parent object inside the additive scene that objects originate from
                    GameObject parentObject = new GameObject(pair.Key.name);

                    foreach (GameObject target in pair.Value.includedSceneObjects)
                    {
                        if (target == null)
                        {
                            Debug.LogWarning("Scene Builder: Target Object in scene " + pair.Key.name +
                                             " is null, it will not be included in the build");
                            continue;
                        }

                        //move the object to the parent object
                        target.transform.SetParent(parentObject.transform);
                    }

                    //move the parent object to the main scene
                    SceneManager.MoveGameObjectToScene(parentObject, mainScene);

                    rootObjects.Add(pair.Key, parentObject);
                }

                if (data.unpackPrefabs)
                {
                    await ShowProgress("Unpacking prefabs...");

                    SceneManager.SetActiveScene(mainScene);

                    //make a copy of the rootObjects dictionary so we can iterate over it while modifying the original
                    Dictionary<Scene, GameObject> rootObjectsCopy = new Dictionary<Scene, GameObject>(rootObjects);
                    
                    //copy the root objects using Instantiate so that they are unpacked, then remove the originals
                    foreach (KeyValuePair<Scene, GameObject> pair in rootObjectsCopy)
                    {
                        GameObject copy = Instantiate(pair.Value);
                        copy.name = pair.Value.name;

                        DestroyImmediate(pair.Value);
                        rootObjects[pair.Key] = copy;
                    }
                }

                await ShowProgress("Running build post processors...");

                SceneManager.SetActiveScene(mainScene);

                //run build post processors
                foreach (KeyValuePair<Scene, SceneBuilderSceneData> pair in sceneData)
                {
                    foreach (SceneBuildProcessor postProcessor in pair.Value.processors)
                    {
                        if (postProcessor == null)
                        {
                            Debug.LogWarning("Scene Builder: Post Processor in scene " + pair.Key.name +
                                             " is null, it will not be included in the build");
                            continue;
                        }

                        postProcessor.OnBuildScene(rootObjects[pair.Key]);
                    }
                }

                await ShowProgress("Cleaning up...");
                
                //delete existing output scene if it exists
                SceneAsset outputScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(data.outputPath);

                //if it exists, delete it
                if (outputScene != null)
                {
                    AssetDatabase.DeleteAsset(data.outputPath);
                }

                await ShowProgress("Saving built scene...");

                //save the main scene copy as a new scene at the target path
                EditorSceneManager.SaveScene(mainScene, data.outputPath, true);
                
                //close the main scene copy
                EditorSceneManager.CloseScene(mainScene, true);
                
                //unload all additive scenes
                foreach (Scene scene in targetScenes)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
                
                mainScene = EditorSceneManager.OpenScene(data.outputPath, OpenSceneMode.Single);

                await ShowProgress("Running final scene processors...");
                
                //Find SceneBuilderMainSceneData in the main scene
                var mainSceneData = FindObjectsOfType<SceneBuilderMainSceneData>().FirstOrDefault();
                
                //Destroy all excluded game objects from the main scene
                if (mainSceneData != null)
                {
                    foreach (var excludedGameObject in mainSceneData.excludedSceneGameObjects)    
                    {
                        DestroyImmediate(excludedGameObject);
                    }
                }
                
                //run post processors
                //get classes that inherit from SceneBuildPostProcessor
                IEnumerable<Type> postProcessorTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .Where(x => typeof(SceneBuildPostProcessor).IsAssignableFrom(x) && !x.IsAbstract);
                foreach(Type type in postProcessorTypes)
                {
                    SceneBuildPostProcessor postProcessor = (SceneBuildPostProcessor) Activator.CreateInstance(type);
                    postProcessor.OnPostBuildScene();
                }
                
                //save the main scene
                EditorSceneManager.SaveScene(mainScene);
                
                //close the main scene
                EditorSceneManager.CloseScene(mainScene, true);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Scene Builder", "An error occurred while building the scene: " + e.Message,
                    "Ok");
                
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isBuilding = false;

                if (loadBuiltScene)
                {
                    EditorSceneManager.OpenScene(data.outputPath, OpenSceneMode.Single);
                }
                else
                {
                    //load original scene
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(currentSceneAsset), OpenSceneMode.Single);
                }
            }
        }

        private static bool isReady = false;
        private static string _nextTask = "";
        private static float progress = 0.0f;
        private static bool automaticBuild = true;
        public static async Task ShowProgress(string nextTask)
        {
            if (!automaticBuild)
            {
                _nextTask = nextTask;

                EditorUtility.ClearProgressBar();
                isReady = false;
                while (!isReady)
                {
                    await Task.Delay(100);
                }
            }

            EditorUtility.DisplayProgressBar("Scene Builder", nextTask, progress);
            progress += 0.1f;
            
            await Task.Delay(100);
        }
    }

    [InitializeOnLoad]
    public static class SceneWatcher
    {
        static SceneWatcher()
        {
            EditorSceneManager.sceneOpened += SceneOpened;
        }
        
        private static void SceneOpened(Scene scene, OpenSceneMode mode)
        {
            //if scene builder window is open
            if (EditorWindow.HasOpenInstances<SceneBuilder>())
            {
                SceneBuilder sceneBuilder = EditorWindow.GetWindow<SceneBuilder>();
                sceneBuilder.DoSceneCheck();
            }
        }
    }
}