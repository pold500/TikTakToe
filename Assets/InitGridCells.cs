using UnityEngine;
using System.Collections;

public struct Point2D
{
    public int x;
    public int y;
    public Point2D(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    public override string ToString()
    {
        return "Point2D " + x + " " + y;
    }
};

public class InitGridCells : MonoBehaviour {
    private const int k_gridSize = 3;
    // Use this for initialization
    private GameObject [,] m_grid;
	void Start () {
        var cellPrefab = Resources.Load("Prefabs/GridCell");
        var markerStart = GameObject.Find("GridStartMarker") as GameObject;
        var markerWidth = markerStart.GetComponent<SpriteRenderer>().bounds.size;
        Vector3 gridStartFrom = markerStart.transform.position;
        //Create grid of cell structures
        m_grid = new GameObject[k_gridSize, k_gridSize];
        for(int y = 0; y < k_gridSize; y++) //y cycle
            for (int x = 0; x < k_gridSize; x++) //x cycle
            {
                //Debug.Log("help");
                var cellPrefabClone = Instantiate(cellPrefab) as GameObject;
                int cellZ = -5;
                var pos = gridStartFrom + new Vector3(markerWidth.x * x, markerWidth.y * y, cellZ);
                cellPrefabClone.transform.position = pos;
                cellPrefabClone.GetComponent<GridCell>().CoordInGrid = new Point2D(x, y);
                cellPrefabClone.GetComponent<GridCell>().CellState = GridCell.State.Empty;
                m_grid[x, y] = cellPrefabClone;
            }
        GameObject.Find("GameProcessor").GetComponent<MainGameLogic>().SendMessage("UploadGridData", m_grid);

    }
	
	// Update is called once per frame
	void Update () {
	        
	}
}
