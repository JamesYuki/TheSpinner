using PurrNet;

namespace Spinner
{
    /// <summary>
    /// プレイヤーへのチーム割り当て戦略のインターフェース
    /// </summary>
    public interface ITeamAssignmentStrategy
    {
        /// <summary>
        /// プレイヤーのインデックスからチームIDとチーム内インデックスを取得
        /// </summary>
        /// <param name="playerIndex">全体でのプレイヤーインデックス</param>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>(teamId, teamPlayerIndex)</returns>
        (int teamId, int teamPlayerIndex) GetTeamAssignment(int playerIndex, PlayerID playerId);
    }
}
