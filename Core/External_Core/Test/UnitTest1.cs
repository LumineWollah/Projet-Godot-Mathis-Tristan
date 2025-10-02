using ConnectFour.Core;
using ConnectFour.Logic;

namespace ConnectFour.Tests
{
    public class GameTests
    {
        [Test]
        public void Player1WinsHorizontally()
        {
            var p1 = new Player("P1", 'X');
            var p2 = new Player("P2", 'O');
            var game = new Game(p1, p2);

            // Place 4 discs in row 5, cols 0-3
            for (int col = 0; col < 4; col++)
            {
                game.Board.PlaceDisc(col, p1.Disc, out int row);
            }

            bool win = Rules.CheckWin(game.Board, 5, 3);
            Assert.True(win, "Player1 should have won with 4 in a row horizontally.");
        }
        
        [Test]
        public void FullGameSimulation()
        {
            var p1 = new Player("P1", 'X');
            var p2 = new Player("P2", 'O');
            var game = new Game(p1, p2);

            // Simulate moves
            game.Board.PlaceDisc(0, p1.Disc, out _);
            game.Board.PlaceDisc(0, p2.Disc, out _);
            game.Board.PlaceDisc(1, p1.Disc, out _);
            game.Board.PlaceDisc(1, p2.Disc, out _);
            game.Board.PlaceDisc(2, p1.Disc, out _);
            game.Board.PlaceDisc(2, p2.Disc, out _);
            game.Board.PlaceDisc(3, p1.Disc, out int row);

            Assert.True(Rules.CheckWin(game.Board, row, 3), "P1 should win with 4 in a row.");
        }

    }
}