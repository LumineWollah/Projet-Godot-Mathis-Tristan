namespace ConnectFour.Console;
using ConnectFour.Core;
using System;

public class ConsoleRenderer
{
    private Board board;

    public ConsoleRenderer(Board board) => this.board = board;

    public void Draw()
    {
        Console.Clear();
        for (int r = 0; r < Board.Rows; r++)
        {
            for (int c = 0; c < Board.Cols; c++)
            {
                var disc = board.GetCell(r, c);
                Console.Write(disc == null ? ". " : disc.Symbol + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("0 1 2 3 4 5 6");
    }
}