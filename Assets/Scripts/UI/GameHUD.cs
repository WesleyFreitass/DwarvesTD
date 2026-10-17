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
        [SerializeField] private GameObject runicTowerPrefab;
        [SerializeField] private GameObject goldMineTowerPrefab;
        [SerializeField] private GameObject heroPrefab;

        [Header("UI Textures")]
        private Texture2D topBarTexture;
        private Texture2D iconHeart;
        private Texture2D iconCoin;
        private Texture2D iconScroll;
        private Texture2D bannerScroll;
        private Texture2D iconCrown;
        private Texture2D iconAxe;
        private Texture2D iconBomb;
        private Texture2D iconHammer;
        private Texture2D iconGoldChest;
        private Texture2D iconTarget;
        private Texture2D iconUpgrade;

        private float gameSpeed = 1f;
        private string statusMessage = "";
        private float statusTimer = 0f;

        private int selectedCategoryTab = 0;
        private readonly string[] categoryNames = new string[] { "Cla", "Cerco", "Runas", "Apoio" };

        // Strings cacheadas para eliminar 100% das alocacoes de Garbage Collection no OnGUI
        private string cachedLivesLabel = "<size=17><b>100</b></size>";
        private string cachedGoldLabel = "<size=17><color=#FFD700><b>650</b></color></size>";
        private string cachedWaveLabel = "<size=15><b><color=#381D06>ONDA 1 / 10</color></b></size>";
        private string cachedWaveStatusLabel = "<size=10><b><color=#3E6B28>PREPARATIVOS</color></b></size>";
        private string cachedFPSLabel = "<size=14><b><color=#00FF66>60 FPS</color></b></size>";
        private string cachedStatusLabel = "";

        // GUIStyles reutilizados (sem alocacoes por quadro)
        private GUIStyle livesStyle;
        private GUIStyle goldStyle;
        private GUIStyle waveTitleStyle;
        private GUIStyle waveSubStyle;
        private GUIStyle statusStyle;
        private GUIStyle fpsStyle;
        private GUIStyle speedBtnStyle;
        private GUIStyle playBtnStyle;
        private GUIStyle cardBtnStyle;
        private bool stylesInitialized = false;

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
            if (runicTowerPrefab == null) runicTowerPrefab = Resources.Load<GameObject>("Prefabs/RunicTower");
            if (goldMineTowerPrefab == null) goldMineTowerPrefab = Resources.Load<GameObject>("Prefabs/GoldMineTower");
            if (heroPrefab == null) heroPrefab = Resources.Load<GameObject>("Prefabs/HeroThorin");

            LoadTextures();

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
                waveSpawner.OnWaveStarted += UpdateWaveStartedCache;
                waveSpawner.OnWaveCompleted += UpdateWaveCompletedCache;
                UpdateWaveWaitingState();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLivesChanged -= UpdateLivesCache;
                GameManager.Instance.OnGoldChanged -= UpdateGoldCache;
                GameManager.Instance.OnRoundRewardGranted -= HandleRoundReward;
            }

            if (waveSpawner != null)
            {
                waveSpawner.OnWaveStarted -= UpdateWaveStartedCache;
                waveSpawner.OnWaveCompleted -= UpdateWaveCompletedCache;
            }
        }

        private Texture2D LoadTexture(string path)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Sprite spr = Resources.Load<Sprite>(path);
                if (spr != null) tex = spr.texture;
            }
            return tex;
        }

        private void LoadTextures()
        {
            topBarTexture = LoadTexture("Sprites/UI/TopBar_Frame");
            iconHeart = LoadTexture("Sprites/UI/Icon_Heart");
            iconCoin = LoadTexture("Sprites/UI/Icon_Coin");
            iconScroll = LoadTexture("Sprites/UI/Icon_Scroll");
            bannerScroll = LoadTexture("Sprites/UI/Banner_Scroll");
            iconCrown = LoadTexture("Sprites/UI/Icon_Crown");
            iconAxe = LoadTexture("Sprites/UI/Icon_Axe");
            iconBomb = LoadTexture("Sprites/UI/Icon_Bomb");
            iconHammer = LoadTexture("Sprites/UI/Icon_Hammer");
            iconGoldChest = LoadTexture("Sprites/UI/Icon_GoldChest");
            iconTarget = LoadTexture("Sprites/UI/Icon_Target");
            iconUpgrade = LoadTexture("Sprites/UI/Icon_Upgrade");
        }

        private void EnsureStyles()
        {
            if (stylesInitialized) return;

            livesStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            goldStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            waveTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            waveSubStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            fpsStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            speedBtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            playBtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            cardBtnStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                padding = new RectOffset(52, 6, 4, 4)
            };

            stylesInitialized = true;
        }

        private void UpdateLivesCache(int lives)
        {
            cachedLivesLabel = $"<b>{lives}</b>";
        }

        private void UpdateGoldCache(int gold)
        {
            cachedGoldLabel = $"<color=#FFD700><b>{gold}</b></color>";
        }

        private void UpdateWaveStartedCache(int wave)
        {
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveLabel = $"<color=#381D06><b>RODADA {wave} / {max}</b></color>";
            cachedWaveStatusLabel = "<color=#8B0000><b>EM ANDAMENTO</b></color>";
        }

        private void UpdateWaveCompletedCache(int wave)
        {
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveLabel = $"<color=#381D06><b>RODADA {wave} / {max}</b></color>";
            cachedWaveStatusLabel = "<color=#2A6B28><b>VENCIDA!</b></color>";
        }

        private void UpdateWaveWaitingState()
        {
            int current = waveSpawner != null ? waveSpawner.CurrentWave : 0;
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            if (current == 0)
            {
                cachedWaveLabel = $"<color=#381D06><b>RODADA 1 / {max}</b></color>";
                cachedWaveStatusLabel = "<color=#3E6B28><b>PREPARATIVOS</b></color>";
            }
            else
            {
                cachedWaveLabel = $"<color=#381D06><b>RODADA {current} / {max}</b></color>";
                cachedWaveStatusLabel = "<color=#2A6B28><b>CONCLUIDA</b></color>";
            }
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
                cachedFPSLabel = $"<size=13><b><color={fpsColor}>{currentFPS} FPS</color></b></size>";
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
            cachedStatusLabel = $"<size=14><b><color=#FFD700>+ {bonus} Ouro da Rodada {round}!</color></b></size>";
            statusTimer = 3.5f;
            UpdateWaveWaitingState();
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
            EnsureStyles();

            bool waveInProgress = waveSpawner != null && waveSpawner.IsWaveInProgress;
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Playing;

            float screenW = Screen.width;
            float screenH = Screen.height;

            // 1. BARRA SUPERIOR FULL HD (TopBar_Frame - Opcao 2)
            float uiScale = Mathf.Clamp(screenW / 1920f, 0.7f, 1.25f);
            float barH = Mathf.Clamp(screenH * 0.082f, 70f, 92f);
            float barW = screenW;
            float barX = 0f;
            float barY = 0f;

            if (topBarTexture != null)
            {
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), topBarTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                GUI.Box(new Rect(barX, barY, barW, barH), "");
            }

            // SLOT 1: VIDAS (Coracao + Quantidade)
            float iconSize = Mathf.Round(barH * 0.72f);
            float slotY = (barH - iconSize) * 0.5f;
            float heartX = 24f * uiScale;
            if (iconHeart != null)
            {
                GUI.DrawTexture(new Rect(heartX, slotY, iconSize, iconSize), iconHeart, ScaleMode.ScaleToFit, true);
            }
            float livesTextX = heartX + iconSize + 10f * uiScale;
            float livesTextW = 80f * uiScale;
            GUI.Label(new Rect(livesTextX, slotY - 1f, livesTextW, iconSize + 2f), cachedLivesLabel, livesStyle);

            // SLOT 2: OURO (Moeda + Quantidade)
            float coinX = livesTextX + livesTextW + 16f * uiScale;
            if (iconCoin != null)
            {
                GUI.DrawTexture(new Rect(coinX, slotY, iconSize, iconSize), iconCoin, ScaleMode.ScaleToFit, true);
            }
            float goldTextX = coinX + iconSize + 10f * uiScale;
            float goldTextW = 100f * uiScale;
            GUI.Label(new Rect(goldTextX, slotY - 1f, goldTextW, iconSize + 2f), cachedGoldLabel, goldStyle);

            // CENTRO: PERGAMINHO DA RODADA (Banner_Scroll)
            float scrollH = Mathf.Round(barH * 0.84f);
            float scrollW = Mathf.Round(scrollH * 3.228f);
            float scrollX = (screenW - scrollW) * 0.5f;
            float scrollY = (barH - scrollH) * 0.5f;

            if (bannerScroll != null)
            {
                GUI.DrawTexture(new Rect(scrollX, scrollY, scrollW, scrollH), bannerScroll, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(new Rect(scrollX + 26f * uiScale, scrollY + scrollH * 0.16f, scrollW - 52f * uiScale, scrollH * 0.44f), cachedWaveLabel, waveTitleStyle);
            GUI.Label(new Rect(scrollX + 26f * uiScale, scrollY + scrollH * 0.56f, scrollW - 52f * uiScale, scrollH * 0.34f), cachedWaveStatusLabel, waveSubStyle);

            // NOTIFICACAO DE BONUS DE RODADA
            if (!string.IsNullOrEmpty(cachedStatusLabel))
            {
                GUI.Label(new Rect(scrollX - 60f, barH + 4f, scrollW + 120f, 26f), cachedStatusLabel, statusStyle);
            }

            // CONTROLES A DIREITA
            float btnH = Mathf.Round(barH * 0.58f);
            float btnY = (barH - btnH) * 0.5f;

            // BOTAO INICIAR / PROXIMA RODADA
            float playBtnW = 145f * uiScale;
            float playBtnX = screenW - playBtnW - 20f * uiScale;
            Rect playRect = new Rect(playBtnX, btnY, playBtnW, btnH);

            GUI.enabled = !waveInProgress && state == GameState.Playing;
            string playBtnText;
            if (waveInProgress)
            {
                playBtnText = "<color=#CCCCCC>Lutando...</color>";
            }
            else if (waveSpawner != null && waveSpawner.CurrentWave == 0)
            {
                playBtnText = "<color=#00FF66><b>Iniciar!</b></color>";
            }
            else
            {
                playBtnText = "<color=#FFD700><b>Proxima >></b></color>";
            }

            if (GUI.Button(playRect, playBtnText, playBtnStyle))
            {
                StartNextWave();
            }
            GUI.enabled = true;

            // BOTAO VELOCIDADE (1x / 2x)
            float speedBtnW = 95f * uiScale;
            float speedBtnX = playBtnX - speedBtnW - 12f * uiScale;
            Rect speedRect = new Rect(speedBtnX, btnY, speedBtnW, btnH);

            string speedLabel = gameSpeed == 1f ? "<b>1x</b>" : "<color=#00FF66><b>2x >></b></color>";
            if (GUI.Button(speedRect, speedLabel, speedBtnStyle))
            {
                ToggleGameSpeed();
            }

            // INDICADOR DE FPS
            float fpsX = speedBtnX - 85f * uiScale;
            GUI.Label(new Rect(fpsX, btnY, 80f * uiScale, btnH), cachedFPSLabel, fpsStyle);

            // 2. PAINEL LATERAL DIREITO (Oficina dos Anoes)
            float drawerW = 210f;
            float drawerX = screenW - drawerW - 10f;
            float drawerY = barH + 12f;
            float drawerH = screenH - drawerY - 10f;

            GUI.Box(new Rect(drawerX, drawerY, drawerW, drawerH), "<b>OFICINA DOS ANOES</b>");

            // SECAO DO HEROI COMANDANTE
            float heroBoxY = drawerY + 28f;
            GUI.Box(new Rect(drawerX + 6, heroBoxY, drawerW - 12, 88), "");

            DwarfHero hero = DwarfHero.Instance;
            if (hero == null)
            {
                bool canAffordHero = (GameManager.Instance != null && GameManager.Instance.CanAfford(150));
                GUI.enabled = canAffordHero && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, heroBoxY + 8, drawerW - 20, 72), "        <b>Comandante Thorin</b>\n        <color=#FFD700>150 Ouro (Heroi)</color>\n        <size=10>Lideranca & Warcry</size>"))
                {
                    if (placementManager != null && heroPrefab != null) placementManager.StartPlacement(heroPrefab);
                }
                if (iconCrown != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 14, heroBoxY + 20, 32, 32), iconCrown, ScaleMode.ScaleToFit, true);
                }
                GUI.enabled = true;
            }
            else
            {
                if (iconCrown != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 10, heroBoxY + 6, 24, 24), iconCrown, ScaleMode.ScaleToFit, true);
                }
                GUI.Label(new Rect(drawerX + 38, heroBoxY + 4, drawerW - 46, 20), $"<b>{hero.HeroName}</b> <color=#FFD700>(Nv. {hero.CurrentLevel})</color>");
                GUI.Label(new Rect(drawerX + 38, heroBoxY + 22, drawerW - 46, 18), $"<size=11>XP: {hero.CurrentXP} / {hero.GetRequiredXPForNextLevel()}</size>");

                if (!hero.IsAbilityUnlocked)
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(drawerX + 10, heroBoxY + 44, drawerW - 20, 36), "<size=11><i>Grito de Guerra (Nv. 3)</i></size>");
                    GUI.enabled = true;
                }
                else if (hero.IsWarcryActive)
                {
                    GUI.Button(new Rect(drawerX + 10, heroBoxY + 44, drawerW - 20, 36), $"<size=11><b><color=#00FF66>GRITO ATIVO! ({hero.WarcryActiveRemaining:F1}s)</color></b></size>");
                }
                else if (hero.IsAbilityReady)
                {
                    if (GUI.Button(new Rect(drawerX + 10, heroBoxY + 44, drawerW - 20, 36), "<size=11><b><color=#FFD700>[GRITO DE GUERRA]</color></b></size>"))
                    {
                        hero.ActivateWarcry();
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(drawerX + 10, heroBoxY + 44, drawerW - 20, 36), $"<size=11>[Recarga: {hero.AbilityCooldownRemaining:F0}s]</size>");
                    GUI.enabled = true;
                }
            }

            // ABAS DAS 4 GUILDAS
            float tabsY = heroBoxY + 94f;
            float tabW = (drawerW - 12f) / 4f;
            for (int i = 0; i < categoryNames.Length; i++)
            {
                bool isSelected = (selectedCategoryTab == i);
                if (GUI.Toggle(new Rect(drawerX + 6 + i * tabW, tabsY, tabW, 26), isSelected, categoryNames[i], "Button"))
                {
                    selectedCategoryTab = i;
                }
            }

            float cardY = tabsY + 34f;

            if (selectedCategoryTab == 0) // Cla Combatente
            {
                bool canAffordBesteiro = (GameManager.Instance != null && GameManager.Instance.CanAfford(120));
                GUI.enabled = canAffordBesteiro && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 64), "<b>Besteiro Anao</b>\n<color=#FFD700>120 Ouro</color>\n<size=10>Tiro direto rapido</size>", cardBtnStyle))
                {
                    if (placementManager != null && besteiroTowerPrefab != null) placementManager.StartPlacement(besteiroTowerPrefab);
                }
                if (iconAxe != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 14, cardY + 12, 36, 36), iconAxe, ScaleMode.ScaleToFit, true);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 72, drawerW - 20, 52), "<b>Martelador</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 1) // Engenharia & Cerco
            {
                bool canAffordCannon = (GameManager.Instance != null && GameManager.Instance.CanAfford(250));
                GUI.enabled = canAffordCannon && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 64), "<b>Canhoneiro</b>\n<color=#FFD700>250 Ouro</color>\n<size=10>Explosao AoE / Blindado</size>", cardBtnStyle))
                {
                    if (placementManager != null && cannonTowerPrefab != null) placementManager.StartPlacement(cannonTowerPrefab);
                }
                if (iconBomb != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 14, cardY + 12, 36, 36), iconBomb, ScaleMode.ScaleToFit, true);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 72, drawerW - 20, 52), "<b>Balista Sniper</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 2) // Forja Runica & Alquimia
            {
                bool canAffordRunic = (GameManager.Instance != null && GameManager.Instance.CanAfford(320));
                GUI.enabled = canAffordRunic && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 64), "<b>Ferreiro Runico</b>\n<color=#FFD700>320 Ouro</color>\n<size=10>Magia / Lentidao 45%</size>", cardBtnStyle))
                {
                    if (placementManager != null && runicTowerPrefab != null) placementManager.StartPlacement(runicTowerPrefab);
                }
                if (iconHammer != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 14, cardY + 12, 36, 36), iconHammer, ScaleMode.ScaleToFit, true);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 72, drawerW - 20, 52), "<b>Golem Protetor</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }
            else if (selectedCategoryTab == 3) // Mineracao & Suporte
            {
                bool canAffordMine = (GameManager.Instance != null && GameManager.Instance.CanAfford(400));
                GUI.enabled = canAffordMine && state == GameState.Playing;
                if (GUI.Button(new Rect(drawerX + 10, cardY, drawerW - 20, 64), "<b>Mina de Ouro</b>\n<color=#FFD700>400 Ouro</color>\n<size=10>+80 Ouro por rodada</size>", cardBtnStyle))
                {
                    if (placementManager != null && goldMineTowerPrefab != null) placementManager.StartPlacement(goldMineTowerPrefab);
                }
                if (iconGoldChest != null)
                {
                    GUI.DrawTexture(new Rect(drawerX + 14, cardY + 12, 36, 36), iconGoldChest, ScaleMode.ScaleToFit, true);
                }
                GUI.enabled = true;

                GUI.enabled = false;
                GUI.Button(new Rect(drawerX + 10, cardY + 72, drawerW - 20, 52), "<b>Posto de Vigia</b>\n<i>(Em breve)</i>");
                GUI.enabled = true;
            }

            GUI.Label(new Rect(drawerX + 10, drawerY + drawerH - 45, drawerW - 20, 40), "<size=10><i>Botao direito: cancelar.\nClique na torre: inspecionar.</i></size>");

            // 3. TELAS FINAIS (VITORIA / DERROTA)
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
