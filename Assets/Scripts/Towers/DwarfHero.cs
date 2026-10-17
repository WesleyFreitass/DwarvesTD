using System;
using System.Collections.Generic;
using DwarvesTD.Core;
using DwarvesTD.Enemies;
using DwarvesTD.Projectiles;
using UnityEngine;

namespace DwarvesTD.Towers
{
    public class DwarfHero : MonoBehaviour
    {
        public static DwarfHero Instance { get; private set; }

        [Header("Hero Identity")]
        [SerializeField] private string heroName = "Comandante Thorin";
        [SerializeField] private int cost = 150;

        [Header("References")]
        [SerializeField] private Transform rotatingPart;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject hammerProjectilePrefab;
        [SerializeField] private GameObject rangeIndicatorVisual;
        [SerializeField] private GameObject warcryFXPrefab;

        [Header("Targeting")]
        [SerializeField] private TargetPriority priority = TargetPriority.First;

        public string HeroName => heroName;
        public int Cost => cost;
        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; } = 0;
        public int MaxLevel => 10;

        public float AttackRange => currentAttackRange;
        public bool IsAbilityUnlocked => CurrentLevel >= 3;
        public bool IsAbilityReady => IsAbilityUnlocked && abilityCooldownTimer <= 0f;
        public float AbilityCooldownRemaining => Mathf.Max(0f, abilityCooldownTimer);
        public float AbilityCooldownDuration => GetAbilityCooldown();
        public bool IsWarcryActive => warcryActiveTimer > 0f;
        public float WarcryActiveRemaining => Mathf.Max(0f, warcryActiveTimer);

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPChanged;
        public event Action OnWarcryTriggered;

        private float currentAttackRange = 5.0f;
        private float currentFireCooldown = 0.9f;
        private int currentDamage = 2;
        private int currentPierce = 1;
        private float currentStunDuration = 0f;

        private float fireTimer = 0f;
        private float retargetTimer = 0f;
        private const float RETARGET_INTERVAL = 0.08f;
        private BaseGoblin currentTarget;
        private float sqrAttackRange;

        private float abilityCooldownTimer = 0f;
        private float warcryActiveTimer = 0f;
        private float passiveAuraCheckTimer = 0f;

        private static readonly int[] XP_THRESHOLDS = new int[]
        {
            0,     // Nivel 1
            120,   // Nivel 2
            260,   // Nivel 3 (Desbloqueia Grito de Guerra)
            450,   // Nivel 4 (Pierce 2)
            700,   // Nivel 5 (Dano 4)
            1000,  // Nivel 6 (Grito de Guerra +1 Dano)
            1400,  // Nivel 7 (Alcance 6.5m, Cooldown 0.7s)
            1900,  // Nivel 8 (Aura Passiva +10% Alcance vizinho)
            2500,  // Nivel 9 (Dano 6, Pierce 3)
            3200   // Nivel 10 (Lorde da Montanha: Dano 8, Stun, Cooldown 20s)
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RecalculateLevelStats();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundRewardGranted += HandleRoundReward;
            }
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundRewardGranted -= HandleRoundReward;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundRewardGranted -= HandleRoundReward;
                GameManager.Instance.OnRoundRewardGranted += HandleRoundReward;
            }
            OnXPChanged?.Invoke(CurrentXP, GetRequiredXPForNextLevel());
        }

        private void HandleRoundReward(int round, int bonus)
        {
            // O heroi ganha 120 XP ao completar cada rodada
            AddXP(120);
        }

        public void AddXP(int amount)
        {
            if (CurrentLevel >= MaxLevel) return;

            CurrentXP += amount;
            while (CurrentLevel < MaxLevel && CurrentXP >= GetRequiredXPForNextLevel())
            {
                CurrentLevel++;
                RecalculateLevelStats();
                OnLevelUp?.Invoke(CurrentLevel);
            }

            OnXPChanged?.Invoke(CurrentXP, GetRequiredXPForNextLevel());
        }

        public int GetRequiredXPForNextLevel()
        {
            if (CurrentLevel >= MaxLevel) return XP_THRESHOLDS[MaxLevel - 1];
            return XP_THRESHOLDS[CurrentLevel];
        }

        private float GetAbilityCooldown()
        {
            return (CurrentLevel >= 10) ? 20f : 30f;
        }

        private void RecalculateLevelStats()
        {
            // Nivel 1 Base
            currentAttackRange = 5.0f;
            currentFireCooldown = 0.9f;
            currentDamage = 2;
            currentPierce = 1;
            currentStunDuration = 0f;

            if (CurrentLevel >= 2) currentAttackRange = 5.8f;
            if (CurrentLevel >= 4) currentPierce = 2;
            if (CurrentLevel >= 5) currentDamage = 4;
            if (CurrentLevel >= 7)
            {
                currentAttackRange = 6.5f;
                currentFireCooldown = 0.7f;
            }
            if (CurrentLevel >= 9)
            {
                currentDamage = 6;
                currentPierce = 3;
            }
            if (CurrentLevel >= 10)
            {
                currentDamage = 8;
                currentStunDuration = 0.8f;
            }

            sqrAttackRange = currentAttackRange * currentAttackRange;
            UpdateRangeVisual();
        }

        private void Update()
        {
            fireTimer -= Time.deltaTime;
            retargetTimer -= Time.deltaTime;

            if (abilityCooldownTimer > 0f)
            {
                abilityCooldownTimer -= Time.deltaTime;
            }

            if (warcryActiveTimer > 0f)
            {
                warcryActiveTimer -= Time.deltaTime;
            }

            // Nivel 8+: Aura passiva de moral (+10% de alcance para torres vizinhas em 5m)
            if (CurrentLevel >= 8)
            {
                passiveAuraCheckTimer -= Time.deltaTime;
                if (passiveAuraCheckTimer <= 0f)
                {
                    ApplyPassiveAura();
                    passiveAuraCheckTimer = 1.0f;
                }
            }

            if (BaseGoblin.ActiveGoblins.Count == 0)
            {
                currentTarget = null;
                return;
            }

            bool isCurrentTargetValid = currentTarget != null &&
                                       currentTarget.gameObject.activeInHierarchy &&
                                       currentTarget.CurrentHealth > 0 &&
                                       (currentTarget.transform.position - transform.position).sqrMagnitude <= sqrAttackRange;

            if (!isCurrentTargetValid || retargetTimer <= 0f)
            {
                currentTarget = SelectTargetOptimized();
                retargetTimer = RETARGET_INTERVAL;
            }

            if (currentTarget != null)
            {
                AimAtTarget(currentTarget.transform.position);

                if (fireTimer <= 0f)
                {
                    ShootHammer(currentTarget);
                    fireTimer = currentFireCooldown;
                }
            }
        }

        private void ApplyPassiveAura()
        {
            float sqrAuraRadius = 5.5f * 5.5f;
            Vector3 heroPos = transform.position;
            var towers = BaseDwarfTower.ActiveTowers;
            int count = towers.Count;

            for (int i = 0; i < count; i++)
            {
                BaseDwarfTower tower = towers[i];
                if (tower == null) continue;

                Vector3 diff = tower.transform.position - heroPos;
                diff.y = 0;
                if (diff.sqrMagnitude <= sqrAuraRadius)
                {
                    tower.ApplyRangeBuff(0.10f, 1.2f);
                }
            }
        }

        public bool ActivateWarcry()
        {
            if (!IsAbilityReady) return false;

            abilityCooldownTimer = GetAbilityCooldown();
            warcryActiveTimer = 8f;

            // Encontra todas as torres aliadas num raio de 6.5 metros e aplica o super buff
            float sqrWarcryRadius = 6.5f * 6.5f;
            Vector3 heroPos = transform.position;
            var towers = BaseDwarfTower.ActiveTowers;
            int count = towers.Count;

            for (int i = 0; i < count; i++)
            {
                BaseDwarfTower tower = towers[i];
                if (tower == null) continue;

                Vector3 diff = tower.transform.position - heroPos;
                diff.y = 0;
                if (diff.sqrMagnitude <= sqrWarcryRadius)
                {
                    tower.ApplyAttackSpeedBuff(0.40f, 8f);
                    if (CurrentLevel >= 6)
                    {
                        tower.ApplyDamageBuff(1, 8f);
                    }
                }
            }

            // Spawn do VFX visual de onda de impacto dourada
            if (warcryFXPrefab != null)
            {
                PoolManager.SpawnObject(warcryFXPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            }

            OnWarcryTriggered?.Invoke();
            return true;
        }

        private BaseGoblin SelectTargetOptimized()
        {
            BaseGoblin bestTarget = null;
            float bestMetric = 0f;
            Vector3 heroPos = transform.position;

            var goblins = BaseGoblin.ActiveGoblins;

            for (int i = 0; i < goblins.Count; i++)
            {
                if (i >= goblins.Count) break;
                BaseGoblin goblin = goblins[i];
                if (goblin == null || !goblin.gameObject.activeInHierarchy || goblin.CurrentHealth <= 0) continue;

                Vector3 diff = goblin.transform.position - heroPos;
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist > sqrAttackRange) continue;

                if (bestTarget == null)
                {
                    bestTarget = goblin;
                    bestMetric = (goblin.Follower != null) ? goblin.Follower.DistanceTraveled : 0f;
                    continue;
                }

                float currentMetric = (goblin.Follower != null) ? goblin.Follower.DistanceTraveled : 0f;
                if (currentMetric > bestMetric)
                {
                    bestTarget = goblin;
                    bestMetric = currentMetric;
                }
            }

            return bestTarget;
        }

        private void AimAtTarget(Vector3 targetPos)
        {
            Transform aimTransform = rotatingPart != null ? rotatingPart : transform;
            Vector3 lookDirection = (targetPos - aimTransform.position);
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
                aimTransform.rotation = Quaternion.RotateTowards(aimTransform.rotation, targetRot, 720f * Time.deltaTime);
            }
        }

        private void ShootHammer(BaseGoblin target)
        {
            if (hammerProjectilePrefab == null) return;

            Vector3 spawnPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
            GameObject projObj = PoolManager.SpawnObject(hammerProjectilePrefab, spawnPoint, Quaternion.identity);

            BaseProjectile projectile = projObj.GetComponent<BaseProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, currentDamage, 17f);
                if (currentStunDuration > 0f)
                {
                    projectile.SetStunStats(currentStunDuration);
                }
            }

            // Heroi ganha 3 XP por golpe bem sucedido
            AddXP(3);
        }

        public void ShowRangeVisual(bool show)
        {
            if (rangeIndicatorVisual != null)
            {
                rangeIndicatorVisual.SetActive(show);
                UpdateRangeVisual();
            }
        }

        private void UpdateRangeVisual()
        {
            if (rangeIndicatorVisual != null)
            {
                rangeIndicatorVisual.transform.localScale = new Vector3(currentAttackRange * 2f, 0.05f, currentAttackRange * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.85f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, currentAttackRange);

            Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 6.5f); // Raio do Grito de Guerra
        }
    }
}
