namespace RIP.Services
{
    public sealed class PlayerProfile
    {
        public PlayerProfile(string playerId, string nickname, int elo)
        {
            PlayerId = playerId;
            Nickname = nickname;
            Elo = elo;
        }

        public string PlayerId { get; }
        public string Nickname { get; }
        public int Elo { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);

        public PlayerProfile WithElo(int elo)
        {
            return new PlayerProfile(PlayerId, Nickname, elo);
        }
    }
}
