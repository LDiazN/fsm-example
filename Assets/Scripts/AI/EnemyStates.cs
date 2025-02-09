namespace AI
{
    public class Patrol : IState
    {
        public void Execute(EnemyAgent agent)
        {
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Patrol)
            {
                agent.ChangeState(nextState);
                return;
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
    }
    
    public class Alert : IState
    {
        public void Execute(EnemyAgent agent)
        {
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Alert)
            {
                agent.ChangeState(nextState);
                return;
            }
            
        }
        
        private EnemyAgent.State GetNextState(EnemyAgent agent)
        {
            var perceptions = agent.GetPerceptions();
            if (!perceptions.canHearPlayer && !perceptions.canSeePlayer)
                return EnemyAgent.State.Patrol;
            if (perceptions.canSeePlayer)
                return EnemyAgent.State.Chase;

            return EnemyAgent.State.Alert;
        }
    }
    
    public class Chase : IState
    {
        public void Execute(EnemyAgent agent)
        {
            var nextState = GetNextState(agent);
            if (nextState != EnemyAgent.State.Alert)
            {
                agent.ChangeState(nextState);
                return;
            }
            
        }
        
        private EnemyAgent.State GetNextState(EnemyAgent agent)
        {
            var perceptions = agent.GetPerceptions();
            if (!perceptions.canSeePlayer && !perceptions.canHearPlayer)
                return EnemyAgent.State.Alert;

            return EnemyAgent.State.Chase;
        }
    }
}
