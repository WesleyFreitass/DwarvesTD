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
            if (!File.Exists(recruitPath) || !File.Exists(cannonPath))
            {
                Debug.Log("[DwarvesTDEditor] Prefabs ausentes detectados. Gerando automaticamente todos os prefabs...");
                GeneratePrefabsAndMaterialsInternal();
            }
        }

        [MenuItem("Dwarves TD/Gerar Arena de Teste Jogvel", false, 1)]
        public static void GeneratePlayableArena()
        {
            GeneratedAssets assets = GeneratePrefabsAndMaterialsInternal();

            // 3. Montar a Cena
            SetupScene(assets.matGround, assets.matPath, assets.recruitPrefab, assets.runnerPrefab, assets.armoredPrefab, assets.trollBossPrefab, assets.dwarfPrefab, assets.cannonTowerPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Dwarves TD - Arena Ultra Otimizada!",
                "Arena gerada com todas as otimizacoes de FPS ativas!\n\n" +
                "- MaterialPropertyBlock (Zero clones de material)\n" +
                "- Rigidbody Kinematic (Zero overhead de PhysX)\n" +
                "- Calculos de distancia otimizados sem raizes quadradas\n" +
                "- Novo Canhoneiro de Polvora (AoE / Dano em Area)\n" +
                "- Cache de texto na HUD\n\n" +
                "De o PLAY e sinta a fluidez das hordas a 60+ FPS!",
                "Excelente!"
            );
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;

                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;

                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static Material GetOrCreateTransparentMaterial(string name, Color color)
        {
            string path = $"{MATERIALS_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;

                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                EditorUtility.SetDirty(mat);
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
            serObj.FindProperty("attackRange").floatValue = 4.5f;
            serObj.FindProperty("fireCooldown").floatValue = 0.8f;
            serObj.FindProperty("damage").intValue = 1;
            serObj.FindProperty("cost").intValue = 120;
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

        private static GameObject CreateCannonTowerPrefab(Material matBase, Material matBarrel, Material matRange, GameObject cannonballPrefab)
        {
            string path = $"{RESOURCES_PREFABS_DIR}/CannonTower.prefab";
            string altPath = $"{PREFABS_DIR}/CannonTower.prefab";

            GameObject root = new GameObject("CannonTower");

            // Base de Pedra/Ferro escuro reforçada
            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.name = "CannonBase";
            basePlatform.transform.SetParent(root.transform);
            basePlatform.transform.localPosition = new Vector3(0, 0.22f, 0);
            basePlatform.transform.localScale = new Vector3(1.1f, 0.22f, 1.1f);
            basePlatform.GetComponent<Renderer>().sharedMaterial = matBase;
            Object.DestroyImmediate(basePlatform.GetComponent<Collider>());

            // Torreta rotativa (mira no goblin)
            GameObject rotatingHead = new GameObject("RotatingHead");
            rotatingHead.transform.SetParent(root.transform);
            rotatingHead.transform.localPosition = new Vector3(0, 0.45f, 0);

            // Suporte do canhao
            GameObject mount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mount.name = "Mount";
            mount.transform.SetParent(rotatingHead.transform);
            mount.transform.localPosition = new Vector3(0, 0.15f, 0);
            mount.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
            mount.GetComponent<Renderer>().sharedMaterial = matBase;
            Object.DestroyImmediate(mount.GetComponent<Collider>());

            // Cano do canhao inclinado
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

        private static void SetupScene(Material matGround, Material matPath, GameObject recruitPrefab, GameObject runnerPrefab, GameObject armoredPrefab, GameObject trollPrefab, GameObject dwarfPrefab, GameObject cannonTowerPrefab)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            // 1. Chao
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

            // 3. Camera Top-Down
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
            serHUD.ApplyModifiedPropertiesWithoutUndo();

            // 6. Posicionar um Anao inicial de teste ao lado do caminho
            GameObject testDwarf = GameObject.Find("InitialDwarf");
            if (testDwarf == null)
            {
                testDwarf = (GameObject)PrefabUtility.InstantiatePrefab(dwarfPrefab);
                testDwarf.name = "InitialDwarf";
                testDwarf.transform.position = new Vector3(-1.5f, 0.5f, 0.5f);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }
    }
}
