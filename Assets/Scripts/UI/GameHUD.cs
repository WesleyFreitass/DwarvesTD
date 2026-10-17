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
        private Texture2D hudDropShadowTexture;
        private Texture2D statPillTexture;
        private Texture2D btnStartWaveTexture;
        private Texture2D btnSpeedTexture;
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
        private string cachedLivesRaw = "100";
        private string cachedGoldRaw = "650 G";
        private string cachedWaveRaw = "Wave 1 / 10";
        private string cachedFPSRaw = "60 FPS";
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
        private Font hudFont;
        private bool stylesInitialized = false;
        private float lastScreenW = 0f;
        private float lastScreenH = 0f;

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
            hudDropShadowTexture = LoadTexture("Sprites/UI/HUD_DropShadow");
            statPillTexture = LoadTexture("Sprites/UI/HUD_StatPill");
            btnStartWaveTexture = LoadTexture("Sprites/UI/Btn_StartWave_Glossy");
            btnSpeedTexture = LoadTexture("Sprites/UI/Btn_Speed_Teal");
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

        private void EnsureStyles(float refScale)
        {
            if (stylesInitialized && Mathf.Approximately(lastScreenW, Screen.width) && Mathf.Approximately(lastScreenH, Screen.height))
                return;

            lastScreenW = Screen.width;
            lastScreenH = Screen.height;

            if (hudFont == null)
            {
                hudFont = Resources.Load<Font>("Fonts/LilitaOne");
            }

            livesStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(22 * refScale),
                fontStyle = FontStyle.Bold
            };
            livesStyle.normal.textColor = Color.white;

            goldStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(22 * refScale),
                fontStyle = FontStyle.Bold
            };
            goldStyle.normal.textColor = Color.white;

            waveTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(22 * refScale),
                fontStyle = FontStyle.Bold
            };
            waveTitleStyle.normal.textColor = Color.white;

            waveSubStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = Mathf.RoundToInt(12 * refScale),
                fontStyle = FontStyle.Bold
            };
            waveSubStyle.normal.textColor = Color.white;

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(16 * refScale),
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = Color.white;

            fpsStyle = new GUIStyle(GUI.skin.label)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(14 * refScale),
                fontStyle = FontStyle.Bold
            };
            fpsStyle.normal.textColor = Color.white;

            speedBtnStyle = new GUIStyle(GUIStyle.none)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(17 * refScale),
                fontStyle = FontStyle.Bold
            };
            speedBtnStyle.normal.textColor = Color.white;

            playBtnStyle = new GUIStyle(GUIStyle.none)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleCenter,
                richText = false,
                fontSize = Mathf.RoundToInt(20 * refScale),
                fontStyle = FontStyle.Bold
            };
            playBtnStyle.normal.textColor = Color.white;

            cardBtnStyle = new GUIStyle(GUI.skin.button)
            {
                font = hudFont,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                padding = new RectOffset(52, 6, 4, 4)
            };

            stylesInitialized = true;
        }

        private void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, int stroke = 2)
        {
            Color prevColor = GUI.color;
            Color prevContent = GUI.contentColor;

            style.normal.textColor = outlineColor;
            GUI.contentColor = outlineColor;

            for (int x = -stroke; x <= stroke; x += stroke)
            {
                for (int y = -stroke; y <= stroke; y += stroke)
                {
                    if (x != 0 || y != 0)
                    {
                        GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), text, style);
                    }
                }
            }

            style.normal.textColor = textColor;
            GUI.contentColor = textColor;
            GUI.Label(rect, text, style);

            GUI.color = prevColor;
            GUI.contentColor = prevContent;
        }

        private void UpdateLivesCache(int lives)
        {
            cachedLivesRaw = lives.ToString();
        }

        private void UpdateGoldCache(int gold)
        {
            cachedGoldRaw = $"{gold} G";
        }

        private void UpdateWaveStartedCache(int wave)
        {
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveRaw = $"Wave {wave} / {max}";
        }

        private void UpdateWaveCompletedCache(int wave)
        {
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveRaw = $"Wave {wave} / {max}";
        }

        private void UpdateWaveWaitingState()
        {
            int current = waveSpawner != null ? waveSpawner.CurrentWave : 0;
            int max = GameManager.Instance != null ? GameManager.Instance.MaxWaves : 10;
            cachedWaveRaw = current == 0 ? $"Wave 1 / {max}" : $"Wave {current} / {max}";
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
                cachedFPSRaw = $"{currentFPS} FPS";
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
            float screenW = Screen.width;
            float screenH = Screen.height;

            // 1. PAINEL LATERAL DIREITO (Oficina dos Anoes)
            float drawerW = Mathf.Clamp(screenW * 0.22f, 240f, 400f);
            float drawerX = screenW - drawerW;

            // 2. BARRA SUPERIOR (Cobre toda a extensao superior, conectando perfeitamente a Oficina dos Anoes)
            float barX = 0f;
            float barY = 0f;
            float barW = screenW;
            float barH = Mathf.Clamp(screenH * 0.11f, 62f, 130f);
            float refScale = barH / 65f;
            EnsureStyles(refScale);

            bool waveInProgress = waveSpawner != null && waveSpawner.IsWaveInProgress;
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Playing;

            // Gaveta Lateral direita comeca exatamente abaixo da Barra de Pedra
            float drawerY = barH;
            float drawerH = screenH - drawerY;

            // MOLDURA DE PEDRA ESCURA SUPERIOR
            if (topBarTexture != null)
            {
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), topBarTexture, ScaleMode.StretchToFill, true);
            }
            else
            {
                GUI.Box(new Rect(barX, barY, barW, barH), "");
            }

            int stroke = refScale >= 1.4f ? 3 : 2;

            // CAPSULA DE VIDAS & OURO COMBINADA (Design 100% Fiel a Referencia)
            float pillH = Mathf.Round(barH * (42f / 65f));
            float pillW = Mathf.Round(pillH * (268f / 42f));
            float pillX = barX + Mathf.Round(14f * refScale);
            float pillY = barY + Mathf.Round(10f * refScale);

            if (statPillTexture != null)
            {
                GUI.DrawTexture(new Rect(pillX, pillY, pillW, pillH), statPillTexture, ScaleMode.StretchToFill, true);
            }

            // Texto de Vidas (dentro do compartimento escuro das vidas)
            float livesTextX = pillX + (pillW * (50f / 268f));
            float livesTextW = pillW * (74f / 268f);
            Rect livesTextRect = new Rect(livesTextX, pillY + (pillH * 0.05f), livesTextW, pillH * 0.90f);
            DrawOutlinedLabel(livesTextRect, cachedLivesRaw, livesStyle, Color.white, new Color(0.04f, 0.04f, 0.08f, 1f), stroke);

            // Texto de Ouro (dentro do compartimento escuro do ouro)
            float goldTextX = pillX + (pillW * (180f / 268f));
            float goldTextW = pillW * (86f / 268f);
            Rect goldTextRect = new Rect(goldTextX, pillY + (pillH * 0.05f), goldTextW, pillH * 0.90f);
            DrawOutlinedLabel(goldTextRect, cachedGoldRaw, goldStyle, new Color(1f, 0.82f, 0.05f, 1f), new Color(0.15f, 0.08f, 0.01f, 1f), stroke);

            // 3. PERGAMINHO CENTRAL DA RODADA (Banner_Scroll)
            float scrollH = Mathf.Round(barH * (49f / 65f));
            float scrollW = Mathf.Round(scrollH * (235f / 49f));
            float scrollX = barX + Mathf.Round(barW * 0.466f) - Mathf.Round(scrollW * 0.5f);
            float scrollY = barY + Mathf.Round(4f * refScale);

            if (bannerScroll != null)
            {
                GUI.DrawTexture(new Rect(scrollX, scrollY, scrollW, scrollH), bannerScroll, ScaleMode.StretchToFill, true);
            }

            Rect waveTextRect = new Rect(scrollX + scrollW * 0.16f, scrollY + scrollH * 0.14f, scrollW * 0.68f, scrollH * 0.66f);
            DrawOutlinedLabel(waveTextRect, cachedWaveRaw, waveTitleStyle, new Color(0.24f, 0.12f, 0.03f, 1f), new Color(0.98f, 0.92f, 0.80f, 0.95f), 1);

            // NOTIFICACAO DE BONUS DE RODADA
            if (!string.IsNullOrEmpty(cachedStatusLabel))
            {
                Rect statusRect = new Rect(scrollX - 60f, barY + barH + 6f, scrollW + 120f, 26f);
                DrawOutlinedLabel(statusRect, cachedStatusLabel, statusStyle, new Color(1f, 0.88f, 0.2f, 1f), Color.black, stroke);
            }

            // 4. BOTOES A DIREITA (START WAVE & VELOCIDADE)
            // BOTAO START WAVE (Verde Glossy com Rebordo Dourado)
            float playBtnH = Mathf.Round(barH * (46f / 65f));
            float playBtnW = Mathf.Round(playBtnH * (158f / 46f));
            float playBtnY = barY + Mathf.Round(8f * refScale);
            float playBtnX = barX + barW - playBtnW - Mathf.Round(16f * refScale);
            Rect playRect = new Rect(playBtnX, playBtnY, playBtnW, playBtnH);

            GUI.enabled = !waveInProgress && state == GameState.Playing;
            if (btnStartWaveTexture != null)
            {
                GUI.DrawTexture(playRect, btnStartWaveTexture, ScaleMode.StretchToFill, true);
            }
            string playBtnText = waveInProgress ? "LUTANDO..." : ((waveSpawner != null && waveSpawner.CurrentWave == 0) ? "START WAVE" : "PROXIMA");
            DrawOutlinedLabel(playRect, playBtnText, playBtnStyle, Color.white, new Color(0.06f, 0.28f, 0.02f, 1f), stroke);
            if (GUI.Button(playRect, GUIContent.none, GUIStyle.none))
            {
                StartNextWave();
            }
            GUI.enabled = true;

            // BOTAO VELOCIDADE (Teal Glossy)
            float speedBtnH = Mathf.Round(barH * (42f / 65f));
            float speedBtnW = Mathf.Round(speedBtnH * (117f / 42f));
            float speedBtnY = barY + Mathf.Round(10f * refScale);
            float speedBtnX = playBtnX - speedBtnW - Mathf.Round(19f * refScale);
            Rect speedRect = new Rect(speedBtnX, speedBtnY, speedBtnW, speedBtnH);

            if (btnSpeedTexture != null)
            {
                GUI.DrawTexture(speedRect, btnSpeedTexture, ScaleMode.StretchToFill, true);
            }
            string speedLabel = gameSpeed == 1f ? "1x Speed" : "2x Speed";
            DrawOutlinedLabel(speedRect, speedLabel, speedBtnStyle, Color.white, new Color(0.02f, 0.24f, 0.18f, 1f), stroke);
            if (GUI.Button(speedRect, GUIContent.none, GUIStyle.none))
            {
                ToggleGameSpeed();
            }

            // FPS COUNTER
            float fpsW = Mathf.Round(75f * refScale);
            float fpsX = speedBtnX - fpsW - Mathf.Round(12f * refScale);
            Rect fpsRect = new Rect(fpsX, speedBtnY, fpsW, speedBtnH);
            Color fpsColor = (currentFPS >= 55) ? new Color(0f, 1f, 0.4f) : ((currentFPS >= 30) ? Color.yellow : Color.red);
            DrawOutlinedLabel(fpsRect, cachedFPSRaw, fpsStyle, fpsColor, Color.black, 1);

            // 5. PAINEL LATERAL DIREITO (Oficina dos Anoes)
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
