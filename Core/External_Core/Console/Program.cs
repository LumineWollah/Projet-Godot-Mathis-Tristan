namespace ConnectFour.Console;
using ConnectFour.Core;
using System;

class Program
{
    static void Main()
    {
        var p1 = new Player("Player 1", 'X');
        var p2 = new Player("Player 2", 'O');
        var game = new Game(p1, p2);

        var renderer = new ConsoleRenderer(game.Board);
        var input = new InputHandler();

        bool running = true;
        while (running)
        {
            renderer.Draw();
            int col = input.GetColumnChoice(game.CurrentPlayer);

            if (game.Board.PlaceDisc(col, game.CurrentPlayer.Disc, out int row))
            {
                if (Rules.CheckWin(game.Board, row, col))
                {
                    renderer.Draw();
                    Console.WriteLine($"{game.CurrentPlayer.Name} wins!");
                    running = false;
                }
                else
                {
                    game.SwitchTurn();
                }
            }
            else
            {
                Console.WriteLine("Column full. Try again.");
            }
        }
    }
}