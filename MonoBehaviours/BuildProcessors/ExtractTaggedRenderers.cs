using System.Collections.Generic;
using UnityEngine;

namespace SceneBuilder.BuildProcessors
{
    public class ExtractTaggedRenderers : SceneBuildProcessor
    {
        public GameObject outputTemplate;
        public string[] tags;
        
        public override void OnBuildScene(GameObject copiedScene)
        {
            //get all renderers in the copied scene
            Transform[] transforms = copiedScene.GetComponentsInChildren<Transform>(true);

            List<Transform> taggedObjects = new List<Transform>();
            
            //iterate through all renderers
            foreach (Transform t in transforms)
            {
                //check if the renderer's gameobject has any of the tags
                foreach (string tag in tags)
                {
                    if (t.gameObject.CompareTag(tag))
                    {
                        taggedObjects.Add(t);
                    }
                }
            }

            Transform outputLocation;
            
            //create a new child in copiedScene for the tagged renderers
            if (outputTemplate != null)
            {
                outputLocation = Instantiate(outputTemplate, copiedScene.transform, true).transform;
            }
            else
            {
                outputLocation = new GameObject("Tagged objects").transform;
                outputLocation.transform.SetParent(copiedScene.transform);
            }

            //move all tagged objects to the new child
            foreach (Transform t in taggedObjects)
            {
                t.SetParent(outputLocation);
            }
        }
    }
}