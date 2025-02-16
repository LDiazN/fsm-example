using UnityEngine;

namespace AI
{
    public class Patrol : IState
    {
        private int _currentWaypoint;

        private enum PatrolState
        {
            Waiting,
            Moving,
        }
        private PatrolState _state = PatrolState.Waiting;
        private float _waitTimer;
        
        public void Execute(EnemyAgent agent)
        {
            // If you should change to a new state, 
            // change and return
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Patrol)
            {
                agent.ChangeState(nextState);
                return;
            }

            switch (_state)
            {
                case PatrolState.Waiting:
                    UpdateWaiting(agent);
                    break;
                case PatrolState.Moving:
                    UpdateMoving(agent);
                    break;
            }
        }

        private EnemyAgent.State GetNextState(EnemyAgent agent)
        {
            var perceptions = agent.GetPerceptions();
            if (perceptions.CanSeePlayer)
                return EnemyAgent.State.Chase;

            if (perceptions.CanHearPlayer)
                return EnemyAgent.State.Alert;

            return EnemyAgent.State.Patrol;
        }

        private void UpdateWaiting(EnemyAgent agent)
        {
            // If no waypoints, just stay here 
            if (agent.Waypoints.Count == 0)
                return;
            
            // Waited enough, reset timer, update waypoint and move
            if (_waitTimer > agent.TimeToWait)
            {
                _waitTimer = 0;
                _currentWaypoint = (_currentWaypoint + 1) % agent.Waypoints.Count;
                StartMoving(agent);
                return;
            }
            
                
            _waitTimer += Time.deltaTime;
        }
        
        private Vector3 GetCurrentWaypointPosition(EnemyAgent agent) => agent.Waypoints[_currentWaypoint].position;

        private void UpdateMoving(EnemyAgent agent)
        {
            var currentPosition = GetCurrentWaypointPosition(agent);
            agent.NavMeshAgent.destination = currentPosition;
            agent.NavMeshAgent.isStopped = false;
            
            var distanceToWaypoint = (currentPosition - agent.transform.position).sqrMagnitude;
            if (distanceToWaypoint < agent.ToleranceDistance * agent.ToleranceDistance)
            {
                StartWaiting(agent);
            }
        }

        private void StartMoving(EnemyAgent agent)
        {
            _state = PatrolState.Moving;
            agent.NavMeshAgent.isStopped = false;
        }

        private void StartWaiting(EnemyAgent agent)
        {
            _state = PatrolState.Waiting;
            agent.NavMeshAgent.isStopped = true;

            // The watcher enemy
            if (agent.Waypoints.Count == 1)
            {
                agent.transform.rotation = Quaternion.Euler(0, agent.OriginalRotation.eulerAngles.y, 0);
            }
        }
    }
}