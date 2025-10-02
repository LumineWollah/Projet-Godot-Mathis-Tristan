namespace ConnectFour.Core
{
    public class Board
    {
        public const int Rows = 6;
        public const int Cols = 7;

        private Disc[,] grid = new Disc[Rows, Cols];

        public bool PlaceDisc(int col, Disc disc, out int rowPlaced)
        {
            rowPlaced = -1;
            if (col < 0 || col >= Cols) return false;

            for (int row = Rows - 1; row >= 0; row--)
            {
                if (grid[row, col] == null)
                {
                    grid[row, col] = disc;
                    rowPlaced = row;
                    return true;
                }
            }
            return false; // column full
        }

        public Disc GetCell(int row, int col) => grid[row, col];
    }
}
