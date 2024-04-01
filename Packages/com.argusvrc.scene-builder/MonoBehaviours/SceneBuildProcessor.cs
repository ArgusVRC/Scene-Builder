using UnityEngine;

namespace SceneBuilder
{
    public abstract class SceneBuildProcessor : MonoBehaviour
    {
        public abstract void OnBuildScene(GameObject copiedScene);
    }

    public abstract class SceneBuildPostProcessor
    {
        public abstract void OnPostBuildScene();
    }
}