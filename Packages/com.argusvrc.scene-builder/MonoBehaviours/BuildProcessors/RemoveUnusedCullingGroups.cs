using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneBuilder.BuildProcessors
{
    public class RemoveUnusedCullingGroups : SceneBuildProcessor
    {
        public override void OnBuildScene(GameObject copiedScene)
        {
            //get all colliders in the scene
            Collider[] colliders = copiedScene.GetComponentsInChildren<Collider>(true);
            
            //filter by colliders that are on disabled gameobjects
            List<Collider> disabledColliders = colliders.Where(c => !c.gameObject.activeSelf).ToList();
            
            //filter by gameobjects that are prefixed with "_CG_"
            List<Collider> cullingGroupColliders = disabledColliders.Where(c => c.gameObject.name.StartsWith("_CG_")).ToList();
            
            //copy to array
            GameObject[] cullingGroupObjects = cullingGroupColliders.Select(c => c.gameObject).ToArray();
            
            //destroy all matching gameobjects
            for (int index = 0; index < cullingGroupObjects.Length; index++)
            {
                GameObject g = cullingGroupObjects[index];
                DestroyImmediate(g);
            }
            
            Debug.Log("Removed " + cullingGroupObjects.Length + " unused culling groups");
        }
    }
}