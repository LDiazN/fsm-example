using System.Runtime.CompilerServices;
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
            // If should change to a new state, 
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
            if (perceptions.canSeePlayer)
                return EnemyAgent.State.Chase;

            if (perceptions.canHearPlayer)
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
    
    public class Alert : IState
    {
        private float _timeSinceLastHeard;
        
        public void Execute(EnemyAgent agent)
        {
            // Check transitions before doing anything else
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Alert)
            {
                agent.ChangeState(nextState);
                return;
            }
            
            agent.NavMeshAgent.isStopped = true;
            
            _timeSinceLastHeard += Time.deltaTime;
            var perceptions = agent.GetPerceptions();
            if (perceptions.canHearPlayer)
                _timeSinceLastHeard = 0;
            
            // Look at the last position where the player was.
            // If the enemy is already in the same position as the last position, don't 
            // do look at because it can cause the enemy to look down the ground
            if ((agent.transform.position - perceptions.lastKnownPosition).sqrMagnitude > 1)
                agent.transform.LookAt(perceptions.lastKnownPosition);
        }
        
        private EnemyAgent.State GetNextState(EnemyAgent agent)
        {
            var perceptions = agent.GetPerceptions();
            if (!perceptions.canHearPlayer && !perceptions.canSeePlayer && _timeSinceLastHeard > agent.TimeBeforePatrol)
                return EnemyAgent.State.Patrol;
            if (perceptions.canSeePlayer)
                return EnemyAgent.State.Chase;

            return EnemyAgent.State.Alert;
        }
    }
    
    public class Chase : IState
    {

        // Time since the last time the enemy knew something new 
        // about the player
        private float _timeSinceLastKnown;
        public void Execute(EnemyAgent agent)
        {
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Chase)
            {
                agent.ChangeState(nextState);
                return;
            }
            
            // Chase the player as long as you see it 
            var perceptions = agent.GetPerceptions();
            Vector3 targetPosition = Vector3.zero;
            
            // If can see player, go to player
            if (perceptions.canSeePlayer)
                targetPosition = agent.Player.transform.position;
            else // go to last known position
                targetPosition = perceptions.lastKnownPosition;
            
            // Update time since last known: if it's too long, go to alert
            _timeSinceLastKnown += Time.deltaTime;
            if (perceptions.canHearPlayer || perceptions.canSeePlayer)
                _timeSinceLastKnown = 0;
            
            agent.NavMeshAgent.destination = targetPosition;
            
            // Stop if you are close to the player
            var distanceToPlayer = Vector3.SqrMagnitude(agent.transform.position - targetPosition);
            agent.NavMeshAgent.isStopped = distanceToPlayer < agent.ToleranceDistance;
        }
        
        private EnemyAgent.State GetNextState(EnemyAgent agent)
        {
            var perceptions = agent.GetPerceptions();
            if (!perceptions.canSeePlayer && !perceptions.canHearPlayer && _timeSinceLastKnown > agent.TimeBeforeAlert)
                return EnemyAgent.State.Alert;

            return EnemyAgent.State.Chase;
        }
    }
}
