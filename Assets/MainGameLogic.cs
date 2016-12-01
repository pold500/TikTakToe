using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class MainGameLogic : MonoBehaviour
{
    private GameObject m_playerXPrefab;
    private GameObject m_gridCell;
    private Vector3 m_cellSize;
    private GameObject[,] m_cellObjectArray;
    private List<GridCell> m_gridCells;
    private GameObject m_enemyOPrefab;
    private enum GameState
    {
        PLAYING,
        FINISHED
    };
    private GameState m_gameState;
    private GameObject m_gameOverCanvas;

    // Use this for initialization
    void Start()
    {
        if (m_gridCells == null)
        {
            m_gameOverCanvas = GameObject.Find("GameOverCanvas");
            m_gameState = GameState.PLAYING;
            m_gridCells = new List<GridCell>();
            m_playerXPrefab = Resources.Load("Prefabs/Player_X") as GameObject;
            m_enemyOPrefab = Resources.Load("Prefabs/Enemy_O") as GameObject;
            m_gridCell = Resources.Load("Prefabs/GridCell") as GameObject;
            m_cellSize = m_gridCell.GetComponent<SpriteRenderer>().bounds.size;
            m_gameOverCanvas.SetActive(false);
        }
    }

    private void RestartGame()
    {
        GameObject.Find("GameOverCanvas").SetActive(false);
        //cleanup
        var objects = GameObject.FindGameObjectsWithTag("Mark");
        foreach (var item in objects)
        {
            Destroy(item);
        }
        //reset memory also
        m_gridCells.ForEach(cell => cell.CellState = GridCell.State.Empty);
        m_gameState = GameState.PLAYING;
    }

    public void UploadGridData(GameObject[,] array)
    {
        Start();
        Debug.Log("We're here");
        m_cellObjectArray = array;
        foreach (var item in m_cellObjectArray)
        {
            m_gridCells.Add(item.GetComponent<GridCell>());
        }
    }

    private GameObject GetCellUnderCoord(Vector3 pos)
    {
        pos.z = -10;
        RaycastHit hitInfo;
        int layerMask = 1 << 8;
        Debug.DrawLine(pos, Vector3.forward * 500, Color.blue, 6000);
        if (Physics.Raycast(new Ray(pos, Vector3.forward), out hitInfo, 500.0f, layerMask))
        {
            if (hitInfo.collider && hitInfo.collider.gameObject.tag.Contains("GridCell"))
            {
                return hitInfo.collider.gameObject;
            }
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if(m_gameState == GameState.FINISHED)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RestartGame();
            }
            return;
        }
        //process user input
        if (Input.GetMouseButtonDown(0))
        {
            if(CheckIfGridFilled() || CheckForWin())
            {
                return; //end game
            }
            Vector3 mouseClickInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseClickInWorld.z = 0;
            
            GameObject cell = GetCellUnderCoord(mouseClickInWorld);
            Debug.Assert(cell);
            if (cell)
            {
                var cellLogic = cell.GetComponent<GridCell>();
                if (cell && playerCanPlaceInThatCell(cellLogic))
                {
                    var playerObject = (GameObject)Instantiate(
                       m_playerXPrefab,
                       new Vector3((cellLogic.CoordInGrid.x - 1) * (m_cellSize.x),
                             (cellLogic.CoordInGrid.y - 1) * (m_cellSize.y), -3),
                       Quaternion.identity) as GameObject;
                    cellLogic.CellState = GridCell.State.Player;
                    Debug.Log(cellLogic.gameObject.name);
                    m_gridCells.ForEach(gridCell => { Debug.Log(gridCell.CellState.ToString()); });

                    if (CheckIfGridFilled() || CheckForWin())
                    {
                        EndGame();
                        return; //end game
                    }

                    AI_MakeMove();

                    if (CheckIfGridFilled() || CheckForWin())
                    {
                        EndGame();
                        return; //end game
                    }
                }
            }
        }
    }

    private void EndGame()
    {
        m_gameState = GameState.FINISHED;
        m_gameOverCanvas.SetActive(true);
    }

    private bool CheckIfGridFilled()
    {
        const int k_maxCells = 9;
        return m_gridCells.Count(cell => cell.CellState != GridCell.State.Empty) == k_maxCells;
    }

    //Game state method. Should end the game if one side wins
    private bool CheckForWin()
    {
        Debug.Assert(m_gridCells.Count != 0);
        var rows = generateRows(m_gridCells);
        bool isPlayerWon = rows.Where(cellList => { return cellList.Count(cell => { return cell.CellState == GridCell.State.Player; }) == 3; }).Count() > 0;
        bool isAIWon = rows.Where(cellList => { return cellList.Count(cell => { return cell.CellState == GridCell.State.AI; }) == 3; }).Count() > 0;
        if(isPlayerWon || isAIWon)
        {
            Debug.Log(isPlayerWon ? "You have won!" : "AI have won!");
            m_gameOverCanvas.transform.FindChild("Who_won_text").GetComponent<UnityEngine.UI.Text>().text = isPlayerWon ? "You have won!" : 
                "AI have won!";
            return true;
        }
        else
        {
            m_gameOverCanvas.transform.FindChild("Who_won_text").GetComponent<UnityEngine.UI.Text>().text = "Draw!";
        }
        return false;
    }

    public class Row
    {
        private List<GridCell> list;

        public Row(List<GridCell> list)
        {
            this.list = list;
        }

        public List<GridCell> GetList()
        {
            return list;
        }

        public static implicit operator List<GridCell>(Row row)
        {
            return row.list as List<GridCell>;
        }

        public static implicit operator Row(List<GridCell> list)
        {
            return new Row(list);
        }

        internal void Add(List<GridCell> list)
        {
            foreach (var item in list)
            {
                this.list.Add(item);
            }
            
        }
    }

    private void AI_MakeMove()
    {
        List<GridCell> cellLogicList = new List<GridCell>();
        foreach (var cell in m_cellObjectArray)
        {
            cellLogicList.Add(cell.GetComponent<GridCell>());
        }
        //must be 8 at total
        var rows = generateRows(cellLogicList);
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
        bool centerIsEmpty = m_gridCells[4].CellState == GridCell.State.Empty;
        if (centerIsEmpty)
        {
            m_gridCells[4].CellState = GridCell.State.AI;
            DrawAICell(m_gridCells[4]);
        }
        else if (twoAIMarks.GetList()!=null)
        {
            Debug.Log("Ai is twoAIMarks!");
            AI_CompleteRow(twoAIMarks);
        }
        else if (twoPlayerMarks.GetList()!=null)
        {
            Debug.Log("Ai is twoPlayerMarks!");
            AI_CompleteRow(twoPlayerMarks);
        }
        else if(onePlayerMark.GetList()!=null)
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
            row.Count(rowCell => rowCell.CellState == GridCell.State.Empty) >= 1; });
    }

    private Row findRowWithOneUserMarkAndEmptySlot(List<List<GridCell>> rows)
    {
        int count = 1;
        if (count >= 1)
        {
            Debug.Log("111");
        }
        return rows.Find(row => { return (row.Count(rowCell => rowCell.CellState == GridCell.State.Player) == 1) &&
                                        (row.Count(rowCell => rowCell.CellState == GridCell.State.Empty) >= 1); });
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
            DrawAICell(cell);
            return true;
        }
        else //no empty cells!
        {
            return false; //gimme another row!
        }
    }

    private void DrawAICell(GridCell cell)
    {
        var AI_Object = (GameObject)Instantiate(
                       m_enemyOPrefab,
                       new Vector3((cell.CoordInGrid.x - 1) * (m_cellSize.x),
                             (cell.CoordInGrid.y - 1) * (m_cellSize.y), -3),
                       Quaternion.identity) as GameObject;
    }

    

    private List<List<GridCell>> generateRows(List<GridCell> cellLogicList/*GameObject[,] m_cellObjectArray*/)
    {
        //Row[] rows = new Row[8];
        //start with horizontal
        List<List<GridCell>> allRows = new List<List<GridCell>>();
        //one horizontal row
        Row horizontalRow1 = cellLogicList.Where(cell => cell.CoordInGrid.y == 0).ToList();
        Row horizontalRow2 = cellLogicList.Where(cell => cell.CoordInGrid.y == 1).ToList();
        Row horizontalRow3 = cellLogicList.Where(cell => cell.CoordInGrid.y == 2).ToList();

        allRows.Add(horizontalRow1);
        allRows.Add(horizontalRow2);
        allRows.Add(horizontalRow3);

        Row verticalRow1 = cellLogicList.Where(cell => cell.CoordInGrid.x == 0).ToList();
        Row verticalRow2 = cellLogicList.Where(cell => cell.CoordInGrid.x == 1).ToList();
        Row verticalRow3 = cellLogicList.Where(cell => cell.CoordInGrid.x == 2).ToList();

        allRows.Add(verticalRow1);
        allRows.Add(verticalRow2);
        allRows.Add(verticalRow3);

        int maxCell = 3 - 1;
        Row diagonalRow1 = cellLogicList.FindAll(cell => { return cell.CoordInGrid.x == cell.CoordInGrid.y; }).ToList();//Where(cell => { return cell.CoordInGrid.x == i && cell.CoordInGrid.y == i; }).ToList();
        Row diagonalRow2 = cellLogicList.FindAll(cell => { return ((cell.CoordInGrid.x == 1 && cell.CoordInGrid.y == 1)); }).ToList();
        diagonalRow2.Add(cellLogicList.FindAll(cell =>
        {
            return (cell.CoordInGrid.x == maxCell && cell.CoordInGrid.y == 0) ||
                    (cell.CoordInGrid.y == maxCell && cell.CoordInGrid.x == 0);
        }).ToList());

        //and two by diagonals
        allRows.Add(diagonalRow1);
        allRows.Add(diagonalRow2);
        return allRows;
    }

    private bool playerCanPlaceInThatCell(GridCell cell)
    {
        return cell.CellState == GridCell.State.Empty;
    }
}
