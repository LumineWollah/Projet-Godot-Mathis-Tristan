namespace ConnectFour.Core
{
    public static class Rules
    {
        private static readonly (int, int)[] Directions =
        {
            (0,1), (1,0), (1,1), (1,-1)
        };

        public static bool CheckWin(Board board, int row, int col)
        {
            Disc disc = board.GetCell(row, col);
            if (disc == null) return false;

            foreach (var (dr, dc) in Directions)
            {
                int count = 1;
                count += CountDirection(board, disc, row, col, dr, dc);
                count += CountDirection(board, disc, row, col, -dr, -dc);
                if (count >= 4) return true;
            }

            return false;
        }

        private static int CountDirection(Board board, Disc disc, int row, int col, int dr, int dc)
        {
            int count = 0;
            for (int r = row + dr, c = col + dc;
                 r >= 0 && r < Board.Rows && c >= 0 && c < Board.Cols;
                 r += dr, c += dc)
            {
                if (board.GetCell(r, c)?.Symbol == disc.Symbol)
                    count++;
                else break;
            }
            return count;
        }
    }
}
