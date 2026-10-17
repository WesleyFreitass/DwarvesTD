using System.Collections.Generic;
using DwarvesTD.Core;
using DwarvesTD.Enemies;
using DwarvesTD.Projectiles;
using UnityEngine;

namespace DwarvesTD.Towers
{
    public enum TargetPriority
    {
        First,    // Goblin mais avançado no caminho (padrão BTD6)
        Last,     // Goblin mais recuado no início do caminho
        Strongest,// Goblin com mais vida
        Closest   // Goblin mais próximo fisicamente da torre
    }

    public enum TowerCategory
    {
        Cla,      // Primárias: Besteiro, Guerreiro
        Cerco,    // Militares: Canhão, Balista
        Runas,    // Mágicas: Ferreiro Rúnico, Golem
        Apoio,    // Suporte: Mina de Ouro, Posto de Vigia
        Heroi     // Heróis Comandantes
    }

    public class BaseDwarfTower : MonoBehaviour
    {
        public static readonly List<BaseDwarfTower> ActiveTowers = new List<BaseDwarfTower>();

        [Header("Tower Identity")]
        [SerializeField] private string dwarfName = "Besteiro Anão";
        [SerializeField] private int cost = 120;
        [SerializeField] private TowerCategory category = TowerCategory.Cla;

        [Header("Variable Stats (Customizável por tipo de torre)")]
        [Tooltip("Raio de alcance da torre. Totalmente variável.")]
        [SerializeField] private float attackRange = 5.0f;

        [Tooltip("Tempo em segundos entre cada disparo.")]
        [SerializeField] private float fireCooldown = 0.8f;

        [Tooltip("Dano infligido por cada disparo.")]
        [SerializeField] private int damage = 1;

        [Tooltip("Velocidade do projétil disparado.")]
        [SerializeField] private float projectileSpeed = 16f;

        [Header("Targeting")]
        [SerializeField] private TargetPriority priority = TargetPriority.First;

        [Header("References")]
        [SerializeField] private Transform rotatingPart; // Cabeça/torreta que mira no goblin
        [SerializeField] private Transform firePoint;     // Ponto de saída do projétil
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject rangeIndicatorVisual;

        public float AttackRange => attackRange * rangeBuffMultiplier;
        public int Cost => cost;
        public string DwarfName => dwarfName;
        public TowerCategory Category => category;
        public TargetPriority Priority
        {
            get => priority;
            set => priority = value;
        }

        private float fireTimer = 0f;
        private float retargetTimer = 0f;
        private const float RETARGET_INTERVAL = 0.08f; // 12 checks por segundo em vez de 60+ (otimização massiva)
        private BaseGoblin currentTarget;
        private float sqrAttackRange;

        // Buffs temporários ou auras (ex: Grito de Guerra do Herói Comandante)
        private float attackSpeedBuffMultiplier = 1f;
        private float attackSpeedBuffTimer = 0f;
        private int bonusDamageBuff = 0;
        private float bonusDamageBuffTimer = 0f;
        private float rangeBuffMultiplier = 1f;
        private float rangeBuffTimer = 0f;

        private void Awake()
        {
            UpdateSqrRange();
        }

        private void OnEnable()
        {
            if (!ActiveTowers.Contains(this))
            {
                ActiveTowers.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveTowers.Remove(this);
        }

        private void OnValidate()
        {
            UpdateSqrRange();
        }

        private void UpdateSqrRange()
        {
            float effectiveRange = attackRange * rangeBuffMultiplier;
            sqrAttackRange = effectiveRange * effectiveRange;
        }

        public void ApplyAttackSpeedBuff(float bonusPercent, float duration)
        {
            attackSpeedBuffMultiplier = 1f + bonusPercent;
            attackSpeedBuffTimer = Mathf.Max(attackSpeedBuffTimer, duration);
        }

        public void ApplyDamageBuff(int extraDamage, float duration)
        {
            bonusDamageBuff = extraDamage;
            bonusDamageBuffTimer = Mathf.Max(bonusDamageBuffTimer, duration);
        }

        public void ApplyRangeBuff(float bonusRangeMultiplier, float duration)
        {
            rangeBuffMultiplier = 1f + bonusRangeMultiplier;
            rangeBuffTimer = Mathf.Max(rangeBuffTimer, duration);
            UpdateSqrRange();
            UpdateRangeVisual();
        }

        private void Update()
        {
            fireTimer -= Time.deltaTime;
            retargetTimer -= Time.deltaTime;

            if (attackSpeedBuffTimer > 0f)
            {
                attackSpeedBuffTimer -= Time.deltaTime;
                if (attackSpeedBuffTimer <= 0f)
                {
                    attackSpeedBuffMultiplier = 1f;
                }
            }

            if (bonusDamageBuffTimer > 0f)
            {
                bonusDamageBuffTimer -= Time.deltaTime;
                if (bonusDamageBuffTimer <= 0f)
                {
                    bonusDamageBuff = 0;
                }
            }

            if (rangeBuffTimer > 0f)
            {
                rangeBuffTimer -= Time.deltaTime;
                if (rangeBuffTimer <= 0f)
                {
                    rangeBuffMultiplier = 1f;
                    UpdateSqrRange();
                    UpdateRangeVisual();
                }
            }

            // Se não há goblins ativos no jogo, economiza 100% de CPU
            if (BaseGoblin.ActiveGoblins.Count == 0)
            {
                currentTarget = null;
                return;
            }

            // Verifica se o alvo atual ainda é válido
            bool isCurrentTargetValid = currentTarget != null &&
                                       currentTarget.gameObject.activeInHierarchy &&
                                       currentTarget.CurrentHealth > 0 &&
                                       (currentTarget.transform.position - transform.position).sqrMagnitude <= sqrAttackRange;

            // Recalcula o alvo apenas periodicamente ou se o atual for inválido
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
                    Shoot(currentTarget);
                    float effectiveCooldown = fireCooldown / Mathf.Max(0.2f, attackSpeedBuffMultiplier);
                    fireTimer = effectiveCooldown;
                }
            }
        }

        /// <summary>
        /// Seleo de alvo ultra otimizada com distncia ao quadrado (sem razes quadradas pesadas)
        /// </summary>
        private BaseGoblin SelectTargetOptimized()
        {
            BaseGoblin bestTarget = null;
            float bestMetric = 0f;
            Vector3 towerPos = transform.position;

            var goblins = BaseGoblin.ActiveGoblins;

            for (int i = 0; i < goblins.Count; i++)
            {
                if (i >= goblins.Count) break;
                BaseGoblin goblin = goblins[i];
                if (goblin == null || !goblin.gameObject.activeInHierarchy || goblin.CurrentHealth <= 0) continue;

                // Usa sqrMagnitude: ~4x mais rpido que Vector3.Distance
                Vector3 diff = goblin.transform.position - towerPos;
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist > sqrAttackRange) continue;

                if (bestTarget == null)
                {
                    bestTarget = goblin;
                    bestMetric = GetMetric(goblin, sqrDist);
                    continue;
                }

                float currentMetric = GetMetric(goblin, sqrDist);
                if (IsBetterTarget(currentMetric, bestMetric))
                {
                    bestTarget = goblin;
                    bestMetric = currentMetric;
                }
            }

            return bestTarget;
        }

        private float GetMetric(BaseGoblin goblin, float sqrDistToTower)
        {
            switch (priority)
            {
                case TargetPriority.First:
                    return goblin.Follower != null ? goblin.Follower.DistanceTraveled : 0f;

                case TargetPriority.Last:
                    return goblin.Follower != null ? goblin.Follower.DistanceTraveled : 0f;

                case TargetPriority.Strongest:
                    return goblin.CurrentHealth;

                case TargetPriority.Closest:
                    return sqrDistToTower;

                default:
                    return 0f;
            }
        }

        private bool IsBetterTarget(float currentMetric, float bestMetric)
        {
            switch (priority)
            {
                case TargetPriority.First:
                case TargetPriority.Strongest:
                    return currentMetric > bestMetric;

                case TargetPriority.Last:
                case TargetPriority.Closest:
                    return currentMetric < bestMetric;

                default:
                    return false;
            }
        }

        private void AimAtTarget(Vector3 targetPos)
        {
            Transform aimTransform = rotatingPart != null ? rotatingPart : transform;
            Vector3 lookDirection = (targetPos - aimTransform.position);
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                // Rotao suave mirando no goblin
                Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
                aimTransform.rotation = Quaternion.RotateTowards(aimTransform.rotation, targetRot, 720f * Time.deltaTime);
            }
        }

        private void Shoot(BaseGoblin target)
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPoint = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;

            // Recicla projteis do PoolManager com Zero Garbage Collection
            GameObject projObj = PoolManager.SpawnObject(projectilePrefab, spawnPoint, Quaternion.identity);

            BaseProjectile projectile = projObj.GetComponent<BaseProjectile>();
            if (projectile != null)
            {
                int effectiveDamage = Mathf.Max(1, damage + bonusDamageBuff);
                projectile.Initialize(target, effectiveDamage, projectileSpeed);
            }
        }

        public void SetRange(float newRange)
        {
            attackRange = newRange;
            UpdateSqrRange();
            UpdateRangeVisual();
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
                rangeIndicatorVisual.transform.localScale = new Vector3(attackRange * 2f, 0.05f, attackRange * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
