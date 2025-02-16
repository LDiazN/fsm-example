using UnityEngine;

namespace AI
{
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
}