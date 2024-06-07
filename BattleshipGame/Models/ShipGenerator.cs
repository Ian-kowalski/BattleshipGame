using System;
using System.Collections.Generic;

namespace BattleshipGame.Models
{
    public static class ShipGenerator
    {
        private static readonly List<int> ShipSizes = new List<int> { 5, 4, 3, 3, 2 };
        private static Random random = new Random();

        private static bool CanPlaceShip(GameBoard board, int size, bool isVertical, int x, int y)
        {
            if (isVertical)
            {
                if (x + size > GameBoard.BoardSize) return false;
                for (int i = 0; i < size; i++)
                {
                    if (board.Cells[x + i, y] != CellState.Empty) return false;
                }
            }
            else
            {
                if (y + size > GameBoard.BoardSize) return false;
                for (int i = 0; i < size; i++)
                {
                    if (board.Cells[x, y + i] != CellState.Empty) return false;
                }
            }
            return true;
        }

        private static void PlaceShip(GameBoard board, int size, bool isVertical, int x, int y)
        {
            if (isVertical)
            {
                for (int i = 0; i < size; i++)
                {
                    board.Cells[x + i, y] = CellState.Ship;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    board.Cells[x, y + i] = CellState.Ship;
                }
            }
        }

        public static void GenerateShips(GameBoard board)
        {
            foreach (int size in ShipSizes)
            {
                bool placed = false;
                while (!placed)
                {
                    bool isVertical = random.Next(2) == 0;
                    int x = random.Next(GameBoard.BoardSize);
                    int y = random.Next(GameBoard.BoardSize);

                    if (CanPlaceShip(board, size, isVertical, x, y))
                    {
                        PlaceShip(board, size, isVertical, x, y);
                        placed = true;
                    }
                }
            }
        }
    }
}
