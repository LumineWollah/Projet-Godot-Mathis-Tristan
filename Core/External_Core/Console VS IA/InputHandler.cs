using ConnectFour.Core;

public class InputHandler
{
    public int GetColumnChoice(Player player)
    {
        int col;
        while (true)
        {
            Console.Write($"{player.Name} ({player.Disc.Symbol}), choose a column (0-6): ");
            string input = Console.ReadLine();
            if (int.TryParse(input, out col) && col >= 0 && col < Board.Cols)
                return col;
            Console.WriteLine("Invalid input. Try again.");
        }
    }
}