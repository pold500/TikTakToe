using UnityEngine;
using System.Collections;

public class GridCell : MonoBehaviour {

    private Point2D m_coordInGrid;
    public enum State
    {
        Empty,
        Player,
        AI
    };
    private State m_state;
    public State CellState { get { return m_state; } set { m_state = value; } }

    public Point2D CoordInGrid
    {
        get { return m_coordInGrid; }
        set { m_coordInGrid = value; }
    }

    // Use this for initialization
    void Start () {
	
	}
	


	// Update is called once per frame
	void Update () {
	
	}
}
