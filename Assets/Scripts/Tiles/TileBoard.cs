using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;

public class TileBoard : MonoBehaviour
{
    public Tilemap tilemap;                     // the tile map
    public bool toggleValueHighlighting = true; // toggle for highlighting the walkable and non-walkable tiles

    private bool[] walkables;                       // 0 = walkable, 1 = non-walkable
    private int[] terrainPenalties;
    private Sprite[] tilemapSprites;            // get all of the sprites we're using - this is only to hardcode the walkable values
    private int[] notWalkableIndices = new int[] { 3, 4, 5, 12, 13, 14, 22, 23, 24, 28, 29, 30, 31, 35, 40, 44, 47, 48, 51, 52, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68 }; // hardcoded for ease
    private int[] difficultTerrain = new int[] { 8, 9, 17, 18, 25, 26, 27, 32, 33, 34, 41, 42, 43};

    TileNode[,] nodes;  // 2D array of all of the grids
    int offsetX;        // index X offset because our bottom left corner might not be at position (0, 0, 0)
    int offsetY;        // index Y offset because our bottom left corner might not be at position (0, 0, 0)
    int gridSizeX;      // total width of the grid
    int gridSizeY;      // total height of the grid

    public int MaxSize { get => gridSizeX * gridSizeY; } // the total size of the grid is gridSizeX * gridSizeY

    private void Awake()
    {
        tilemapSprites = Resources.LoadAll<Sprite>("TilesetExample");
        walkables = new bool[tilemapSprites.Length];
        terrainPenalties = new int[tilemapSprites.Length];

        // hardcode the walkable array for now
        for (int i = 0; i < walkables.Length; i++)
        {
            walkables[i] = true;
        }

        for (int i = 0; i < notWalkableIndices.Length; i++)
        {
            walkables[notWalkableIndices[i]] = false;
        }

        // hardcode the terrain penalties array as well
        for(int i = 0; i < difficultTerrain.Length; i++)
        {
            terrainPenalties[difficultTerrain[i]] = 5;
        }

        CreateGrid();
    }

    // creates the grid array
    private void CreateGrid()
    {
        offsetX = Mathf.Abs(tilemap.cellBounds.xMin);   // get the leftmost X value on the tilemap
        offsetY = Mathf.Abs(tilemap.cellBounds.yMin);   // get the bottommost Y value on the tilemap
        int columnCount = tilemap.cellBounds.xMin - 1;  // set the column count to the leftmost X value - 1
        int rowCount = tilemap.cellBounds.yMin - 1;     // set the row count to the bottommost Y value - 1

        // count the number of columns and rows based on if a tile is actually there or not
        // Unity's Tilemap allows you to erase tiles, but it doesn't make the grid smaller and instead just leaves null cells
        // we do this to ensure the grid for our pathfinding is made up of only valid tiles
        // NOT CURRENTLY TESTED WITH AN INCOMPLETE COLUMN OR ROW
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos) && tilemap.GetTile(pos) != null) continue;

            if (pos.x > columnCount)
            {
                columnCount++;
                gridSizeX++;
            }

            if (pos.y > rowCount)
            {
                rowCount++;
                gridSizeY++;
            }
        }

        nodes = new TileNode[gridSizeX, gridSizeY]; // create our grid of nodes

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;        // ensure the tile is still there

            TileBase baseTile = tilemap.GetTile(pos);   // get the tile base

            // get the number from the tile's name (which is derived from the spritesheet - this is used to determine the correct walkability value for the tile)
            string pattern = @"\d";

            StringBuilder sb = new StringBuilder();

            foreach (Match m in Regex.Matches(baseTile.name, pattern))
                sb.Append(m);

            int code;
            Int32.TryParse(sb.ToString(), out code);

            // set the correct indices/positions in the grid array to account for the grid not starting from (0,0,0)
            int aGridX = pos.x + offsetX;
            int aGridy = pos.y + offsetY;

            // set up the tile
            TileNode tile = new TileNode(
                pos,
                tilemap.CellToWorld(pos),
                baseTile,
                tilemap,
                pos.x + ", " + pos.y,
                walkables[code],
                aGridX,
                aGridy,
                terrainPenalties[code]);

            nodes[aGridX, aGridy] = tile; // add the tile
        }
        //Debug.Log("Grid Size: " + MaxSize + ", Nodes length: " + nodes.Length);

        //TileNode first = NodeFromWorldPosition(new Vector3(-10, -5, 0));
        //TileNode last = NodeFromWorldPosition(new Vector3(17.5f, 16.5f, 0));

        //Debug.Log("First world pos: " + first.worldPosition + ", Last world pos: " + last.worldPosition);
        //Debug.Log("tilemap size x: " + gridSizeX + ", tilemap size y: " + gridSizeY);
    }

    // Get the tile's neighbors
    public List<TileNode> GetNeighbors(TileNode node)
    {
        List<TileNode> neighbors = new List<TileNode>();

        // check a 3x3 around the node
        // (-1,1)  (0,1),   (1,1)
        // (-1, 0) (0,0),   (1,0)
        // (-1,-1) (0, -1), (1, -1)
        for(int x = -1; x <= 1; x++)
        {
            for(int y = -1; y <= 1; y++)
            {
                // ignore the node if it is itself or diagonal (since I only care about 4 directional movement)
                // allow values where both x and y are non-zero for 8 directional movement/neighbor checking
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

    // get the correct node in the grid based on a world position
    public TileNode NodeFromWorldPosition(Vector3 worldPos)
    {
        Vector3 centeredPosition = worldPos - tilemap.cellBounds.center; // subtract the center point in case the map's world position center is not at (0,0)
        
        // convert world position to a percent of how far on the grid it is
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

    // color the tiles base on walkable and non-walkable values
    private void OnDrawGizmos()
    {
        if(nodes != null)
        {
            foreach(TileNode n in nodes)
            {
                n.tilemapMember.SetTileFlags(n.localPosition, TileFlags.None);

                if (toggleValueHighlighting)
                {

                    if (n.walkable) // walkable
                        n.tilemapMember.SetColor(n.localPosition, Color.green);
                    else if (!n.walkable) // not walkable
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
