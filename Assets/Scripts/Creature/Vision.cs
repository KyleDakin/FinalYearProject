// Code adapted from tutorial found here https://www.youtube.com/watch?v=rQG9aUWarwE&t=1094s
// Covered by MIT License

using System;
using System.Collections.Generic;
using Manager_Scripts;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Creature
{
    public class Vision : MonoBehaviour
    {
        public float visionRange;
        public float viewAngle;

        public LayerMask targetMask;
        public LayerMask obstacleMask;

        public List<Transform> _visibleTargets = new List<Transform>();
        private Species _targetSpecies;

        public GameObject SearchFor(Species entity)
        {
            _targetSpecies = entity;
            return LookForTarget();
        }

        private GameObject LookForTarget()
        {
            Transform finalTarget = null;
            _visibleTargets.Clear();
            Collider[] targetsInRange = Physics.OverlapSphere(transform.position, visionRange, targetMask);
            for (int i = 0; i < targetsInRange.Length; i++)
            {
                Transform target = targetsInRange[i].transform;
                if (target.CompareTag(_targetSpecies.ToString()))
                {
                    Vector3 dirToTarget = (target.position - transform.position).normalized;
                    if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle/2)
                    {
                        float distToTarget = Vector3.Distance(transform.position, target.position);
                        if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                        {
                            _visibleTargets.Add(target);
                        }
                    }
                }
            }

            if (_visibleTargets.Count > 0)
            {
                float minDistance = float.MaxValue;
                Vector3 targetPos = Vector3.zero;
                foreach (Transform target in _visibleTargets)
                {
                    finalTarget = (minDistance > Vector3.Distance(transform.position, target.position))
                        ? target
                        : finalTarget;
                    minDistance = (minDistance > Vector3.Distance(transform.position, target.position))
                        ? Vector3.Distance(transform.position, target.position)
                        : minDistance;
                }
            }

            if(finalTarget != null) return finalTarget.gameObject;

            return null;
        }

        public bool FoodStillThere(Transform target)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 GenerateWanderPoint()
        {
            Vector3 direction = Random.insideUnitSphere * visionRange;
            direction += transform.position;
            NavMeshHit navMeshHit;
            NavMesh.SamplePosition(direction, out navMeshHit, visionRange,-1);
            return navMeshHit.position;
        }

        public Vector3 GenerateFleePoint(Vector3 predatorPosition)
        {
            Vector3 direction = predatorPosition - transform.position;
            NavMeshHit navMeshHit;
            NavMesh.SamplePosition(-direction, out navMeshHit, visionRange, -1);
            return navMeshHit.position;
        }
    }
}
