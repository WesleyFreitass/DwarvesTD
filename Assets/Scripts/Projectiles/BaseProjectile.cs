using DwarvesTD.Core;
using DwarvesTD.Enemies;
using UnityEngine;

namespace DwarvesTD.Projectiles
{
    public class BaseProjectile : MonoBehaviour
    {
        [Header("Projectile Stats")]
        [SerializeField] private float speed = 16f;
        [SerializeField] private int damage = 1;
        [SerializeField] private int pierce = 1;
        [SerializeField] private float maxLifetime = 3f;

        [Header("Explosion / Area of Effect (AoE)")]
        [SerializeField] private bool isExplosive = false;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private bool ignoreArmor = false;
        [SerializeField] private GameObject explosionPrefab;

        private BaseGoblin targetGoblin;
        private Vector3 lastTargetPos;
        private int remainingPierce;
        private float lifeTimer = 0f;

        private void Awake()
        {
            // Garante Rigidbody kinematic para evitar que PhysX recalcule a rvore esttica a cada frame
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public void Initialize(BaseGoblin target, int damageAmount, float projSpeed = 16f)
        {
            targetGoblin = target;
            damage = damageAmount;
            speed = projSpeed;
            remainingPierce = pierce;
            lifeTimer = 0f;

            if (target != null)
            {
                lastTargetPos = target.transform.position;
            }
        }

        public void SetExplosiveStats(bool explosive, float radius, bool bypassArmor, GameObject vfx = null)
        {
            isExplosive = explosive;
            explosionRadius = radius;
            ignoreArmor = bypassArmor;
            explosionPrefab = vfx;
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime)
            {
                DespawnSelf();
                return;
            }

            Vector3 targetPosition = (targetGoblin != null && targetGoblin.gameObject.activeInHierarchy)
                ? targetGoblin.transform.position
                : lastTargetPos;

            Vector3 currentPosition = transform.position;
            Vector3 direction = (targetPosition - currentPosition);
            float stepDistance = speed * Time.deltaTime;

            // Clculo de coliso puramente matemtico (ultra leve, 0 impacto na fsica)
            if (direction.sqrMagnitude <= stepDistance * stepDistance)
            {
                transform.position = targetPosition;
                OnHitTarget();
            }
            else
            {
                transform.position += direction.normalized * stepDistance;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }

        private void OnHitTarget()
        {
            if (isExplosive)
            {
                ExplodeAtPosition(transform.position);
            }
            else
            {
                if (targetGoblin != null && targetGoblin.gameObject.activeInHierarchy)
                {
                    HitGoblin(targetGoblin);
                }
                else
                {
                    DespawnSelf();
                }
            }
        }

        private void ExplodeAtPosition(Vector3 explosionPos)
        {
            if (explosionPrefab != null)
            {
                PoolManager.SpawnObject(explosionPrefab, explosionPos + Vector3.up * 0.2f, Quaternion.identity);
            }

            float sqrRadius = explosionRadius * explosionRadius;
            var goblins = BaseGoblin.ActiveGoblins;
            int count = goblins.Count;

            for (int i = 0; i < count; i++)
            {
                BaseGoblin goblin = goblins[i];
                if (goblin == null || !goblin.gameObject.activeInHierarchy || goblin.CurrentHealth <= 0) continue;

                Vector3 diff = goblin.transform.position - explosionPos;
                diff.y = 0;
                if (diff.sqrMagnitude <= sqrRadius)
                {
                    goblin.TakeDamage(damage, ignoreArmor);
                }
            }

            DespawnSelf();
        }

        private void HitGoblin(BaseGoblin goblin)
        {
            goblin.TakeDamage(damage);
            remainingPierce--;

            if (remainingPierce <= 0)
            {
                DespawnSelf();
            }
        }

        private void DespawnSelf()
        {
            targetGoblin = null;
            PoolManager.DespawnObject(gameObject);
        }
    }
}
