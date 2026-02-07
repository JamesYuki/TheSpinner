using PurrNet;

namespace Spinner
{
    /// <summary>
    /// プレイヤーを交互に2チームに割り当てる戦略
    /// プレイヤー0,2,4... -> チーム1
    /// プレイヤー1,3,5... -> チーム2
    /// </summary>
    public class AlternateTeamAssignmentStrategy : ITeamAssignmentStrategy
    {
        public (int teamId, int teamPlayerIndex) GetTeamAssignment(int playerIndex, PlayerID playerId)
        {
            int teamId = (playerIndex % 2) + 1; // 1 or 2
            int teamPlayerIndex = playerIndex / 2;
            return (teamId, teamPlayerIndex);
        }
    }
}
