namespace ConnectFour.Core
{
    public class Player
    {
        public string Name { get; }
        public Disc Disc { get; }

        public Player(string name, char symbol)
        {
            Name = name;
            Disc = new Disc(symbol);
        }
    }
}
