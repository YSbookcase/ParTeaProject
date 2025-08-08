using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace JTW_JumpGame
{
    public class BaseUI : MonoBehaviour
    {
        private Dictionary<string, GameObject> gameObjectDic;
        private Dictionary<string, Component> componentDic;

        private void Awake()
        {
            RectTransform[] transforms = GetComponentsInChildren<RectTransform>(true);
            gameObjectDic = new Dictionary<string, GameObject>(transforms.Length * 4);
            foreach (RectTransform child in transforms)
            {
                gameObjectDic.TryAdd(child.gameObject.name, child.gameObject);
            }

            Component[] components = GetComponentsInChildren<Component>(true);
            componentDic = new Dictionary<string, Component>(components.Length * 4);
            foreach (Component child in components)
            {
                componentDic.TryAdd($"{child.gameObject.name}_{child.GetType().Name}", child);
            }
        }

        public GameObject GetUI(in string name)
        {
            gameObjectDic.TryGetValue(name, out GameObject gameObject);
            return gameObject;
        }

        public T GetUI<T>(in string name) where T : Component
        {
            componentDic.TryGetValue($"{name}_{typeof(T).Name}", out Component component);
            if (component != null)
                return component as T;

            GameObject gameObject = GetUI(name);
            if (gameObject == null)
                return null;

            component = gameObject.GetComponent<T>();
            if (component == null)
                return null;

            componentDic.TryAdd($"{name}_{typeof(T).Name}", component);
            return component as T;
        }
    }
}



