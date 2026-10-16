using System;
using System.Collections.Generic;
using DwarvesTD.Core;
using DwarvesTD.Pathfinding;
using UnityEngine;

namespace DwarvesTD.Enemies
{
    [RequireComponent(typeof(PathFollower))]
    public class BaseGoblin : MonoBehaviour
    {
        public static readonly List<BaseGoblin> ActiveGoblins = new List<BaseGoblin>();

        private static MaterialPropertyBlock propBlock;
        private static readonly int baseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int colorProp = Shader.PropertyToID("_Color");

        [Header("Goblin Identity")]
        [SerializeField] private string goblinName = "Goblin Recruta";
        [SerializeField] private Color goblinColor = new Color(0.25f, 0.75f, 0.20f); // Cor distinta por tipo de goblin

        [Header("Variable Stats (Customizavel por tipo)")]
        [SerializeField] private int goldReward = 1;
        [SerializeField] private int damageToLives = 1;
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float moveSpeed = 3.5f;

        [Header("Armor & Special Traits")]
        [SerializeField] private bool isArmored = false;
        [SerializeField] private bool isStealth = false;

        [Header("Split On Death (Estilo MOAB do BTD6)")]
        [SerializeField] private bool splitsOnDeath = false;
        [SerializeField] private GameObject splitPrefab;
        [SerializeField] private int splitCount = 3;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public int GoldReward => goldReward;
        public int DamageToLives => damageToLives;
        public string GoblinName => goblinName;
        public bool IsArmored => isArmored;
        public bool IsStealth => isStealth;
        public Color GoblinColor => goblinColor;

        public PathFollower Follower { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        private Renderer meshRenderer;
        private float flashTimer = 0f;

        private void Awake()
        {
            Follower = GetComponent<PathFollower>();
            meshRenderer = GetComponentInChildren<Renderer>();

            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnEnable()
        {
            if (!ActiveGoblins.Contains(this))
            {
                ActiveGoblins.Add(this);
            }
            flashTimer = 0f;
            ApplyGoblinColor();
        }

        private void OnDisable()
        {
            ActiveGoblins.Remove(this);
            flashTimer = 0f;
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    ApplyGoblinColor();
                }
            }
        }

        public void Initialize(PathRoute path)
        {
            CurrentHealth = maxHealth;

            if (Follower == null) Follower = GetComponent<PathFollower>();
            Follower.OnReachedEnd -= HandleReachedEnd;
            Follower.OnReachedEnd += HandleReachedEnd;
            Follower.Initialize(path, moveSpeed);

            ApplyGoblinColor();
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void InitializeAtPoint(PathRoute path, Vector3 spawnPosition, int waypointIndex, float distance)
        {
            CurrentHealth = maxHealth;

            if (Follower == null) Follower = GetComponent<PathFollower>();
            Follower.OnReachedEnd -= HandleReachedEnd;
            Follower.OnReachedEnd += HandleReachedEnd;
            Follower.InitializeAtPoint(path, moveSpeed, spawnPosition, waypointIndex, distance);

            ApplyGoblinColor();
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(int damage, bool ignoreArmor = false)
        {
            if (CurrentHealth <= 0) return;

            int finalDamage = damage;
            if (isArmored && !ignoreArmor && finalDamage > 1)
            {
                finalDamage = Mathf.Max(1, finalDamage - 1);
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            TriggerFlash();

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(goldReward);
            }

            if (splitsOnDeath && splitPrefab != null && Follower != null && Follower.CurrentPath != null)
            {
                SpawnSplitChildren();
            }

            OnDied?.Invoke();
            DespawnSelf();
        }

        private void SpawnSplitChildren()
        {
            PathRoute path = Follower.CurrentPath;
            int wpIndex = Follower.CurrentWaypointIndex;
            float dist = Follower.DistanceTraveled;
            Vector3 basePos = transform.position;

            for (int i = 0; i < splitCount; i++)
            {
                Vector3 offset = (UnityEngine.Random.insideUnitSphere * 0.45f);
                offset.y = 0;
                Vector3 childPos = basePos + offset;

                GameObject childObj = PoolManager.SpawnObject(splitPrefab, childPos, Quaternion.identity);
                BaseGoblin childGoblin = childObj.GetComponent<BaseGoblin>();
                if (childGoblin != null)
                {
                    childGoblin.InitializeAtPoint(path, childPos, wpIndex, Mathf.Max(0, dist - (i * 0.5f)));
                }
            }
        }

        private void HandleReachedEnd()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage(damageToLives);
            }

            DespawnSelf();
        }

        private void DespawnSelf()
        {
            Follower.OnReachedEnd -= HandleReachedEnd;
            PoolManager.DespawnObject(gameObject);
        }

        private void TriggerFlash()
        {
            if (meshRenderer != null)
            {
                flashTimer = 0.08f;
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(baseColorProp, Color.white);
                propBlock.SetColor(colorProp, Color.white);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }

        public void ApplyGoblinColor()
        {
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();

            if (meshRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();

                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(baseColorProp, goblinColor);
                propBlock.SetColor(colorProp, goblinColor);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }

        public void SetStats(int health, float speed, int reward, int livesDamage, Color color, bool armored = false, bool stealth = false)
        {
            maxHealth = health;
            CurrentHealth = health;
            moveSpeed = speed;
            goldReward = reward;
            damageToLives = livesDamage;
            goblinColor = color;
            isArmored = armored;
            isStealth = stealth;

            if (Follower != null)
            {
                Follower.Speed = speed;
            }

            ApplyGoblinColor();
        }

        public void SetSplitOnDeath(GameObject childPrefab, int count)
        {
            splitsOnDeath = (childPrefab != null && count > 0);
            splitPrefab = childPrefab;
            splitCount = count;
        }
    }
}
