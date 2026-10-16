using DwarvesTD.Core;
using DwarvesTD.Enemies;
using DwarvesTD.Projectiles;
using UnityEngine;

namespace DwarvesTD.Towers
{
    public enum TargetPriority
    {
        First,    // Goblin mais avanado no caminho (padro BTD6)
        Last,     // Goblin mais recuado no incio do caminho
        Strongest,// Goblin com mais vida
        Closest   // Goblin mais prximo fisicamente da torre
    }

    public class BaseDwarfTower : MonoBehaviour
    {
        [Header("Tower Identity")]
        [SerializeField] private string dwarfName = "Besteiro Ano";
        [SerializeField] private int cost = 120;

        [Header("Variable Stats (Customizvel por tipo de torre)")]
        [Tooltip("Raio de alcance da torre. Totalmente varivel.")]
        [SerializeField] private float attackRange = 5.0f;

        [Tooltip("Tempo em segundos entre cada disparo.")]
        [SerializeField] private float fireCooldown = 0.8f;

        [Tooltip("Dano infligido por cada disparo.")]
        [SerializeField] private int damage = 1;

        [Tooltip("Velocidade do projtil disparado.")]
        [SerializeField] private float projectileSpeed = 16f;

        [Header("Targeting")]
        [SerializeField] private TargetPriority priority = TargetPriority.First;

        [Header("References")]
        [SerializeField] private Transform rotatingPart; // Cabea/torreta que mira no goblin
        [SerializeField] private Transform firePoint;     // Ponto de sada do projtil
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject rangeIndicatorVisual;

        public float AttackRange => attackRange;
        public int Cost => cost;
        public string DwarfName => dwarfName;
        public TargetPriority Priority
        {
            get => priority;
            set => priority = value;
        }

        private float fireTimer = 0f;
        private float retargetTimer = 0f;
        private const float RETARGET_INTERVAL = 0.08f; // 12 checks por segundo em vez de 60+ (otimizao massiva)
        private BaseGoblin currentTarget;
        private float sqrAttackRange;

        private void Awake()
        {
            UpdateSqrRange();
        }

        private void OnValidate()
        {
            UpdateSqrRange();
        }

        private void UpdateSqrRange()
        {
            sqrAttackRange = attackRange * attackRange;
        }

        private void Update()
        {
            fireTimer -= Time.deltaTime;
            retargetTimer -= Time.deltaTime;

            // Se no h goblins ativos no jogo, economiza 100% de CPU
            if (BaseGoblin.ActiveGoblins.Count == 0)
            {
                currentTarget = null;
                return;
            }

            // Verifica se o alvo atual ainda  vlido
            bool isCurrentTargetValid = currentTarget != null &&
                                       currentTarget.gameObject.activeInHierarchy &&
                                       currentTarget.CurrentHealth > 0 &&
                                       (currentTarget.transform.position - transform.position).sqrMagnitude <= sqrAttackRange;

            // Recalcula o alvo apenas periodicamente ou se o atual for invlido
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
                    fireTimer = fireCooldown;
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
            int count = goblins.Count;

            for (int i = 0; i < count; i++)
            {
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
                projectile.Initialize(target, damage, projectileSpeed);
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
