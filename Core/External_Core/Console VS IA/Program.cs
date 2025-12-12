﻿namespace ConnectFour.Console;
using ConnectFour.Core;
using System;
using System.IO;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr create_ai();

    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr load_ai_text([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern int save_ai_text(IntPtr ai, [MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void destroy_ai(IntPtr ai);

    [DllImport("ml_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern int predict_scores(
        IntPtr ai,
        [In] double[] input,
        int input_len,
        [Out] double[] outScores,
        int out_len
    );

    private static readonly string ModelsDir = "models";

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
                    features[idx] = 1.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 0.0;
                }
                else if (disc.Symbol == 'X')
                {
                    features[idx] = 0.0;
                    features[idx + 1] = 1.0;
                    features[idx + 2] = 0.0;
                }
                else if (disc.Symbol == 'O')
                {
                    features[idx] = 0.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 1.0;
                }
                else
                {
                    features[idx] = 1.0;
                    features[idx + 1] = 0.0;
                    features[idx + 2] = 0.0;
                }

                idx += 3;
            }
        }

        char sym = currentPlayer.Disc.Symbol;
        if (sym == 'X')
        {
            features[idx] = 0.0;
            features[idx + 1] = 1.0;
            features[idx + 2] = 0.0;
        }
        else if (sym == 'O')
        {
            features[idx] = 0.0;
            features[idx + 1] = 0.0;
            features[idx + 2] = 1.0;
        }
        else
        {
            features[idx] = 1.0;
            features[idx + 1] = 0.0;
            features[idx + 2] = 0.0;
        }

        return features;
    }

    private static bool IsColumnFull(Board board, int col)
    {
        if (col < 0 || col >= Board.Cols) return true;
        return board.GetCell(0, col) != null;
    }

    private static int GetAiMoveBestValid(IntPtr ai, Board board, Player currentPlayer)
    {
        double[] features = EncodeBoard(board, currentPlayer);

        double[] scores = new double[Board.Cols];
        int ok = predict_scores(ai, features, features.Length, scores, scores.Length);

        if (ok == 0)
        {
            for (int c = 0; c < Board.Cols; c++)
                if (!IsColumnFull(board, c)) return c;
            return -1;
        }

        int[] cols = new int[Board.Cols];
        for (int i = 0; i < Board.Cols; i++) cols[i] = i;

        Array.Sort(cols, (a, b) =>
        {
            double sa = scores[a];
            double sb = scores[b];

            bool na = double.IsNaN(sa);
            bool nb = double.IsNaN(sb);
            if (na && nb) return 0;
            if (na) return 1;
            if (nb) return -1;

            return sb.CompareTo(sa); // descending
        });

        foreach (int c in cols)
        {
            if (!IsColumnFull(board, c))
                return c;
        }

        return -1;
    }

    private static string[] ListSavedModels()
    {
        if (!Directory.Exists(ModelsDir))
            return Array.Empty<string>();

        // Store names without extension
        var files = Directory.GetFiles(ModelsDir, "*.txt", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
            files[i] = Path.GetFileNameWithoutExtension(files[i]);

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static IntPtr SelectOrTrainModel()
    {
        Directory.CreateDirectory(ModelsDir);

        string[] models = ListSavedModels();

        if (models.Length > 0)
        {
            Console.WriteLine("Saved AI models found:");
            foreach (var m in models)
                Console.WriteLine($" - {m}");
            Console.WriteLine();
            Console.Write("Type a model name to load (or press Enter to train a new one): ");
            string? choice = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(choice))
            {
                string name = choice.Trim();
                string path = Path.Combine(ModelsDir, name + ".txt");

                IntPtr ai = load_ai_text(path);
                if (ai != IntPtr.Zero)
                {
                    Console.WriteLine($"Loaded model '{name}'.");
                    return ai;
                }

                Console.WriteLine($"Failed to load '{name}'. Training a new model instead...");
            }
        }

        Console.WriteLine("Training a new model...");
        return create_ai();
    }

    private static void PromptSaveModel(IntPtr ai)
    {
        Console.Write("Save this AI ? (Y/N): ");
        string? ans = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ans))
            return;

        char c = char.ToUpperInvariant(ans.Trim()[0]);
        if (c != 'Y')
            return;

        Console.Write("Model name (press Enter for timestamp): ");
        string? name = Console.ReadLine();
        name = (name ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
            name = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Basic sanitization for filenames
        foreach (char bad in Path.GetInvalidFileNameChars())
            name = name.Replace(bad, '_');

        Directory.CreateDirectory(ModelsDir);
        string path = Path.Combine(ModelsDir, name + ".txt");

        int ok = save_ai_text(ai, path);
        Console.WriteLine(ok == 1
            ? $"Saved model to '{path}'."
            : $"ERROR: failed to save model to '{path}'.");
    }

    static void Main()
    {
        var p1 = new Player("Player 1", 'X'); // Human
        var p2 = new Player("Bot O", 'O');    // AI

        var game = new Game(p1, p2);
        var renderer = new ConsoleRenderer(game.Board);
        var input = new InputHandler();

        IntPtr botAi = SelectOrTrainModel();

        bool gameEnded = false;

        try
        {
            bool running = true;
            while (running)
            {
                renderer.Draw();

                int col;

                if (game.CurrentPlayer == p2)
                {
                    col = GetAiMoveBestValid(botAi, game.Board, game.CurrentPlayer);
                    if (col < 0)
                    {
                        renderer.Draw();
                        Console.WriteLine("Draw! (No valid moves left)");
                        gameEnded = true;
                        break;
                    }

                    Console.WriteLine($"{game.CurrentPlayer.Name} chooses column {col}");
                }
                else
                {
                    col = input.GetColumnChoice(game.CurrentPlayer);
                }

                if (game.Board.PlaceDisc(col, game.CurrentPlayer.Disc, out int rowPlaced))
                {
                    if (Rules.CheckWin(game.Board, rowPlaced, col))
                    {
                        renderer.Draw();
                        Console.WriteLine($"{game.CurrentPlayer.Name} wins!");
                        gameEnded = true;
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
        finally
        {
            // Prompt save only if we actually reached end-of-game
            if (gameEnded)
                PromptSaveModel(botAi);

            destroy_ai(botAi);
        }
    }
}
