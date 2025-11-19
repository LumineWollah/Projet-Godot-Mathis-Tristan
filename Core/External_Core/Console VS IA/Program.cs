namespace ConnectFour.Console;
using ConnectFour.Core;
using System;
using System.Runtime.InteropServices;

class Program
{
    // Import from the Rust DLL.
    // Adjust "ml_rs" to the actual DLL name (without .dll).
    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern int predict_move([In] double[] input, int input_len);

    /// <summary>
    /// Encode the board + current player into a feature vector compatible
    /// with the Rust model.
    ///
    /// Encoding (per cell):
    ///   [1,0,0] = empty
    ///   [0,1,0] = 'X'
    ///   [0,0,1] = 'O'
    ///
    /// Then we append 3 values for the current player:
    ///   [0,1,0] if player is 'X'
    ///   [0,0,1] if player is 'O'
    ///
    /// Total length: Board.Rows * Board.Cols * 3 + 3
    /// </summary>
    private static double[] EncodeBoard(Board board, Player currentPlayer)
    {
        int cells = Board.Rows * Board.Cols;
        double[] features = new double[cells * 3 + 3];

        int idx = 0;
        for (int r = 0; r < Board.Rows; r++)
        {
            for (int c = 0; c < Board.Cols; c++)
            {
                Disc disc = board.GetCell(r, c);

                if (disc == null)
                {
                    // empty
                    features[idx]     = 1.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 0.0;
                }
                else if (disc.Symbol == 'X')
                {
                    features[idx]     = 0.0;
                    features[idx + 1] = 1.0;
                    features[idx + 2] = 0.0;
                }
                else if (disc.Symbol == 'O')
                {
                    features[idx]     = 0.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 1.0;
                }
                else
                {
                    // Par sécurité : considérer tout autre symbole comme vide
                    features[idx]     = 1.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 0.0;
                }

                idx += 3;
            }
        }

        // Encode current player as one-hot at the end
        char sym = currentPlayer.Disc.Symbol;
        if (sym == 'X')
        {
            features[idx]     = 0.0;
            features[idx + 1] = 1.0;
            features[idx + 2] = 0.0;
        }
        else
        {
            // assume 'O'
            features[idx]     = 0.0;
            features[idx + 1] = 0.0;
            features[idx + 2] = 1.0;
        }

        return features;
    }

    /// <summary>
    /// Ask the Rust model (our lib) which column O should play.
    /// Returns a column index in [0, Board.Cols - 1].
    /// </summary>
    private static int GetAiColumn(Board board, Player aiPlayer)
    {
        double[] input = EncodeBoard(board, aiPlayer);
        int col = predict_move(input, input.Length);

        // Clamp in case the model outputs something invalid
        if (col < 0) col = 0;
        if (col >= Board.Cols) col = Board.Cols - 1;

        return col;
    }

    static void Main()
    {
        var p1 = new Player("Player 1", 'X'); // Human
        var p2 = new Player("Bot O", 'O');    // Bot controlled by Rust model
        var game = new Game(p1, p2);

        var renderer = new ConsoleRenderer(game.Board);
        var input = new InputHandler();

        bool running = true;
        while (running)
        {
            renderer.Draw();

            int col;

            // Human plays as X
            if (game.CurrentPlayer.Disc.Symbol == 'X')
            {
                col = input.GetColumnChoice(game.CurrentPlayer);
            }
            else
            {
                // Bot (O) uses the Rust model
                col = GetAiColumn(game.Board, game.CurrentPlayer);
                Console.WriteLine($"Bot ({game.CurrentPlayer.Name}) chooses column {col + 1}");
            }

            if (game.Board.PlaceDisc(col, game.CurrentPlayer.Disc, out int row))
            {
                if (Rules.CheckWin(game.Board, row, col))
                {
                    renderer.Draw();
                    Console.WriteLine($"{game.CurrentPlayer.Name} wins!");
                    System.Threading.Thread.Sleep(3000);
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
