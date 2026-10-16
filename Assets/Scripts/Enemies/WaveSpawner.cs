using System;
using System.Collections;
using System.Collections.Generic;
using DwarvesTD.Core;
using DwarvesTD.Pathfinding;
using UnityEngine;

namespace DwarvesTD.Enemies
{
    [System.Serializable]
    public class WaveSpawnGroup
    {
        public GameObject enemyPrefab;
        public int count = 5;
        public float interval = 0.8f;
        public float delayBeforeStart = 0f;
    }

    [System.Serializable]
    public class WaveConfig
    {
        public string waveTitle = "Rodada";
        public List<WaveSpawnGroup> groups = new List<WaveSpawnGroup>();
    }

    public class WaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PathRoute pathRoute;

        [Header("Enemy Prefab Roster")]
        [SerializeField] private GameObject recruitPrefab;
        [SerializeField] private GameObject runnerPrefab;
        [SerializeField] private GameObject armoredPrefab;
        [SerializeField] private GameObject trollBossPrefab;

        [Header("Curated Waves")]
        [SerializeField] private List<WaveConfig> customWaves = new List<WaveConfig>();

        public int CurrentWave { get; private set; } = 0;
        public bool IsWaveInProgress { get; private set; } = false;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void Start()
        {
            EnsureDependencies();
        }

        private static GameObject proceduralTemplateHolder;

        /// <summary>
        /// Garante que nenhuma referência fique nula, carregando do Resources ou gerando templates procedurais caso necessário
        /// </summary>
        public void EnsureDependencies()
        {
            if (pathRoute == null)
            {
                pathRoute = FindAnyObjectByType<PathRoute>();
            }

            if (recruitPrefab == null) recruitPrefab = Resources.Load<GameObject>("Prefabs/GoblinRecruit");
            if (runnerPrefab == null) runnerPrefab = Resources.Load<GameObject>("Prefabs/GoblinRunner");
            if (armoredPrefab == null) armoredPrefab = Resources.Load<GameObject>("Prefabs/GoblinArmored");
            if (trollBossPrefab == null) trollBossPrefab = Resources.Load<GameObject>("Prefabs/TrollBoss");

            if (recruitPrefab == null) recruitPrefab = Resources.Load<GameObject>("Prefabs/GoblinScout");

            // Se ainda assim algum prefab for nulo, gera dinamicamente em tempo de execucao (Fallback a prova de falhas)
            if (recruitPrefab == null)
            {
                recruitPrefab = CreateProceduralGoblinTemplate(
                    "GoblinRecruit",
                    PrimitiveType.Capsule,
                    new Vector3(0.6f, 0.6f, 0.6f),
                    new Color(0.20f, 0.85f, 0.20f),
                    health: 3,
                    speed: 3.5f,
                    reward: 1,
                    livesDamage: 1,
                    armored: false
                );
            }

            if (runnerPrefab == null)
            {
                runnerPrefab = CreateProceduralGoblinTemplate(
                    "GoblinRunner",
                    PrimitiveType.Capsule,
                    new Vector3(0.45f, 0.45f, 0.45f),
                    new Color(1.0f, 0.88f, 0.05f),
                    health: 2,
                    speed: 5.5f,
                    reward: 1,
                    livesDamage: 1,
                    armored: false
                );
            }

            if (armoredPrefab == null)
            {
                armoredPrefab = CreateProceduralGoblinTemplate(
                    "GoblinArmored",
                    PrimitiveType.Cube,
                    new Vector3(0.8f, 0.8f, 0.8f),
                    new Color(0.35f, 0.45f, 0.55f),
                    health: 10,
                    speed: 2.2f,
                    reward: 4,
                    livesDamage: 2,
                    armored: true
                );
            }

            if (trollBossPrefab == null)
            {
                trollBossPrefab = CreateProceduralGoblinTemplate(
                    "TrollBoss",
                    PrimitiveType.Cylinder,
                    new Vector3(1.3f, 1.2f, 1.3f),
                    new Color(0.55f, 0.25f, 0.12f),
                    health: 35,
                    speed: 1.5f,
                    reward: 25,
                    livesDamage: 10,
                    armored: false,
                    splitChild: recruitPrefab,
                    splitCount: 3
                );
            }
        }

        private GameObject CreateProceduralGoblinTemplate(
            string templateName,
            PrimitiveType primitive,
            Vector3 scale,
            Color color,
            int health,
            float speed,
            int reward,
            int livesDamage,
            bool armored,
            GameObject splitChild = null,
            int splitCount = 0)
        {
            if (proceduralTemplateHolder == null)
            {
                proceduralTemplateHolder = new GameObject("[ProceduralGoblinsTemplates]");
                proceduralTemplateHolder.SetActive(false);
                DontDestroyOnLoad(proceduralTemplateHolder);
            }

            GameObject root = new GameObject(templateName);
            root.transform.SetParent(proceduralTemplateHolder.transform);

            GameObject body = GameObject.CreatePrimitive(primitive);
            body.name = "VisualBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, scale.y * 0.5f, 0);
            body.transform.localScale = scale;

            Collider bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) DestroyImmediate(bodyCol);

            Renderer rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Material mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                mat.enableInstancing = true;
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = Mathf.Max(scale.x, scale.z) * 0.7f;
            col.center = new Vector3(0, scale.y * 0.5f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PathFollower>();
            BaseGoblin goblin = root.AddComponent<BaseGoblin>();
            goblin.SetStats(health, speed, reward, livesDamage, color, armored);
            if (splitChild != null && splitCount > 0)
            {
                goblin.SetSplitOnDeath(splitChild, splitCount);
            }

            root.SetActive(false);
            return root;
        }

        public void SetPath(PathRoute path)
        {
            pathRoute = path;
        }

        public void SetEnemyPrefabs(GameObject recruit, GameObject runner, GameObject armored, GameObject troll)
        {
            recruitPrefab = recruit;
            runnerPrefab = runner;
            armoredPrefab = armored;
            trollBossPrefab = troll;
        }

        public void StartNextWave()
        {
            if (IsWaveInProgress) return;

            EnsureDependencies();
            CurrentWave++;
            StartCoroutine(ExecuteWaveRoutine(CurrentWave));
        }

        private IEnumerator ExecuteWaveRoutine(int waveIndex)
        {
            IsWaveInProgress = true;
            OnWaveStarted?.Invoke(waveIndex);

            WaveConfig config = GetWaveConfiguration(waveIndex);

            foreach (var group in config.groups)
            {
                if (group.delayBeforeStart > 0f)
                {
                    yield return new WaitForSeconds(group.delayBeforeStart);
                }

                GameObject prefabToSpawn = group.enemyPrefab != null ? group.enemyPrefab : recruitPrefab;
                if (prefabToSpawn == null)
                {
                    EnsureDependencies();
                    prefabToSpawn = recruitPrefab;
                }

                if (prefabToSpawn != null)
                {
                    for (int i = 0; i < group.count; i++)
                    {
                        SpawnSingleEnemy(prefabToSpawn);
                        yield return new WaitForSeconds(group.interval);
                    }
                }
                else
                {
                    Debug.LogError("[WaveSpawner] Falha critica: nenhum prefab disponivel!");
                }
            }

            // Aguarda todos os goblins e divisoes serem eliminados
            while (BaseGoblin.ActiveGoblins.Count > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            IsWaveInProgress = false;
            OnWaveCompleted?.Invoke(waveIndex);
            Debug.Log($"[WaveSpawner] Rodada {waveIndex} concluida com sucesso!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteRound(waveIndex);
            }
        }

        private void SpawnSingleEnemy(GameObject prefab)
        {
            if (prefab == null)
            {
                EnsureDependencies();
                prefab = recruitPrefab;
            }

            if (prefab == null)
            {
                Debug.LogError("[WaveSpawner] SpawnSingleEnemy chamado com prefab nulo e sem fallback!");
                return;
            }

            if (pathRoute == null || pathRoute.WaypointCount == 0)
            {
                pathRoute = FindAnyObjectByType<PathRoute>();
                if (pathRoute == null || pathRoute.WaypointCount == 0)
                {
                    Debug.LogError("[WaveSpawner] Nao foi possivel encontrar o caminho EnemyPath na cena!");
                    return;
                }
            }

            Vector3 spawnPos = pathRoute.GetWaypoint(0).position;
            GameObject enemyObj = PoolManager.SpawnObject(prefab, spawnPos, Quaternion.identity);

            BaseGoblin goblin = enemyObj.GetComponent<BaseGoblin>();
            if (goblin != null)
            {
                goblin.Initialize(pathRoute);
            }
        }

        private WaveConfig GetWaveConfiguration(int wave)
        {
            if (customWaves != null && wave <= customWaves.Count && customWaves[wave - 1] != null && customWaves[wave - 1].groups.Count > 0)
            {
                bool hasValidPrefab = false;
                foreach (var g in customWaves[wave - 1].groups)
                {
                    if (g.enemyPrefab != null)
                    {
                        hasValidPrefab = true;
                        break;
                    }
                }
                if (hasValidPrefab)
                {
                    return customWaves[wave - 1];
                }
            }

            EnsureDependencies();

            GameObject rec = recruitPrefab;
            GameObject run = runnerPrefab != null ? runnerPrefab : rec;
            GameObject arm = armoredPrefab != null ? armoredPrefab : rec;
            GameObject trl = trollBossPrefab != null ? trollBossPrefab : rec;

            WaveConfig waveCfg = new WaveConfig();

            switch (wave)
            {
                case 1:
                    waveCfg.waveTitle = "Rodada 1: Primeiros Exploradores";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 10, interval = 1.0f });
                    break;

                case 2:
                    waveCfg.waveTitle = "Rodada 2: Marcha de Recrutas";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 16, interval = 0.75f });
                    break;

                case 3:
                    waveCfg.waveTitle = "Rodada 3: Chegada dos Corredores";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 8, interval = 0.8f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 6, interval = 0.5f, delayBeforeStart = 1.0f });
                    break;

                case 4:
                    waveCfg.waveTitle = "Rodada 4: Rajadas Velozes";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 14, interval = 0.45f });
                    break;

                case 5:
                    waveCfg.waveTitle = "Rodada 5: Primeira Armadura de Ferro";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 10, interval = 0.6f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = arm, count = 2, interval = 2.0f, delayBeforeStart = 1.5f });
                    break;

                case 6:
                    waveCfg.waveTitle = "Rodada 6: Horda Mista";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 14, interval = 0.5f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 10, interval = 0.4f, delayBeforeStart = 0.5f });
                    break;

                case 7:
                    waveCfg.waveTitle = "Rodada 7: Escolta de Ferro";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = arm, count = 4, interval = 1.6f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 12, interval = 0.4f, delayBeforeStart = 1.0f });
                    break;

                case 8:
                    waveCfg.waveTitle = "Rodada 8: Enxame Incontrolavel";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 24, interval = 0.3f });
                    break;

                case 9:
                    waveCfg.waveTitle = "Rodada 9: Falange de Aco";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = arm, count = 8, interval = 1.2f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = rec, count = 12, interval = 0.5f });
                    break;

                case 10:
                default:
                    waveCfg.waveTitle = "Rodada 10: O Troll da Caverna (Chefe)!";
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = trl, count = 1, interval = 1.0f });
                    waveCfg.groups.Add(new WaveSpawnGroup { enemyPrefab = run, count = 10, interval = 0.4f, delayBeforeStart = 3.0f });
                    break;
            }

            return waveCfg;
        }
    }
}
