using DwarvesTD.Core;
using DwarvesTD.Enemies;
using UnityEngine;

namespace DwarvesTD.Towers
{
    public class GoldMineTower : MonoBehaviour
    {
        [Header("Mine Identity")]
        [SerializeField] private string towerName = "Mina de Ouro dos Anoes";
        [SerializeField] private int cost = 400;
        [SerializeField] private int roundIncome = 80;

        [Header("Visuals & FX")]
        [SerializeField] private GameObject rangeIndicatorVisual;
        [SerializeField] private GameObject goldNuggetVisual;

        public string TowerName => towerName;
        public int Cost => cost;
        public int RoundIncome => roundIncome;
        public int TotalGoldGenerated { get; private set; } = 0;

        private float nuggetPulseTimer = 0f;
        private Vector3 initialNuggetScale = Vector3.one;

        private void Awake()
        {
            if (goldNuggetVisual != null)
            {
                initialNuggetScale = goldNuggetVisual.transform.localScale;
            }
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundRewardGranted -= HandleRoundReward;
            }
        }

        private void Start()
        {
            // Caso tenha sido instanciado apos Awake do GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundRewardGranted -= HandleRoundReward;
                GameManager.Instance.OnRoundRewardGranted += HandleRoundReward;
            }
        }

        private void Update()
        {
            // Pulso animado da pepita de ouro quando produz receita
            if (nuggetPulseTimer > 0f)
            {
                nuggetPulseTimer -= Time.deltaTime;
                if (goldNuggetVisual != null)
                {
                    float scaleFactor = 1f + Mathf.Sin(nuggetPulseTimer * Mathf.PI * 4f) * 0.35f;
                    goldNuggetVisual.transform.localScale = initialNuggetScale * scaleFactor;

                    if (nuggetPulseTimer <= 0f)
                    {
                        goldNuggetVisual.transform.localScale = initialNuggetScale;
                    }
                }
            }
        }

        private void HandleRoundReward(int roundIndex, int regularBonus)
        {
            ProduceGold();
        }

        public void ProduceGold()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(roundIncome);
                TotalGoldGenerated += roundIncome;
                nuggetPulseTimer = 1.0f;
            }
        }

        public void ShowRangeVisual(bool show)
        {
            if (rangeIndicatorVisual != null)
            {
                rangeIndicatorVisual.SetActive(show);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.84f, 0.0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 2.5f);
        }
    }
}
