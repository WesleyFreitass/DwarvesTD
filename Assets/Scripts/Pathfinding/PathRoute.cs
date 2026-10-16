using System.Collections.Generic;
using UnityEngine;

namespace DwarvesTD.Pathfinding
{
    public class PathRoute : MonoBehaviour
    {
        [Tooltip("Lista ordenada de Waypoints por onde os goblins caminham.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        public IReadOnlyList<Transform> Waypoints => waypoints;
        public int WaypointCount => waypoints.Count;

        public Transform GetWaypoint(int index)
        {
            if (index >= 0 && index < waypoints.Count)
            {
                return waypoints[index];
            }
            return null;
        }

        public void AutoFindWaypointsFromChildren()
        {
            waypoints.Clear();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Waypoint"))
                {
                    waypoints.Add(child);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.8f);

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 0.35f);

                if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }
    }
}
