using DwarvesTD.Core;
using DwarvesTD.Enemies;
using DwarvesTD.Towers;
using UnityEngine;

namespace DwarvesTD.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private TowerPlacementManager placementManager;

        [Header("Tower Prefabs")]
        [SerializeField] private GameObject besteiroTowerPrefab;
        [SerializeField] private GameObject cannonTowerPrefab;

        private float gameSpeed = 1f;
        private string statusMessage = "";
        private float statusTimer = 0f;

        private int selectedCategoryTab = 0;
        private readonly string[] categoryNames = new string[] { "Cla", "Cerco", "Runas", "Apoio" };

        // Strings cacheadas para eliminar 100% das alocacoes de Garbage Collection no OnGUI
        private string cachedLivesLabel = "<size=18><b>Vidas: 100</b></size>";
        private string cachedGoldLabel = "<size=18><color=#FFD700><b>Ouro: 650 G</b></color></size>";
        private string cachedWaveLabel = "<size=16>Aguardando Inicio</size>";
        private string cachedFPSLabel = "<size=16><b><color=#00FF66>60 FPS</color></b></size>";
        private string cachedStatusLabel = "";

        private void Start()
        {
            if (waveSpawner == null)
            {
                waveSpawner = FindAnyObjectByType<WaveSpawner>();
            }

            if (placementManager == null)
            {
                placementManager = FindAnyObjectByType<TowerPlacementManager>();
            }

            if (besteiroTowerPrefab == null) besteiroTowerPrefab = Resources.Load<GameObject>("Prefabs/DwarfTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Prefabs/CannonTower");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLivesChanged += UpdateLivesCache;
                GameManager.Instance.OnGoldChanged += UpdateGoldCache;
                GameManager.Instance.OnRoundRewardGranted += HandleRoundReward;

                UpdateLivesCache(GameManager.Instance.CurrentLives);
                UpdateGoldCache(GameManager.Instance.CurrentGold);
            }

            if (waveSpawner != null)
            {
                waveSpawner.OnWaveStarted += UpdateWaveCache;
            }
        }

        private void UpdateLivesCache(int lives)
        {
            cachedLivesLabel = $"<size=18><b>Vidas: {lives}</b></size>";
        }

        private void UpdateGoldCache(int gold)
        {
            cachedGoldLabel = $"<size=18><color=#FFD700><b>Ouro: {gold} G</b></color></size>";
        }

        private void UpdateWaveCache(int wave)
        {
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveLabel = $"<size=16>Rodada: <b>{wave} / {max}</b></size>";
        }

        private float fpsTimeAccumulator = 0f;
        private int fpsFrameCount = 0;
        private int currentFPS = 60;

        private void Update()
        {
            // Calculo de FPS em tempo real
            fpsTimeAccumulator += Time.unscaledDeltaTime;
            fpsFrameCount++;
            if (fpsTimeAccumulator >= 0.35f)
            {
                currentFPS = Mathf.RoundToInt(fpsFrameCount / fpsTimeAccumulator);
                string fpsColor = currentFPS >= 55 ? "#00FF66" : (currentFPS >= 30 ? "#FFCC00" : "#FF3333");
                cachedFPSLabel = $"<size=16><b><color={fpsColor}>{currentFPS} FPS</color></b></size>";
                fpsTimeAccumulator = 0f;
                fpsFrameCount = 0;
            }

            if (statusTimer > 0f)
            {
                statusTimer -= Time.unscaledDeltaTime;
                if (statusTimer <= 0f)
                {
                    statusMessage = "";
                    cachedStatusLabel = "";
                }
            }
        }

        private void HandleRoundReward(int round, int bonus)
        {
            statusMessage = $"+ {bonus} Ouro bonus da Rodada {round}!";
            cachedStatusLabel = $"<size=15><b><color=#FFD700>+ {bonus} Ouro bonus da Rodada {round}!</color></b></size>";
            statusTimer = 3.5f;
        }

        public void StartNextWave()
        {
            if (waveSpawner != null)
            {
                waveSpawner.StartNextWave();
            }
        }

        public void ToggleGameSpeed()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            gameSpeed = (gameSpeed == 1f) ? 2f : 1f;
            Time.timeScale = gameSpeed;
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnGUI()
        {
            bool waveInProgress = waveSpawner != null && waveSpawner.IsWaveInProgress;
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Playing;

            float screenW = Screen.width;
            float screenH = Screen.height;

            // 1. BARRA SUPERIOR
            float topBarHeight = 52f;
            GUI.Box(new Rect(10, 10, screenW - 200, topBarHeight), "");

            GUI.Label(new Rect(25, 16, 140, 35), cachedLivesLabel);
            GUI.Label(new Rect(165, 16, 170, 35), cachedGoldLabel);
            GUI.Label(new Rect(340, 16, 220, 35), cachedWaveLabel);

            if (!string.IsNullOrEmpty(cachedStatusLabel))
            {
                GUI.Label(new Rect(560, 16, 300, 35), cachedStatusLabel);
            }

            float controlX = screenW - 390;

            GUI.Label(new Rect(controlX - 110, 18, 105, 35), cachedFPSLabel);

            if (GUI.Button(new Rect(controlX, 15, 90, 42), $"Vel: {gameSpeed}x"))
            {
                ToggleGameSpeed();
            }

            GUI.enabled = !waveInProgress && state == GameState.Playing;
            string playBtnText = (waveSpawner != null && waveSpawner.CurrentWave == 0) ? "Iniciar" : (waveInProgress ? "Lutando..." : "Proxima");
            if (GUI.Button(new Rect(controlX + 95, 15, 90, 42), $"<b>{playBtnText}</b>"))
            {
                StartNextWave();
            }
            GUI.enabled = true;

            // 2. PAINEL LATERAL DIREITO
            float drawerW = 185f;
            float drawerX = screenW - drawerW - 10f;
            float drawerY = 70f;
            float drawerH = screenH - 85f;

            GUI.Box(new Rect(drawerX, drawerY, drawerW, drawerH), "<b>OFICINA DOS ANOES</b>");

            float tabW = (drawerW - 10) / 4f;
            for (int i = 0; i < categoryNames.Length; i++)
            {
                bool isSelected = (selectedCategoryTab == i);
                if (GUI.Toggle(new Rect(drawerX + 5 + i * tabW, drawerY + 30, tabW, 26), isSelected, categoryNames[i], "Button"))
                {
                    selectedCategoryTab = i;
                }
            }

            float cardY = drawerY + 65;

            if (selectedCategoryTab == 0)
            {
                bool canAffordBesteiro = (GameManager.Instance != null && GameManager.Instance.CanAfford(120));
                GUI.enabled = canAffordBesteiro && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 60), "<b>Besteiro Anao</b>\n<color=#FFD700>120 Ouro</color>"))
                {
                    if (placementManager != null) placementManager.StartPlacement(besteiroTowerPrefab);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 70, drawerW - 20, 50), "<b>Martelador</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 1)
            {
                bool canAffordCannon = (GameManager.Instance != null && GameManager.Instance.CanAfford(250));
                GUI.enabled = canAffordCannon && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 60), "<b>Canhoneiro</b>\n<color=#FFD700>250 Ouro</color>"))
                {
                    if (placementManager != null) placementManager.StartPlacement(cannonTowerPrefab);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 70, drawerW - 20, 50), "<b>Balista Sniper</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 2)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 50), "<b>Ferreiro Runico</b>\n<i>(Em breve)</i>");
                GUI.Button(new Rect(drawerX + 10, cardY + 60, drawerW - 20, 50), "<b>Golem Protetor</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 3)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 50), "<b>Mina de Ouro</b>\n<i>(Em breve)</i>");
                GUI.Button(new Rect(drawerX + 10, cardY + 60, drawerW - 20, 50), "<b>Posto de Vigia</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }

            GUI.Label(new Rect(drawerX + 10, drawerY + drawerH - 45, drawerW - 20, 40), "<size=11><i>Clique direito para cancelar a colocacao.</i></size>");

            // 3. TELAS FINAIS
            if (state == GameState.Victory)
            {
                GUI.Box(new Rect(screenW / 2 - 200, screenH / 2 - 130, 400, 240), "");
                GUI.Label(new Rect(screenW / 2 - 180, screenH / 2 - 110, 360, 45), "<size=24><b><color=#FFD700>VITORIA DOS ANOES!</color></b></size>");
                GUI.Label(new Rect(screenW / 2 - 180, screenH / 2 - 60, 360, 40), "<size=15>A fortaleza resistiu a todas as 10 ondas!\nOs goblins fugiram de volta para o pantano.</size>");

                if (GUI.Button(new Rect(screenW / 2 - 120, screenH / 2 + 30, 240, 50), "<size=16><b>Jogar Novamente</b></size>"))
                {
                    RestartGame();
                }
            }
            else if (state == GameState.Defeat)
            {
                int currentWaveNum = waveSpawner != null ? waveSpawner.CurrentWave : 0;
                GUI.Box(new Rect(screenW / 2 - 200, screenH / 2 - 130, 400, 240), "");
                GUI.Label(new Rect(screenW / 2 - 180, screenH / 2 - 110, 360, 45), "<size=24><b><color=#FF4500>A FORTALEZA CAIU...</color></b></size>");
                GUI.Label(new Rect(screenW / 2 - 180, screenH / 2 - 60, 360, 40), $"<size=15>Os goblins invadiram os portoes!\nVoce resistiu ate a Rodada <b>{currentWaveNum}</b>.</size>");

                if (GUI.Button(new Rect(screenW / 2 - 120, screenH / 2 + 30, 240, 50), "<size=16><b>Tentar Novamente</b></size>"))
                {
                    RestartGame();
                }
            }
        }
    }
}
