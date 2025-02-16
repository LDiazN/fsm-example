using UnityEngine;

namespace AI
{
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