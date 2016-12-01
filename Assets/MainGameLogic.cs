using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Game;

public class Helpers
{
    public static List<List<GridCell>> generateRows(List<GridCell> cellLogicList)
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

public sealed class Singleton<T>
{
    private Singleton()
    {
    }

    public static Singleton<T> Instance { get { return Nested.instance; } }

    private class Nested
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Nested()
        {
        }

        internal static readonly Singleton<T> instance = new Singleton<T>();
    }
}

public class GridLogic: MonoBehaviour
{
    //cached values
    GameObject m_enemyOPrefab;
    GameObject m_playerXPrefab;
    Vector3 m_cellSize;
    List<GameObject> m_marks;

    public GridLogic()
    {
        //init prefabs
        m_playerXPrefab = Resources.Load("Prefabs/Player_X") as GameObject;
        m_enemyOPrefab = Resources.Load("Prefabs/Enemy_O") as GameObject;
        var gridCell = Resources.Load("Prefabs/GridCell") as GameObject;
        m_cellSize = gridCell.GetComponent<SpriteRenderer>().bounds.size;
        m_marks = new List<GameObject>();
    }

    public enum MarkType
    {
        Player,
        AI
    }

    public void AddMarkToGrid(GridCell cell, MarkType type)
    {
        var newObject = (GameObject)Instantiate(
                       type == MarkType.AI ? m_enemyOPrefab : m_playerXPrefab,
                       new Vector3((cell.CoordInGrid.x - 1) * (m_cellSize.x),
                             (cell.CoordInGrid.y - 1) * (m_cellSize.y), -3),
                       Quaternion.identity) as GameObject;

        m_marks.Add(newObject);
    }
};


public class MainGameLogic : MonoBehaviour
{
    private GameObject m_playerXPrefab;
    private GameObject m_gridCell;
    private Vector3 m_cellSize;
    private GameObject[,] m_cellObjectArray;
    private List<GridCell> m_gridCells;
    private GameObject m_enemyOPrefab;
    private GridLogic m_gridLogic;
    private enum GameState
    {
        PLAYING,
        FINISHED
    };
    private GameState m_gameState;
    private GameObject m_gameOverCanvas;
    private GameAI m_ai;

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
            m_gridLogic = new GridLogic();
            m_ai = new GameAI(m_gridLogic);
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
        }
        //process user input
        else if (Input.GetMouseButtonDown(0))
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
                    m_gridLogic.AddMarkToGrid(cellLogic, GridLogic.MarkType.Player);

                    cellLogic.CellState = GridCell.State.Player;

                    Debug.Log(cellLogic.gameObject.name);
                    m_gridCells.ForEach(gridCell => { Debug.Log(gridCell.CellState.ToString()); });

                    if (CheckIfGridFilled() || CheckForWin())
                    {
                        EndGame();
                        return; //end game
                    }

                    m_ai.AI_MakeMove(m_gridCells);

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
        var rows = Helpers.generateRows(m_gridCells);
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

    private bool playerCanPlaceInThatCell(GridCell cell)
    {
        return cell.CellState == GridCell.State.Empty;
    }
}
