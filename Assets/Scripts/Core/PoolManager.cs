using System.Collections.Generic;
using UnityEngine;

namespace DwarvesTD.Core
{
    public class PooledInstance : MonoBehaviour
    {
        public GameObject SourcePrefab { get; set; }
    }

    /// <summary>
    /// Gerenciador genrico de Object Pooling de alta performance.
    /// Evita reparenting para nao poluir o cache de transform da Unity.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> poolQueues = new Dictionary<GameObject, Queue<GameObject>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!poolQueues.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>(32);
                poolQueues[prefab] = queue;
            }

            GameObject obj;

            while (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj != null)
                {
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.SetActive(true);
                    return obj;
                }
            }

            // Instancia novo caso o pool esteja vazio
            obj = Instantiate(prefab, position, rotation);
            PooledInstance tracker = obj.GetComponent<PooledInstance>();
            if (tracker == null) tracker = obj.AddComponent<PooledInstance>();
            tracker.SourcePrefab = prefab;

            return obj;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            PooledInstance tracker = instance.GetComponent<PooledInstance>();
            if (tracker != null && tracker.SourcePrefab != null)
            {
                instance.SetActive(false);

                if (!poolQueues.TryGetValue(tracker.SourcePrefab, out var queue))
                {
                    queue = new Queue<GameObject>(32);
                    poolQueues[tracker.SourcePrefab] = queue;
                }

                queue.Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (Instance != null)
            {
                return Instance.Spawn(prefab, position, rotation);
            }
            return Instantiate(prefab, position, rotation);
        }

        public static void DespawnObject(GameObject instance)
        {
            if (Instance != null)
            {
                Instance.Despawn(instance);
            }
            else
            {
                Destroy(instance);
            }
        }
    }
}
