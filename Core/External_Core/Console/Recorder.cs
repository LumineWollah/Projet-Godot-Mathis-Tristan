namespace ConnectFour.Console;
using ConnectFour.Core;

public class Recorder
{
    public void RecordState(Board board, int col, Player player)
    {
        // Saving state and move to csv
        using (var writer = new System.IO.StreamWriter("dataset.csv", true))
        {
            string move;
            if (player.Disc.Symbol == 'X')
            {
                move = "0,1,0";
            }
            else if (player.Disc.Symbol == 'O')
            {
                move = "0,0,1";
            }
            else
            {
                move = null;
            }

            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    var cell = board.GetCell(r, c);
                    if (cell == null)
                    {
                        writer.Write("1,0,0");
                    }
                    else if (cell.Symbol == 'X')
                    {
                        writer.Write("0,1,0");
                    }
                    else if(cell.Symbol == 'O')
                    {
                        writer.Write("0,0,1");
                    }
                    if (c < Board.Cols - 1)
                        writer.Write(",");
                }
                writer.WriteLine();
            }
            writer.WriteLine($"{move},{col}");
            writer.WriteLine(); // Blank line to separate moves
        }
    }
}