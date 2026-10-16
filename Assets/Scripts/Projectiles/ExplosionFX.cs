using DwarvesTD.Core;
using UnityEngine;

namespace DwarvesTD.Projectiles
{
    public class ExplosionFX : MonoBehaviour
    {
        [SerializeField] private float duration = 0.18f;
        [SerializeField] private float maxRadius = 2.5f;

        private float timer = 0f;

        private void OnEnable()
        {
            timer = 0f;
            transform.localScale = Vector3.one * 0.4f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            float currentScale = Mathf.Lerp(0.4f, maxRadius * 2f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            transform.localScale = new Vector3(currentScale, currentScale * 0.4f, currentScale);

            if (timer >= duration)
            {
                PoolManager.DespawnObject(gameObject);
            }
        }
    }
}
