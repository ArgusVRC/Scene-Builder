using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneBuilder.BuildProcessors
{
    public class CollisionExtractor : SceneBuildProcessor
    {
        public override void OnBuildScene(GameObject copiedScene)
        {
            Debug.Log("Collision Extractor: Extracting collisions from '" + copiedScene.name + "'");
            
            //clone the copied scene object
            GameObject colliderVersion = Instantiate(copiedScene);
            colliderVersion.name = $"{copiedScene.name} (Collisions)";
            
            Debug.Log($"Created collider version of scene: {colliderVersion.name}", colliderVersion);
            
            //remove all colliders from the base scene
            Collider[] colliders = copiedScene.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                DestroyImmediate(c);
            }
            
            //remove all components except for the colliders from colliderVersion
            Component[] components = colliderVersion.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null) continue;
                
                if (component is Collider || component is Transform)
                    continue;
                
                //remove the component. because dependencies are pain we need to do it in a *special* way
                if (HasDependencies(component))
                {
                    RemoveComponentWithDependencies(component);
                }
                else
                {
                    DestroyImmediate(component);
                }
            }

            //remove empty gameobjects from colliderVersion
            Transform[] transforms = colliderVersion.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t == null) continue;
                
                if(t.GetComponentInChildren<Collider>() == null)
                    DestroyImmediate(t.gameObject);
            }
        }

        Dictionary<Type, bool> componentDependencies = new Dictionary<Type, bool>();

        public bool HasDependencies(Component c)
        {
            //try to get the dependencies from the dictionary
            if (componentDependencies.TryGetValue(c.GetType(), out bool hasDependencies))
            {
                return hasDependencies;
            }

            RequireComponent[] requiredComponents = (RequireComponent[])c.GetType().GetCustomAttributes(typeof(RequireComponent), true);
            
            componentDependencies.Add(c.GetType(), requiredComponents.Length > 0);
            return requiredComponents.Length > 0;
        }
        
        //removes component of type T from the given gameobject by scanning for its dependencies and removing them first
        public void RemoveComponentWithDependencies(Component c)
        {
            if (c == null) return;

            Debug.Log($"Component of type {c.GetType()} has dependencies");
            
            //get dependencies
            RequireComponent[] requiredComponents = (RequireComponent[])c.GetType().GetCustomAttributes(typeof(RequireComponent), true);
            
            List<Component> dependencies = new List<Component>();
            
            //find dependencies
            foreach (RequireComponent requiredComponent in requiredComponents)
            {
                if (requiredComponent.m_Type0 != null && c.GetComponent(requiredComponent.m_Type0) != null)
                {
                    dependencies.Add(c.GetComponent(requiredComponent.m_Type0));
                }
                if (requiredComponent.m_Type1 != null && c.GetComponent(requiredComponent.m_Type1) != null)
                {
                    dependencies.Add(c.GetComponent(requiredComponent.m_Type1));
                }
                if (requiredComponent.m_Type2 != null && c.GetComponent(requiredComponent.m_Type2) != null)
                {
                    dependencies.Add(c.GetComponent(requiredComponent.m_Type2));
                }
            }
            
            //list of dependency names
            string dependencyNames = string.Join(", ", dependencies.Select(d => d.GetType().Name).ToArray());
            
            Debug.Log("Found dependencies: " + dependencyNames);
            
            //remove component
            DestroyImmediate(c);
            
            //remove dependencies
            foreach (Component dependency in dependencies)
            {
                if (HasDependencies(dependency))
                {
                    RemoveComponentWithDependencies(dependency);
                }
                else
                {
                    if (dependency != null && dependency.GetType() != typeof(Transform))
                    {
                        DestroyImmediate(dependency);
                    }
                }
            }
        }
    }
}