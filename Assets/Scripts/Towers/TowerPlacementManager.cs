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

            BaseDwarfTower towerData = dwarfTowerPrefab.GetComponent<BaseDwarfTower>();
            if (towerData != null && GameManager.Instance != null && !GameManager.Instance.CanAfford(towerData.Cost))
            {
                Debug.LogWarning("[Placement] Ouro insuficiente para contratar este ano!");
                return;
            }

            CancelPlacement();

            previewInstance = Instantiate(dwarfTowerPrefab);
            previewTowerScript = previewInstance.GetComponent<BaseDwarfTower>();

            // Desativa a lgica de ataque do preview temporariamente
            if (previewTowerScript != null)
            {
                previewTowerScript.enabled = false;
                previewTowerScript.ShowRangeVisual(true);
            }

            // Desativa colisores do preview para no colidir com o prprio raio
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
            isPlacing = false;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (!isPlacing) return;

            Vector2 mouseScreenPos = GetMousePosition();
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                Vector3 placementPos = hit.point;
                placementPos.y = 0.5f; // Altura no cho
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

            BaseDwarfTower towerData = dwarfTowerPrefab.GetComponent<BaseDwarfTower>();
            int cost = towerData != null ? towerData.Cost : 100;

            if (GameManager.Instance != null && !GameManager.Instance.SpendGold(cost))
            {
                Debug.LogWarning("[Placement] Ouro insuficiente para posicionar.");
                CancelPlacement();
                return;
            }

            GameObject placedTower = Instantiate(dwarfTowerPrefab, position, Quaternion.identity);
            BaseDwarfTower script = placedTower.GetComponent<BaseDwarfTower>();
            if (script != null)
            {
                script.enabled = true;
                script.ShowRangeVisual(false);
            }

            CancelPlacement();
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
