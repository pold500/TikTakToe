using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Game
{
    class GameAI
    {
        GridLogic m_gridLogic;

        public GameAI(GridLogic gridLogic)
        {
            m_gridLogic = gridLogic;
        }

        public void AI_MakeMove(List<GridCell> cellLogicList)
        {
            //must be 8 at total
            var rows = Helpers.generateRows(cellLogicList);
            //there're two strategies here: defense and offense.
            //as offense for now we just consider random hit if there's no rows we should likely fill (rows that contain two ai points),
            //or filling up a row where we could possibly win and that have at least one mark already.

            //So, our priorities is like this:
            //if there's a player row with two marks, we "break it", by setting our third mark
            //if there's a row with 2 marks which we can fill in, we step in and set 3 mark and win
            //else, if there's a row with 1 AI mark and we can fill in, we fill it in
            //else, just place a random hit in free cell
            var twoPlayerMarks = findRowWithTwoUserMarksAndEmptySlot(rows);
            var onePlayerMark = findRowWithOneUserMarkAndEmptySlot(rows);
            var twoAIMarks = findRowWithTwoAIMarksAndEmptySlot(rows);

            int middleCellIndex = cellLogicList.Count() / 2;
            bool centerIsEmpty = cellLogicList[middleCellIndex].CellState == GridCell.State.Empty;
            if (centerIsEmpty)
            {
                cellLogicList[middleCellIndex].CellState = GridCell.State.AI;
                m_gridLogic.AddMarkToGrid(cellLogicList[middleCellIndex], GridLogic.MarkType.AI);
            }
            else if (twoAIMarks.GetList() != null)
            {
                Debug.Log("Ai is twoAIMarks!");
                AI_CompleteRow(twoAIMarks);
            }
            else if (twoPlayerMarks.GetList() != null)
            {
                Debug.Log("Ai is twoPlayerMarks!");
                AI_CompleteRow(twoPlayerMarks);
            }
            else if (onePlayerMark.GetList() != null)
            {
                Debug.Log("Ai is onePlayerMark!");
                AI_CompleteRow(onePlayerMark);
            }
        }

        private Row findRowWithTwoAIMarksAndEmptySlot(List<List<GridCell>> rows)
        {
            return rows.Find(row => {
                return row.Count(rowCell => rowCell.CellState == GridCell.State.AI) == 2 &&
                      row.Count(rowCell => rowCell.CellState == GridCell.State.Empty) >= 1;
            });
        }

        private Row findRowWithTwoUserMarksAndEmptySlot(List<List<GridCell>> rows)
        {
            //print full grid
            Debug.Log("Printing full grid start");
            rows.ForEach(row => row.ForEach(cell => Debug.Log(cell.CellState.ToString())));
            Debug.Log("Printing full grid end");
            int i = 0;
            return rows.Find(row => {
                Debug.Log("RowWithTwo " + i + " count " + row.Count(rowCell => rowCell.CellState == GridCell.State.Player));
                i++;
                return
                row.Count(rowCell => rowCell.CellState == GridCell.State.Player) > 1 &&
                row.Count(rowCell => rowCell.CellState == GridCell.State.Empty) >= 1;
            });
        }

        private Row findRowWithOneUserMarkAndEmptySlot(List<List<GridCell>> rows)
        {
            int count = 1;
            if (count >= 1)
            {
                Debug.Log("111");
            }
            return rows.Find(row => {
                return (row.Count(rowCell => rowCell.CellState == GridCell.State.Player) == 1) &&
                      (row.Count(rowCell => rowCell.CellState == GridCell.State.Empty) >= 1);
            });
        }

        private Row findRowAtLeastOneEmptyCell(List<List<GridCell>> rows)
        {
            return new Row(rows.Find(row => { return row.Count(cell => cell.CellState == GridCell.State.Empty) >= 1; }));
        }

        private bool AI_CompleteRow(Row row)
        {
            //find first empty cell and busy it
            var cell = row.GetList().Find(cell_ => { return cell_.CellState == GridCell.State.Empty; });
            if (cell != null)
            {
                cell.CellState = GridCell.State.AI;
                //DrawAICell(cell);
                m_gridLogic.AddMarkToGrid(cell, GridLogic.MarkType.AI);
                return true;
            }
            else //no empty cells!
            {
                return false; //gimme another row!
            }
        }
    }
}
