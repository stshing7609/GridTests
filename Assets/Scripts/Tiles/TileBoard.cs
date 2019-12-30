using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;

public class TileBoard : MonoBehaviour
{
    public static TileBoard instance;
    public Tilemap tilemap;
    public bool toggleValueHighlighting = true;

    private int[] values; // 0 = walkable, 1 = blocked
    private Sprite[] tilemapSprites;
    private int[] notWalkableIndices = new int[] { 3, 4, 5, 12, 13, 14, 22, 23, 24, 28, 29, 30, 31, 35, 40, 44, 47, 48, 51, 52, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68 };

    TileNode[,] nodes;
    int offsetX;
    int offsetY;
    int gridSizeX;
    int gridSizeY;


    public int MaxSize { get => gridSizeX * gridSizeY; }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        tilemapSprites = Resources.LoadAll<Sprite>("TilesetExample");
        values = new int[tilemapSprites.Length];

        for (int i = 0; i < notWalkableIndices.Length; i++)
        {
            values[notWalkableIndices[i]] = 1;
        }

        CreateGrid();
    }

    private void CreateGrid()
    {
        offsetX = Mathf.Abs(tilemap.cellBounds.xMin);
        offsetY = Mathf.Abs(tilemap.cellBounds.yMin);
        int storedX = tilemap.cellBounds.xMin - 1;
        int storedY = tilemap.cellBounds.yMin - 1;

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;

            if (pos.x > storedX)
            {
                storedX++;
                gridSizeX++;
            }

            if (pos.y > storedY)
            {
                storedY++;
                gridSizeY++;
            }
        }

        nodes = new TileNode[gridSizeX, gridSizeY];

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;

            TileBase baseTile = tilemap.GetTile(pos);

            string pattern = @"\d";

            StringBuilder sb = new StringBuilder();

            foreach (Match m in Regex.Matches(baseTile.name, pattern))
                sb.Append(m);

            int code;
            Int32.TryParse(sb.ToString(), out code);

            int arrayX = pos.x + offsetX;
            int arrayY = pos.y + offsetY;

            TileNode tile = new TileNode(
                pos,
                tilemap.CellToWorld(pos),
                baseTile,
                tilemap,
                pos.x + ", " + pos.y,
                values[code],
                arrayX,
                arrayY);

            nodes[arrayX, arrayY] = tile;
        }
        //Debug.Log("Grid Size: " + MaxSize + ", Nodes length: " + nodes.Length);

        //TileNode first = NodeFromWorldPosition(new Vector3(-10, -5, 0));
        //TileNode last = NodeFromWorldPosition(new Vector3(17.5f, 16.5f, 0));

        //Debug.Log("First world pos: " + first.worldPosition + ", Last world pos: " + last.worldPosition);
        //Debug.Log("tilemap size x: " + gridSizeX + ", tilemap size y: " + gridSizeY);
    }

    public List<TileNode> GetNeighbors(TileNode node)
    {
        List<TileNode> neighbors = new List<TileNode>();

        // check a 3x3 around the node
        for(int x = -1; x <= 1; x++)
        {
            for(int y = -1; y <= 1; y++)
            {
                // ignore the node if it is itself or diagonal (since I only care about 4 directional movement)
                if (x == 0 && y == 0 || (x !=0 && y!=0))
                    continue;

                int checkX = node.aGridX + x;
                int checkY = node.aGridY + y;

                // make sure the node would be in the grid (account for edges and corners)
                if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(nodes[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    public TileNode NodeFromWorldPosition(Vector3 point)
    {
        Vector3 centeredPosition = point - tilemap.cellBounds.center; // subtract the center point in case the map's center not being (0,0)
        
        // convert world position to a percent of how part on the grid it is
        float percentX = (centeredPosition.x + gridSizeX / 2) / gridSizeX;
        float percentY = (centeredPosition.y + gridSizeY / 2) / gridSizeY;
        // clamp between 0 and 1 to avoid anything not on the grid
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        // find the correct grid space by multiplying the percent of the grid by the valid grid indices
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        //Debug.Log("x: " + x + ", y: " + y);

        return nodes[x, y];
    }

    public List<TileNode> path;
    private void OnDrawGizmos()
    {
        if(nodes != null)
        {
            foreach(TileNode n in nodes)
            {
                n.tilemapMember.SetTileFlags(n.localPosition, TileFlags.None);

                if (toggleValueHighlighting)
                {

                    if (n.value == 0) // walkable
                        n.tilemapMember.SetColor(n.localPosition, Color.green);
                    else if (n.value == 1) // not walkable
                        n.tilemapMember.SetColor(n.localPosition, Color.red);
                }
                else
                {
                    n.tilemapMember.SetColor(n.localPosition, Color.white);
                }

                //if (path != null)
                //    if (path.Contains(n))
                //        n.tilemapMember.SetColor(n.localPosition, Color.black);
            }
        }
    }
}
