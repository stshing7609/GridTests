using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileNode : IHeapItem<TileNode>
{
    public Vector3Int localPosition;
    public Vector3 worldPosition;
    public TileBase tileBase;
    public Tilemap tilemapMember;
    public int aGridX;
    public int aGridY;

    public string name;
    public int value;

    public int gCost;
    public int hCost;
    public TileNode parent;
    int heapIndex;

    public TileNode(Vector3Int _localPosition, Vector3 _worldPosition, TileBase _tileBase, Tilemap _tilemapMember, string _name, int _value, int _aGridX, int _aGridY)
    {
        localPosition = _localPosition;
        worldPosition = _worldPosition + new Vector3(0.5f, 0.5f, 0);
        tileBase = _tileBase;
        tilemapMember = _tilemapMember;
        name = _name;
        value = _value;
        aGridX = _aGridX;
        aGridY = _aGridY;
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex { get => heapIndex; set => heapIndex = value; }

    public int CompareTo(TileNode nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost); // compare fCost
        if (compare == 0)
            compare = hCost.CompareTo(nodeToCompare.hCost); // compare hCost if fCost is equal

        // lower priority = -1, same priority = 0, higher priority = 1
        // lower priority is if the value is higher and higher priority is if the value is lower.
        // since we compared integers, it returns 1 if the value is higher, so we need to negate compare for the correct priority
        return -compare;
    }
}
