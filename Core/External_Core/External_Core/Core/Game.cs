namespace ConnectFour.Core
{
    public class Game
    {
        public Board Board { get; }
        public Player Player1 { get; }
        public Player Player2 { get; }
        public Player CurrentPlayer { get; private set; }

        public Game(Player p1, Player p2)
        {
            Board = new Board();
            Player1 = p1;
            Player2 = p2;
            CurrentPlayer = p1;
        }

        public void SwitchTurn()
        {
            CurrentPlayer = (CurrentPlayer == Player1) ? Player2 : Player1;
        }
    }
}
