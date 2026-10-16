using System;
using UnityEngine;

namespace DwarvesTD.Pathfinding
{
    public class PathFollower : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private float rotationSpeed = 12f;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float DistanceTraveled { get; private set; }
        public bool HasReachedEnd { get; private set; }
        public PathRoute CurrentPath => currentPath;
        public int CurrentWaypointIndex => currentWaypointIndex;

        public event Action OnReachedEnd;

        private PathRoute currentPath;
        private int currentWaypointIndex = 0;
        private Quaternion targetRotation = Quaternion.identity;

        public void Initialize(PathRoute path, float moveSpeed)
        {
            currentPath = path;
            speed = moveSpeed;
            currentWaypointIndex = 0;
            DistanceTraveled = 0f;
            HasReachedEnd = false;

            if (currentPath != null && currentPath.WaypointCount > 0)
            {
                transform.position = currentPath.GetWaypoint(0).position;
                if (currentPath.WaypointCount > 1)
                {
                    Vector3 dir = (currentPath.GetWaypoint(1).position - transform.position);
                    dir.y = 0;
                    if (dir != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        transform.rotation = targetRotation;
                    }
                }
            }
        }

        public void InitializeAtPoint(PathRoute path, float moveSpeed, Vector3 spawnPosition, int waypointIndex, float distance)
        {
            currentPath = path;
            speed = moveSpeed;
            currentWaypointIndex = waypointIndex;
            DistanceTraveled = distance;
            HasReachedEnd = false;

            transform.position = spawnPosition;
            if (currentPath != null && waypointIndex < currentPath.WaypointCount)
            {
                Vector3 dir = (currentPath.GetWaypoint(waypointIndex).position - spawnPosition);
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    transform.rotation = targetRotation;
                }
            }
        }

        private void Update()
        {
            if (HasReachedEnd || currentPath == null || currentPath.WaypointCount == 0) return;

            Transform targetWaypoint = currentPath.GetWaypoint(currentWaypointIndex);
            if (targetWaypoint == null) return;

            Vector3 currentPos = transform.position;
            Vector3 targetPos = targetWaypoint.position;
            targetPos.y = currentPos.y;

            Vector3 moveDirection = (targetPos - currentPos);
            float sqrDistToWp = moveDirection.sqrMagnitude;
            float stepDistance = speed * Time.deltaTime;

            // Evita Vector3.Distance pesado usando sqrMagnitude
            if (sqrDistToWp <= stepDistance * stepDistance)
            {
                transform.position = targetPos;
                currentWaypointIndex++;
                DistanceTraveled += stepDistance;

                if (currentWaypointIndex >= currentPath.WaypointCount)
                {
                    HasReachedEnd = true;
                    OnReachedEnd?.Invoke();
                    return;
                }

                // Ao alcancar o waypoint, ja calcula a direcao do proximo para virar sem lag
                Transform nextWp = currentPath.GetWaypoint(currentWaypointIndex);
                if (nextWp != null)
                {
                    Vector3 nextDir = (nextWp.position - targetPos);
                    nextDir.y = 0;
                    if (nextDir != Vector3.zero)
                    {
                        targetRotation = Quaternion.LookRotation(nextDir.normalized, Vector3.up);
                    }
                }
            }
            else
            {
                Vector3 normDir = moveDirection.normalized;
                transform.position += normDir * stepDistance;
                DistanceTraveled += stepDistance;

                if (normDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * 40f * Time.deltaTime);
                }
            }
        }
    }
}
