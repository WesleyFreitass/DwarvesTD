using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DwarvesTD.Core
{
    public enum GameState
    {
        Playing,
        Victory,
        Defeat
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Starting Stats")]
        [SerializeField] private int startingLives = 100;
        [SerializeField] private int startingGold = 650;
        [SerializeField] private int maxWaves = 10; // Rodadas para vencer esta fase

        public int CurrentLives { get; private set; }
        public int CurrentGold { get; private set; }
        public int MaxWaves => maxWaves;
        public GameState CurrentState { get; private set; } = GameState.Playing;

        public event Action<int> OnLivesChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnRoundRewardGranted; // round, reward
        public event Action OnVictory;
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentLives = startingLives;
            CurrentGold = startingGold;
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;

            // Garante taxa de quadros alta e estavel sem travamentos de sincronizacao
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
        }

        private void Start()
        {
            OnLivesChanged?.Invoke(CurrentLives);
            OnGoldChanged?.Invoke(CurrentGold);
        }

        public void AddGold(int amount)
        {
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }

        public bool SpendGold(int amount)
        {
            if (CurrentGold >= amount)
            {
                CurrentGold -= amount;
                OnGoldChanged?.Invoke(CurrentGold);
                return true;
            }
            return false;
        }

        public bool CanAfford(int amount)
        {
            return CurrentGold >= amount;
        }

        public void TakeDamage(int damage)
        {
            if (CurrentState != GameState.Playing) return;

            CurrentLives = Mathf.Max(0, CurrentLives - damage);
            OnLivesChanged?.Invoke(CurrentLives);

            if (CurrentLives <= 0)
            {
                CurrentState = GameState.Defeat;
                Time.timeScale = 0f; // Pausa o jogo na derrota
                Debug.LogWarning("[GameManager] A fortaleza dos anes caiu! Game Over.");
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Chamado pelo WaveSpawner ao trmino de cada rodada
        /// </summary>
        public void CompleteRound(int roundIndex)
        {
            if (CurrentState != GameState.Playing) return;

            // Bnus de concluso de rodada estilo Bloons TD: 100 + rodada
            int roundBonus = 100 + (roundIndex * 5);
            AddGold(roundBonus);
            OnRoundRewardGranted?.Invoke(roundIndex, roundBonus);

            if (roundIndex >= maxWaves)
            {
                CurrentState = GameState.Victory;
                Time.timeScale = 0f; // Pausa o jogo na vitria
                Debug.Log("[GameManager] Vitria! Todos os goblins foram repelidos!");
                OnVictory?.Invoke();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
