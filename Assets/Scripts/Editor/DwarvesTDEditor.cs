using System.IO;
using DwarvesTD.Core;
using DwarvesTD.Enemies;
using DwarvesTD.Pathfinding;
using DwarvesTD.Projectiles;
using DwarvesTD.Towers;
using DwarvesTD.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DwarvesTD.Editor
{
    public struct GeneratedAssets
    {
        public Material matGround;
        public Material matPath;
        public GameObject projPrefab;
        public GameObject dwarfPrefab;
        public GameObject cannonTowerPrefab;
        public GameObject cannonballPrefab;
        public GameObject explosionFXPrefab;
        public GameObject runicBoltPrefab;
        public GameObject runicTowerPrefab;
        public GameObject goldMineTowerPrefab;
        public GameObject heroHammerPrefab;
        public GameObject heroPrefab;
        public GameObject warcryFXPrefab;
        public GameObject recruitPrefab;
        public GameObject runnerPrefab;
        public GameObject armoredPrefab;
        public GameObject trollBossPrefab;
    }

    [InitializeOnLoad]
    public static class DwarvesTDEditor
    {
        private const string PREFABS_DIR = "Assets/Prefabs";
        private const string RESOURCES_PREFABS_DIR = "Assets/Resources/Prefabs";
        private const string MATERIALS_DIR = "Assets/Materials";
        private const string SCENES_DIR = "Assets/Scenes";

        static DwarvesTDEditor()
        {
            EditorApplication.delayCall += AutoEnsureAssets;
        }

        private static void AutoEnsureAssets()
        {
            if (Application.isPlaying) return;

            string recruitPath = $"{RESOURCES_PREFABS_DIR}/GoblinRecruit.prefab";
            string cannonPath = $"{RESOURCES_PREFABS_DIR}/CannonTower.prefab";
            string runicPath = $"{RESOURCES_PREFABS_DIR}/RunicTower.prefab";
            string goldMinePath = $"{RESOURCES_PREFABS_DIR}/GoldMineTower.prefab";
            string heroPath = $"{RESOURCES_PREFABS_DIR}/HeroThorin.prefab";

            if (!File.Exists(recruitPath) || !File.Exists(cannonPath) || !File.Exists(runicPath) || !File.Exists(goldMinePath) || !File.Exists(heroPath))
            {
                Debug.Log("[DwarvesTDEditor] Prefabs ausentes detectados. Gerando automaticamente todos os prefabs das 4 Guildas e Heroi...");
                GeneratePrefabsAndMaterialsInternal();
            }
        }

        [MenuItem("Dwarves TD/Gerar Arena de Teste Jogavel", false, 1)]
        public static void GeneratePlayableArena()
        {
            GeneratedAssets assets = GeneratePrefabsAndMaterialsInternal();

            // Montar a Cena
            SetupScene(
                assets.matGround,
                assets.matPath,
                assets.recruitPrefab,
                assets.runnerPrefab,
                assets.armoredPrefab,
                assets.trollBossPrefab,
                assets.dwarfPrefab,
                assets.cannonTowerPrefab,
                assets.runicTowerPrefab,
                assets.goldMineTowerPrefab,
                assets.heroPrefab
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Dwarves TD - Arsenal Completo dos Anões!",
                    "Arena gerada com todas as 4 Guildas dos Anões + Herói Comandante!\n\n" +
                    "1. Clã Combatente: Besteiro Anão ($120)\n" +
                    "2. Engenharia & Cerco: Canhoneiro de Pólvora ($250)\n" +
                    "3. Forja Rúnica & Alquimia: Ferreiro Rúnico ($320 - Magia / Lentidão 45%)\n" +
                    "4. Mineração & Suporte: Mina de Ouro ($400 - +80 Ouro por rodada)\n" +
                    "5. Herói do Clã: Comandante Thorin ($150 - XP, Níveis 1-10 e Grito de Guerra!)\n\n" +
                    "Dê o PLAY e lidere os anões rumo à vitória!",
                    "Excelente!"
                );
            }
        }

        public static GeneratedAssets GeneratePrefabsAndMaterialsInternal()
        {
            EnsureDirectories();

            // 1. Materiais com GPU Instancing ativado
            Material matGround = GetOrCreateMaterial("Mat_GrassGround", new Color(0.38f, 0.68f, 0.22f));
            Material matPath = GetOrCreateMaterial("Mat_StonePath", new Color(0.68f, 0.70f, 0.68f));

            Material matDwarf = GetOrCreateMaterial("Mat_DwarfBronze", new Color(0.85f, 0.50f, 0.20f));
            Material matTurret = GetOrCreateMaterial("Mat_DwarfTurret", new Color(0.35f, 0.25f, 0.20f));
            Material matProj = GetOrCreateMaterial("Mat_AxeProjectile", new Color(0.9f, 0.2f, 0.15f));
            Material matRange = GetOrCreateTransparentMaterial("Mat_RangeIndicator", new Color(0.2f, 0.7f, 1f, 0.25f));

            // Materiais do Canhoneiro de Polvora
            Material matCannonBase   = GetOrCreateMaterial("Mat_DwarfCannonBase", new Color(0.24f, 0.26f, 0.28f));
            Material matCannonBarrel = GetOrCreateMaterial("Mat_DwarfCannonBarrel", new Color(0.14f, 0.15f, 0.17f));
            Material matCannonball   = GetOrCreateMaterial("Mat_Cannonball", new Color(0.10f, 0.10f, 0.12f));
            Material matExplosion    = GetOrCreateMaterial("Mat_ExplosionBlast", new Color(1.0f, 0.45f, 0.05f));

            // Materiais do Ferreiro Runico
            Material matRunicCyan  = GetOrCreateMaterial("Mat_RunicCyan", new Color(0.0f, 0.90f, 1.0f));
            Material matRunicStone = GetOrCreateMaterial("Mat_RunicStone", new Color(0.12f, 0.16f, 0.22f));

            // Materiais da Mina de Ouro
            Material matMineWood = GetOrCreateMaterial("Mat_MineWood", new Color(0.42f, 0.26f, 0.15f));
            Material matGoldOre  = GetOrCreateMaterial("Mat_GoldOre", new Color(1.0f, 0.84f, 0.0f));

            // Materiais do Heroi Comandante
            Material matHeroArmor = GetOrCreateMaterial("Mat_HeroArmor", new Color(0.78f, 0.52f, 0.18f));
            Material matHeroBeard = GetOrCreateMaterial("Mat_HeroBeard", new Color(0.68f, 0.18f, 0.15f));

            // Materiais de Inimigos
            Material matGoblinRecruit = GetOrCreateMaterial("Mat_GoblinRecruit", new Color(0.20f, 0.85f, 0.20f));
            Material matGoblinRunner  = GetOrCreateMaterial("Mat_GoblinRunner",  new Color(1.0f, 0.88f, 0.05f));
            Material matGoblinArmored = GetOrCreateMaterial("Mat_GoblinArmored", new Color(0.35f, 0.45f, 0.55f));
            Material matTrollBoss     = GetOrCreateMaterial("Mat_TrollBoss",     new Color(0.55f, 0.25f, 0.12f));

            // 2. Prefabs de Torres e Projeteis
            GameObject projPrefab = CreateProjectilePrefab(matProj);
            GameObject dwarfPrefab = CreateDwarfPrefab(matDwarf, matTurret, matRange, projPrefab);

            GameObject explosionFXPrefab = CreateExplosionFXPrefab(matExplosion);
            GameObject cannonballPrefab = CreateCannonballPrefab(matCannonball, explosionFXPrefab);
            GameObject cannonTowerPrefab = CreateCannonTowerPrefab(matCannonBase, matCannonBarrel, matRange, cannonballPrefab);

            // Torre 3: Ferreiro Runico
            GameObject runicBoltPrefab = CreateRunicBoltPrefab(matRunicCyan);
            GameObject runicTowerPrefab = CreateRunicTowerPrefab(matRunicStone, matRunicCyan, matRange, runicBoltPrefab);

            // Torre 4: Mina de Ouro
            GameObject goldMineTowerPrefab = CreateGoldMineTowerPrefab(matMineWood, matGoldOre, matRange);

            // Heroi: Comandante Thorin
            GameObject heroHammerPrefab = CreateHeroHammerPrefab(matHeroArmor);
            GameObject heroPrefab = CreateHeroThorinPrefab(matHeroArmor, matHeroBeard, matRange, heroHammerPrefab, explosionFXPrefab);

            // 3. Prefabs de Goblins
            GameObject recruitPrefab = CreateGoblinRecruitPrefab(matGoblinRecruit);
            GameObject runnerPrefab = CreateGoblinRunnerPrefab(matGoblinRunner);
            GameObject armoredPrefab = CreateGoblinArmoredPrefab(matGoblinArmored);
            GameObject trollBossPrefab = CreateTrollBossPrefab(matTrollBoss, recruitPrefab);

            AssetDatabase.SaveAssets();

            return new GeneratedAssets
            {
                matGround = matGround,
                matPath = matPath,
                projPrefab = projPrefab,
                dwarfPrefab = dwarfPrefab,
                cannonTowerPrefab = cannonTowerPrefab,
                cannonballPrefab = cannonballPrefab,
                explosionFXPrefab = explosionFXPrefab,
                runicBoltPrefab = runicBoltPrefab,
                runicTowerPrefab = runicTowerPrefab,
                goldMineTowerPrefab = goldMineTowerPrefab,
                heroHammerPrefab = heroHammerPrefab,
                heroPrefab = heroPrefab,
                warcryFXPrefab = explosionFXPrefab,
                recruitPrefab = recruitPrefab,
                runnerPrefab = runnerPrefab,
                armoredPrefab = armoredPrefab,
                trollBossPrefab = trollBossPrefab
            };
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(PREFABS_DIR)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(RESOURCES_PREFABS_DIR)) AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
            if (!AssetDatabase.IsValidFolder(MATERIALS_DIR)) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(SCENES_DIR)) AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MATERIALS_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");

                mat = new Material(urpShader);
                mat.color = color;
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material GetOrCreateTransparentMaterial(string name, Color color)
        {
            string path = $"{MATERIALS_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");

                mat = new Material(urpShader);
                mat.color = color;
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static GameObject CreateProjectilePrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/AxeProjectile.prefab";
            string altPath = $"{PREFABS_DIR}/AxeProjectile.prefab";

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Renderer projRend = obj.GetComponent<Renderer>();
            projRend.sharedMaterial = mat;
            projRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            obj.AddComponent<BaseProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            PrefabUtility.SaveAsPrefabAsset(obj, altPath);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateRunicBoltPrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/RunicBolt.prefab";
            string altPath = $"{PREFABS_DIR}/RunicBolt.prefab";

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "RunicBolt";
            obj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            Renderer rend = obj.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            BaseProjectile proj = obj.AddComponent<BaseProjectile>();
            proj.SetSlowStats(percent: 0.45f, duration: 2.5f, bypassArmor: true);

            SerializedObject serObj = new SerializedObject(proj);
            serObj.FindProperty("speed").floatValue = 15f;
            serObj.FindProperty("damage").intValue = 2;
            serObj.FindProperty("ignoreArmor").boolValue = true;
            serObj.FindProperty("slowPercent").floatValue = 0.45f;
            serObj.FindProperty("slowDuration").floatValue = 2.5f;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            PrefabUtility.SaveAsPrefabAsset(obj, altPath);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateHeroHammerPrefab(Material matArmor)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/HeroHammer.prefab";
            string altPath = $"{PREFABS_DIR}/HeroHammer.prefab";

            GameObject root = new GameObject("HeroHammer");
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "HammerHead";
            head.transform.SetParent(root.transform);
            head.transform.localScale = new Vector3(0.45f, 0.35f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = matArmor;
            head.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            BaseProjectile proj = root.AddComponent<BaseProjectile>();
            SerializedObject serObj = new SerializedObject(proj);
            serObj.FindProperty("speed").floatValue = 17f;
            serObj.FindProperty("damage").intValue = 2;
            serObj.FindProperty("pierce").intValue = 1;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateExplosionFXPrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/ExplosionFX.prefab";
            string altPath = $"{PREFABS_DIR}/ExplosionFX.prefab";

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "ExplosionFX";
            Renderer rend = obj.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            obj.AddComponent<ExplosionFX>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            PrefabUtility.SaveAsPrefabAsset(obj, altPath);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateCannonballPrefab(Material mat, GameObject vfxPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/Cannonball.prefab";
            string altPath = $"{PREFABS_DIR}/Cannonball.prefab";

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Cannonball";
            obj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            Renderer projRend = obj.GetComponent<Renderer>();
            projRend.sharedMaterial = mat;
            projRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(obj.GetComponent<Collider>());

            BaseProjectile proj = obj.AddComponent<BaseProjectile>();
            proj.SetExplosiveStats(explosive: true, radius: 2.5f, bypassArmor: true, vfx: vfxPrefab);

            SerializedObject serObj = new SerializedObject(proj);
            serObj.FindProperty("speed").floatValue = 13f;
            serObj.FindProperty("damage").intValue = 3;
            serObj.FindProperty("isExplosive").boolValue = true;
            serObj.FindProperty("explosionRadius").floatValue = 2.5f;
            serObj.FindProperty("ignoreArmor").boolValue = true;
            serObj.FindProperty("explosionPrefab").objectReferenceValue = vfxPrefab;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            PrefabUtility.SaveAsPrefabAsset(obj, altPath);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateDwarfPrefab(Material matBody, Material matTurret, Material matRange, GameObject projPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/DwarfTower.prefab";
            string altPath = $"{PREFABS_DIR}/DwarfTower.prefab";

            GameObject root = new GameObject("DwarfTower");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "DwarfBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.45f, 0);
            body.transform.localScale = new Vector3(0.85f, 0.45f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turret.name = "RotatingHead";
            turret.transform.SetParent(root.transform);
            turret.transform.localPosition = new Vector3(0, 0.9f, 0);
            turret.transform.localScale = new Vector3(0.6f, 0.45f, 0.8f);
            turret.GetComponent<Renderer>().sharedMaterial = matTurret;
            Object.DestroyImmediate(turret.GetComponent<Collider>());

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(turret.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 0.6f);

            GameObject rangeVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeVis.name = "RangeVisual";
            rangeVis.transform.SetParent(root.transform);
            rangeVis.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeVis.transform.localScale = new Vector3(9f, 0.01f, 9f);
            rangeVis.GetComponent<Renderer>().sharedMaterial = matRange;
            Object.DestroyImmediate(rangeVis.GetComponent<Collider>());
            rangeVis.SetActive(false);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 0.6f, 0);

            BaseDwarfTower tower = root.AddComponent<BaseDwarfTower>();
            SerializedObject serObj = new SerializedObject(tower);
            serObj.FindProperty("dwarfName").stringValue = "Besteiro Anao";
            serObj.FindProperty("attackRange").floatValue = 4.5f;
            serObj.FindProperty("fireCooldown").floatValue = 0.8f;
            serObj.FindProperty("damage").intValue = 1;
            serObj.FindProperty("cost").intValue = 120;
            serObj.FindProperty("category").enumValueIndex = (int)TowerCategory.Cla;
            serObj.FindProperty("rotatingPart").objectReferenceValue = turret.transform;
            serObj.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            serObj.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
            serObj.FindProperty("rangeIndicatorVisual").objectReferenceValue = rangeVis;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCannonTowerPrefab(Material matBase, Material matBarrel, Material matRange, GameObject cannonballPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/CannonTower.prefab";
            string altPath = $"{PREFABS_DIR}/CannonTower.prefab";

            GameObject root = new GameObject("CannonTower");

            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.name = "CannonBase";
            basePlatform.transform.SetParent(root.transform);
            basePlatform.transform.localPosition = new Vector3(0, 0.22f, 0);
            basePlatform.transform.localScale = new Vector3(1.1f, 0.22f, 1.1f);
            basePlatform.GetComponent<Renderer>().sharedMaterial = matBase;
            Object.DestroyImmediate(basePlatform.GetComponent<Collider>());

            GameObject rotatingHead = new GameObject("RotatingHead");
            rotatingHead.transform.SetParent(root.transform);
            rotatingHead.transform.localPosition = new Vector3(0, 0.45f, 0);

            GameObject mount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mount.name = "Mount";
            mount.transform.SetParent(rotatingHead.transform);
            mount.transform.localPosition = new Vector3(0, 0.15f, 0);
            mount.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
            mount.GetComponent<Renderer>().sharedMaterial = matBase;
            Object.DestroyImmediate(mount.GetComponent<Collider>());

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "CannonBarrel";
            barrel.transform.SetParent(rotatingHead.transform);
            barrel.transform.localPosition = new Vector3(0, 0.35f, 0.32f);
            barrel.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.38f, 0.65f, 0.38f);
            barrel.GetComponent<Renderer>().sharedMaterial = matBarrel;
            Object.DestroyImmediate(barrel.GetComponent<Collider>());

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(rotatingHead.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.45f, 0.95f);

            GameObject rangeVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeVis.name = "RangeVisual";
            rangeVis.transform.SetParent(root.transform);
            rangeVis.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeVis.transform.localScale = new Vector3(11f, 0.01f, 11f);
            rangeVis.GetComponent<Renderer>().sharedMaterial = matRange;
            Object.DestroyImmediate(rangeVis.GetComponent<Collider>());
            rangeVis.SetActive(false);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.3f;
            col.radius = 0.55f;
            col.center = new Vector3(0, 0.65f, 0);

            BaseDwarfTower tower = root.AddComponent<BaseDwarfTower>();
            SerializedObject serObj = new SerializedObject(tower);
            serObj.FindProperty("dwarfName").stringValue = "Canhoneiro de Polvora";
            serObj.FindProperty("cost").intValue = 250;
            serObj.FindProperty("category").enumValueIndex = (int)TowerCategory.Cerco;
            serObj.FindProperty("attackRange").floatValue = 5.5f;
            serObj.FindProperty("fireCooldown").floatValue = 1.8f;
            serObj.FindProperty("damage").intValue = 3;
            serObj.FindProperty("projectileSpeed").floatValue = 13f;
            serObj.FindProperty("rotatingPart").objectReferenceValue = rotatingHead.transform;
            serObj.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            serObj.FindProperty("projectilePrefab").objectReferenceValue = cannonballPrefab;
            serObj.FindProperty("rangeIndicatorVisual").objectReferenceValue = rangeVis;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRunicTowerPrefab(Material matStone, Material matCyan, Material matRange, GameObject boltPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/RunicTower.prefab";
            string altPath = $"{PREFABS_DIR}/RunicTower.prefab";

            GameObject root = new GameObject("RunicTower");

            // Base de Basalto Escuro
            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.name = "RunicBase";
            basePlatform.transform.SetParent(root.transform);
            basePlatform.transform.localPosition = new Vector3(0, 0.2f, 0);
            basePlatform.transform.localScale = new Vector3(1.1f, 0.2f, 1.1f);
            basePlatform.GetComponent<Renderer>().sharedMaterial = matStone;
            Object.DestroyImmediate(basePlatform.GetComponent<Collider>());

            // Pilar Obelisco Rúnico
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "RunicPillar";
            pillar.transform.SetParent(root.transform);
            pillar.transform.localPosition = new Vector3(0, 0.6f, 0);
            pillar.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            pillar.GetComponent<Renderer>().sharedMaterial = matStone;
            Object.DestroyImmediate(pillar.GetComponent<Collider>());

            // Cristal Rúnico Flutuante e Rotativo (mira nos goblins)
            GameObject rotatingHead = new GameObject("RotatingHead");
            rotatingHead.transform.SetParent(root.transform);
            rotatingHead.transform.localPosition = new Vector3(0, 1.15f, 0);

            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "RunicCrystal";
            crystal.transform.SetParent(rotatingHead.transform);
            crystal.transform.localPosition = Vector3.zero;
            crystal.transform.localScale = new Vector3(0.48f, 0.65f, 0.48f);
            crystal.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(crystal.GetComponent<Collider>());

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(rotatingHead.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.1f, 0.5f);

            GameObject rangeVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeVis.name = "RangeVisual";
            rangeVis.transform.SetParent(root.transform);
            rangeVis.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeVis.transform.localScale = new Vector3(10.4f, 0.01f, 10.4f);
            rangeVis.GetComponent<Renderer>().sharedMaterial = matRange;
            Object.DestroyImmediate(rangeVis.GetComponent<Collider>());
            rangeVis.SetActive(false);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.4f;
            col.radius = 0.55f;
            col.center = new Vector3(0, 0.7f, 0);

            BaseDwarfTower tower = root.AddComponent<BaseDwarfTower>();
            SerializedObject serObj = new SerializedObject(tower);
            serObj.FindProperty("dwarfName").stringValue = "Ferreiro Runico";
            serObj.FindProperty("cost").intValue = 320;
            serObj.FindProperty("category").enumValueIndex = (int)TowerCategory.Runas;
            serObj.FindProperty("attackRange").floatValue = 5.2f;
            serObj.FindProperty("fireCooldown").floatValue = 1.1f;
            serObj.FindProperty("damage").intValue = 2;
            serObj.FindProperty("projectileSpeed").floatValue = 15f;
            serObj.FindProperty("rotatingPart").objectReferenceValue = rotatingHead.transform;
            serObj.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            serObj.FindProperty("projectilePrefab").objectReferenceValue = boltPrefab;
            serObj.FindProperty("rangeIndicatorVisual").objectReferenceValue = rangeVis;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGoldMineTowerPrefab(Material matWood, Material matGold, Material matRange)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/GoldMineTower.prefab";
            string altPath = $"{PREFABS_DIR}/GoldMineTower.prefab";

            GameObject root = new GameObject("GoldMineTower");

            // Fundação de madeira
            GameObject baseFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseFrame.name = "MineFoundation";
            baseFrame.transform.SetParent(root.transform);
            baseFrame.transform.localPosition = new Vector3(0, 0.2f, 0);
            baseFrame.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);
            baseFrame.GetComponent<Renderer>().sharedMaterial = matWood;
            Object.DestroyImmediate(baseFrame.GetComponent<Collider>());

            // Estrutura de suporte vertical (poço da mina)
            GameObject scaffold = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scaffold.name = "MineScaffold";
            scaffold.transform.SetParent(root.transform);
            scaffold.transform.localPosition = new Vector3(0, 0.65f, 0);
            scaffold.transform.localScale = new Vector3(0.75f, 0.7f, 0.75f);
            scaffold.GetComponent<Renderer>().sharedMaterial = matWood;
            Object.DestroyImmediate(scaffold.GetComponent<Collider>());

            // Depósito de pepitas de ouro puro
            GameObject goldDeposit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goldDeposit.name = "GoldOreVisual";
            goldDeposit.transform.SetParent(root.transform);
            goldDeposit.transform.localPosition = new Vector3(0, 1.05f, 0);
            goldDeposit.transform.localScale = new Vector3(0.55f, 0.22f, 0.55f);
            goldDeposit.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(goldDeposit.GetComponent<Collider>());

            GameObject rangeVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeVis.name = "RangeVisual";
            rangeVis.transform.SetParent(root.transform);
            rangeVis.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeVis.transform.localScale = new Vector3(5f, 0.01f, 5f);
            rangeVis.GetComponent<Renderer>().sharedMaterial = matRange;
            Object.DestroyImmediate(rangeVis.GetComponent<Collider>());
            rangeVis.SetActive(false);

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 1.2f, 1.2f);
            col.center = new Vector3(0, 0.6f, 0);

            GoldMineTower mine = root.AddComponent<GoldMineTower>();
            SerializedObject serObj = new SerializedObject(mine);
            serObj.FindProperty("towerName").stringValue = "Mina de Ouro dos Anoes";
            serObj.FindProperty("cost").intValue = 400;
            serObj.FindProperty("roundIncome").intValue = 80;
            serObj.FindProperty("rangeIndicatorVisual").objectReferenceValue = rangeVis;
            serObj.FindProperty("goldNuggetVisual").objectReferenceValue = goldDeposit;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateHeroThorinPrefab(Material matArmor, Material matBeard, Material matRange, GameObject hammerPrefab, GameObject warcryPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/HeroThorin.prefab";
            string altPath = $"{PREFABS_DIR}/HeroThorin.prefab";

            GameObject root = new GameObject("HeroThorin");

            // Corpo robusto de armadura dourada
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "HeroBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.45f, 0);
            body.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
            body.GetComponent<Renderer>().sharedMaterial = matArmor;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Cabeça rotativa com barba imponente e elmo
            GameObject rotatingHead = new GameObject("RotatingHead");
            rotatingHead.transform.SetParent(root.transform);
            rotatingHead.transform.localPosition = new Vector3(0, 0.85f, 0);

            GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            helmet.name = "HeroHelmet";
            helmet.transform.SetParent(rotatingHead.transform);
            helmet.transform.localPosition = new Vector3(0, 0.2f, 0);
            helmet.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
            helmet.GetComponent<Renderer>().sharedMaterial = matArmor;
            Object.DestroyImmediate(helmet.GetComponent<Collider>());

            GameObject beard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beard.name = "HeroBeard";
            beard.transform.SetParent(rotatingHead.transform);
            beard.transform.localPosition = new Vector3(0, -0.05f, 0.35f);
            beard.transform.localScale = new Vector3(0.48f, 0.45f, 0.32f);
            beard.GetComponent<Renderer>().sharedMaterial = matBeard;
            Object.DestroyImmediate(beard.GetComponent<Collider>());

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(rotatingHead.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.2f, 0.7f);

            GameObject rangeVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeVis.name = "RangeVisual";
            rangeVis.transform.SetParent(root.transform);
            rangeVis.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeVis.transform.localScale = new Vector3(10f, 0.01f, 10f);
            rangeVis.GetComponent<Renderer>().sharedMaterial = matRange;
            Object.DestroyImmediate(rangeVis.GetComponent<Collider>());
            rangeVis.SetActive(false);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.35f;
            col.radius = 0.55f;
            col.center = new Vector3(0, 0.65f, 0);

            DwarfHero hero = root.AddComponent<DwarfHero>();
            SerializedObject serObj = new SerializedObject(hero);
            serObj.FindProperty("heroName").stringValue = "Comandante Thorin";
            serObj.FindProperty("cost").intValue = 150;
            serObj.FindProperty("rotatingPart").objectReferenceValue = rotatingHead.transform;
            serObj.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            serObj.FindProperty("hammerProjectilePrefab").objectReferenceValue = hammerPrefab;
            serObj.FindProperty("warcryFXPrefab").objectReferenceValue = warcryPrefab;
            serObj.FindProperty("rangeIndicatorVisual").objectReferenceValue = rangeVis;
            serObj.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGoblinRecruitPrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/GoblinRecruit.prefab";
            string altPath = $"{PREFABS_DIR}/GoblinRecruit.prefab";

            GameObject root = new GameObject("GoblinRecruit");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "VisualBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            
            Renderer bodyRend = body.GetComponent<Renderer>();
            bodyRend.sharedMaterial = mat;
            bodyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            col.center = new Vector3(0, 0.6f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PathFollower>();
            BaseGoblin goblin = root.AddComponent<BaseGoblin>();
            goblin.SetStats(health: 3, speed: 3.5f, reward: 1, livesDamage: 1, color: new Color(0.20f, 0.85f, 0.20f));

            SerializedObject serRecruit = new SerializedObject(goblin);
            serRecruit.FindProperty("goblinName").stringValue = "Goblin Recruta";
            serRecruit.FindProperty("goblinColor").colorValue = new Color(0.20f, 0.85f, 0.20f);
            serRecruit.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGoblinRunnerPrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/GoblinRunner.prefab";
            string altPath = $"{PREFABS_DIR}/GoblinRunner.prefab";

            GameObject root = new GameObject("GoblinRunner");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "VisualBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.45f, 0);
            body.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            Renderer bodyRendRunner = body.GetComponent<Renderer>();
            bodyRendRunner.sharedMaterial = mat;
            bodyRendRunner.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.45f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PathFollower>();
            BaseGoblin goblin = root.AddComponent<BaseGoblin>();
            goblin.SetStats(health: 2, speed: 5.5f, reward: 1, livesDamage: 1, color: new Color(1.0f, 0.88f, 0.05f));

            SerializedObject serRunner = new SerializedObject(goblin);
            serRunner.FindProperty("goblinName").stringValue = "Goblin Corredor";
            serRunner.FindProperty("goblinColor").colorValue = new Color(1.0f, 0.88f, 0.05f);
            serRunner.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateGoblinArmoredPrefab(Material mat)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/GoblinArmored.prefab";
            string altPath = $"{PREFABS_DIR}/GoblinArmored.prefab";

            GameObject root = new GameObject("GoblinArmored");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "VisualBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.7f, 0);
            body.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Renderer bodyRendArmored = body.GetComponent<Renderer>();
            bodyRendArmored.sharedMaterial = mat;
            bodyRendArmored.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;
            col.center = new Vector3(0, 0.7f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PathFollower>();
            BaseGoblin goblin = root.AddComponent<BaseGoblin>();
            goblin.SetStats(health: 10, speed: 2.2f, reward: 4, livesDamage: 2, color: new Color(0.35f, 0.45f, 0.55f), armored: true);

            SerializedObject serArmored = new SerializedObject(goblin);
            serArmored.FindProperty("goblinName").stringValue = "Goblin Blindado";
            serArmored.FindProperty("goblinColor").colorValue = new Color(0.35f, 0.45f, 0.55f);
            serArmored.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTrollBossPrefab(Material mat, GameObject splitChildPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/TrollBoss.prefab";
            string altPath = $"{PREFABS_DIR}/TrollBoss.prefab";

            GameObject root = new GameObject("TrollBoss");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "VisualBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 1.2f, 0);
            body.transform.localScale = new Vector3(1.3f, 1.2f, 1.3f);

            Renderer bodyRendTroll = body.GetComponent<Renderer>();
            bodyRendTroll.sharedMaterial = mat;
            bodyRendTroll.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.0f;
            col.center = new Vector3(0, 1.2f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            root.AddComponent<PathFollower>();
            BaseGoblin goblin = root.AddComponent<BaseGoblin>();
            goblin.SetStats(health: 35, speed: 1.5f, reward: 25, livesDamage: 10, color: new Color(0.55f, 0.25f, 0.12f));
            goblin.SetSplitOnDeath(splitChildPrefab, count: 3);

            SerializedObject serTroll = new SerializedObject(goblin);
            serTroll.FindProperty("goblinName").stringValue = "Troll da Caverna";
            serTroll.FindProperty("goblinColor").colorValue = new Color(0.55f, 0.25f, 0.12f);
            serTroll.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, altPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetupScene(
            Material matGround,
            Material matPath,
            GameObject recruitPrefab,
            GameObject runnerPrefab,
            GameObject armoredPrefab,
            GameObject trollPrefab,
            GameObject dwarfPrefab,
            GameObject cannonTowerPrefab,
            GameObject runicTowerPrefab,
            GameObject goldMineTowerPrefab,
            GameObject heroPrefab)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path) && File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            }

            // 1. Chão
            GameObject ground = GameObject.Find("ArenaGround");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
            }
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2.4f, 1f, 1.8f);
            ground.GetComponent<Renderer>().sharedMaterial = matGround;

            // 2. Caminho e Waypoints
            GameObject pathObj = GameObject.Find("EnemyPath");
            if (pathObj == null)
            {
                pathObj = new GameObject("EnemyPath");
            }

            while (pathObj.transform.childCount > 0)
            {
                Object.DestroyImmediate(pathObj.transform.GetChild(0).gameObject);
            }

            Vector3[] routePoints = new Vector3[]
            {
                new Vector3(-11f, 0.1f, 4f),
                new Vector3(-5f,  0.1f, 4f),
                new Vector3(-5f,  0.1f, -3f),
                new Vector3(2f,   0.1f, -3f),
                new Vector3(2f,   0.1f, 3f),
                new Vector3(7f,   0.1f, 3f),
                new Vector3(7f,   0.1f, -8f),
            };

            for (int i = 0; i < routePoints.Length; i++)
            {
                GameObject wp = new GameObject($"Waypoint_{i:D2}");
                wp.transform.SetParent(pathObj.transform);
                wp.transform.position = routePoints[i];

                if (i < routePoints.Length - 1)
                {
                    Vector3 start = routePoints[i];
                    Vector3 end = routePoints[i + 1];
                    Vector3 center = (start + end) * 0.5f;
                    float len = Vector3.Distance(start, end);

                    GameObject roadPiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    roadPiece.name = $"Road_{i}";
                    roadPiece.transform.SetParent(pathObj.transform);
                    roadPiece.transform.position = center + Vector3.down * 0.45f;
                    roadPiece.transform.localScale = new Vector3(1.4f, 0.95f, len);
                    roadPiece.transform.rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
                    roadPiece.GetComponent<Renderer>().sharedMaterial = matPath;
                    Object.DestroyImmediate(roadPiece.GetComponent<Collider>());
                }
            }

            PathRoute pathRoute = pathObj.GetComponent<PathRoute>();
            if (pathRoute == null) pathRoute = pathObj.AddComponent<PathRoute>();
            pathRoute.AutoFindWaypointsFromChildren();

            // 3. Câmera Top-Down
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0f, 17f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            mainCam.fieldOfView = 50f;

            // 4. Luz do Sol com sombras suaves
            Light sunLight = Object.FindAnyObjectByType<Light>();
            if (sunLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                sunLight = lightObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }
            sunLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sunLight.color = new Color(1f, 0.96f, 0.88f);
            sunLight.intensity = 1.2f;
            sunLight.shadows = LightShadows.Soft;

            // 5. Game Managers & Object Pooling
            GameObject managersObj = GameObject.Find("--- MANAGERS ---");
            if (managersObj == null) managersObj = new GameObject("--- MANAGERS ---");

            if (managersObj.GetComponent<PoolManager>() == null) managersObj.AddComponent<PoolManager>();
            if (managersObj.GetComponent<GameManager>() == null) managersObj.AddComponent<GameManager>();

            WaveSpawner spawner = managersObj.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = managersObj.AddComponent<WaveSpawner>();
            spawner.SetPath(pathRoute);
            spawner.SetEnemyPrefabs(recruitPrefab, runnerPrefab, armoredPrefab, trollPrefab);

            SerializedObject serSpawner = new SerializedObject(spawner);
            serSpawner.FindProperty("pathRoute").objectReferenceValue = pathRoute;
            serSpawner.FindProperty("recruitPrefab").objectReferenceValue = recruitPrefab;
            serSpawner.FindProperty("runnerPrefab").objectReferenceValue = runnerPrefab;
            serSpawner.FindProperty("armoredPrefab").objectReferenceValue = armoredPrefab;
            serSpawner.FindProperty("trollBossPrefab").objectReferenceValue = trollPrefab;
            serSpawner.ApplyModifiedPropertiesWithoutUndo();

            TowerPlacementManager placement = managersObj.GetComponent<TowerPlacementManager>();
            if (placement == null) placement = managersObj.AddComponent<TowerPlacementManager>();
            SerializedObject serPlacement = new SerializedObject(placement);
            serPlacement.FindProperty("dwarfTowerPrefab").objectReferenceValue = dwarfPrefab;
            serPlacement.ApplyModifiedPropertiesWithoutUndo();

            GameHUD hud = managersObj.GetComponent<GameHUD>();
            if (hud == null) hud = managersObj.AddComponent<GameHUD>();
            SerializedObject serHUD = new SerializedObject(hud);
            serHUD.FindProperty("besteiroTowerPrefab").objectReferenceValue = dwarfPrefab;
            serHUD.FindProperty("cannonTowerPrefab").objectReferenceValue = cannonTowerPrefab;
            serHUD.FindProperty("runicTowerPrefab").objectReferenceValue = runicTowerPrefab;
            serHUD.FindProperty("goldMineTowerPrefab").objectReferenceValue = goldMineTowerPrefab;
            serHUD.FindProperty("heroPrefab").objectReferenceValue = heroPrefab;
            serHUD.ApplyModifiedPropertiesWithoutUndo();

            // 6. Posicionar um Anão inicial de teste ao lado do caminho
            GameObject testDwarf = GameObject.Find("InitialDwarf");
            if (testDwarf == null)
            {
                testDwarf = (GameObject)PrefabUtility.InstantiatePrefab(dwarfPrefab);
                testDwarf.name = "InitialDwarf";
                testDwarf.transform.position = new Vector3(-1.5f, 0.5f, 0.5f);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene, "Assets/Scenes/SampleScene.unity");
            }
            else
            {
                EditorSceneManager.SaveScene(activeScene);
            }
        }
    }
}
