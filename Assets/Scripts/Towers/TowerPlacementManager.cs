using DwarvesTD.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DwarvesTD.Towers
{
    public class TowerPlacementManager : MonoBehaviour
    {
        public static TowerPlacementManager Instance { get; private set; }

        [Header("Tower Prefab To Place")]
        [SerializeField] private GameObject dwarfTowerPrefab;

        [Header("Placement Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask obstacleLayer;

        private GameObject previewInstance;
        private BaseDwarfTower previewTowerScript;
        private bool isPlacing = false;
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public void StartPlacement(GameObject towerPrefabToPlace = null)
        {
            if (towerPrefabToPlace != null)
            {
                dwarfTowerPrefab = towerPrefabToPlace;
            }

            if (dwarfTowerPrefab == null) return;

            // Regra do Heroi: Apenas 1 heroi por partida (estilo BTD6)
            DwarfHero heroData = dwarfTowerPrefab.GetComponent<DwarfHero>();
            if (heroData != null && DwarfHero.Instance != null)
            {
                Debug.LogWarning("[Placement] O Comandante Thorin ja esta liderando no campo! Limite de 1 Heroi por partida.");
                return;
            }

            int towerCost = GetPrefabCost(dwarfTowerPrefab);
            if (GameManager.Instance != null && !GameManager.Instance.CanAfford(towerCost))
            {
                Debug.LogWarning("[Placement] Ouro insuficiente para contratar!");
                return;
            }

            CancelPlacement();

            previewInstance = Instantiate(dwarfTowerPrefab);
            previewTowerScript = previewInstance.GetComponent<BaseDwarfTower>();

            // Desativa a lógica de ataque/produção do preview temporariamente
            if (previewTowerScript != null)
            {
                previewTowerScript.enabled = false;
                previewTowerScript.ShowRangeVisual(true);
            }

            GoldMineTower mineScript = previewInstance.GetComponent<GoldMineTower>();
            if (mineScript != null)
            {
                mineScript.enabled = false;
                mineScript.ShowRangeVisual(true);
            }

            DwarfHero heroScript = previewInstance.GetComponent<DwarfHero>();
            if (heroScript != null)
            {
                heroScript.enabled = false;
                heroScript.ShowRangeVisual(true);
            }

            // Desativa colisores do preview para não colidir com o próprio raio
            Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            isPlacing = true;
        }

        public void CancelPlacement()
        {
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            previewTowerScript = null;
            isPlacing = false;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (!isPlacing || previewInstance == null) return;

            Vector2 mouseScreenPos = GetMousePosition();
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                Vector3 placementPos = hit.point;
                placementPos.y = 0.5f; // Altura no chão
                previewInstance.transform.position = placementPos;

                bool isValidSpot = (groundLayer.value == 0 || ((1 << hit.collider.gameObject.layer) & groundLayer.value) != 0);

                if (IsLeftMouseClicked() && isValidSpot)
                {
                    PlaceTower(placementPos);
                }
            }

            if (IsRightMouseClicked() || IsCancelKeyPressed())
            {
                CancelPlacement();
            }
        }

        private void PlaceTower(Vector3 position)
        {
            if (dwarfTowerPrefab == null) return;

            int cost = GetPrefabCost(dwarfTowerPrefab);

            if (GameManager.Instance != null && !GameManager.Instance.SpendGold(cost))
            {
                Debug.LogWarning("[Placement] Ouro insuficiente para posicionar.");
                CancelPlacement();
                return;
            }

            GameObject placedTower = Instantiate(dwarfTowerPrefab, position, Quaternion.identity);

            BaseDwarfTower baseTower = placedTower.GetComponent<BaseDwarfTower>();
            if (baseTower != null)
            {
                baseTower.enabled = true;
                baseTower.ShowRangeVisual(false);
            }

            GoldMineTower mineTower = placedTower.GetComponent<GoldMineTower>();
            if (mineTower != null)
            {
                mineTower.enabled = true;
                mineTower.ShowRangeVisual(false);
            }

            DwarfHero heroTower = placedTower.GetComponent<DwarfHero>();
            if (heroTower != null)
            {
                heroTower.enabled = true;
                heroTower.ShowRangeVisual(false);
            }

            CancelPlacement();
        }

        private int GetPrefabCost(GameObject prefab)
        {
            if (prefab == null) return 100;
            BaseDwarfTower baseTower = prefab.GetComponent<BaseDwarfTower>();
            if (baseTower != null) return baseTower.Cost;

            GoldMineTower mine = prefab.GetComponent<GoldMineTower>();
            if (mine != null) return mine.Cost;

            DwarfHero hero = prefab.GetComponent<DwarfHero>();
            if (hero != null) return hero.Cost;

            return 100;
        }

        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private bool IsLeftMouseClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasPressedThisFrame;
            }
#endif
            return Input.GetMouseButtonDown(0);
        }

        private bool IsRightMouseClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.rightButton.wasPressedThisFrame;
            }
#endif
            return Input.GetMouseButtonDown(1);
        }

        private bool IsCancelKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown(KeyCode.Escape);
        }
    }
}
