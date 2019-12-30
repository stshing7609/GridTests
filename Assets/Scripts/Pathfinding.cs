using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Pathfinding : MonoBehaviour
{
    //public Transform seeker, target;
    PathRequestManager requestManager;
    TileBoard grid;

    private void Awake()
    {
        requestManager = GetComponent<PathRequestManager>();
        grid = GetComponent<TileBoard>();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    FindPath(seeker.position, target.position);
        //}

        if (Input.GetKeyUp(KeyCode.Return))
        {
            grid.toggleValueHighlighting = !TileBoard.instance.toggleValueHighlighting;
        }
    }

    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    IEnumerator FindPath(Vector3 start, Vector3 end)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        TileNode startNode = grid.NodeFromWorldPosition(start);
        TileNode targetNode = grid.NodeFromWorldPosition(end);

        // only pathfind if both the start and end are walkable
        if (startNode.value == 0 && targetNode.value == 0)
        {
            //List<TileNode> openSet = new List<TileNode>(); // what nodes are available to check
            Heap<TileNode> openSet = new Heap<TileNode>(grid.MaxSize); // use a Binary Heap instead so that we only compare to parent
            HashSet<TileNode> closedSet = new HashSet<TileNode>(); // what nodes have been checked

            openSet.Add(startNode); // add the start node
            while (openSet.Count > 0)
            {
                TileNode currentNode = openSet.RemoveFirst(); // openSet[0]
                // very slow because it has to iterate through everything
                //for(int i = 1; i < openSet.Count; i++)
                //{
                //    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                //    {
                //        currentNode = openSet[i];
                //    }
                //}

                //openSet.Remove(currentNode);
                closedSet.Add(currentNode); // add the current node to the closed set so we can't check it again

                // if we find the node we want, then retrace the path and exit the function because that's our path
                if (currentNode == targetNode)
                {
                    sw.Stop();
                    print("Path found: " + sw.ElapsedMilliseconds + "ms");
                    pathSuccess = true;
                    //RetracePath(startNode, targetNode);
                    //return;
                    break;
                }

                // check each neighbor
                foreach (TileNode neighbor in grid.GetNeighbors(currentNode))
                {
                    // if the neighbor is not walkable (value of 1) or in the closed set, don't check it
                    if (neighbor.value == 1 || closedSet.Contains(neighbor))
                        continue;

                    // get path from the current node to the neighbor
                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor); // add the current node's g to find out how far it is from the start in total
                                                                                                            // if the new path to the neighbor is shorter or the the neighbor isn't in the open set yet
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        // set up the neighbor
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode); // heuristic distance to the target
                        neighbor.parent = currentNode; // set the neighbor's parent to the currentNode (this allows us to traverse the path later)

                        // add the neighbor if it's not in the open set
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                        else // if it is in the open set, update it's values
                            openSet.UpdateItem(neighbor);
                    }
                }
            }
        }

        yield return null;
        if(pathSuccess)
            waypoints = RetracePath(startNode, targetNode);
        requestManager.FinishedProcessPath(waypoints, pathSuccess);
    }

    // go through the path
    Vector3[] RetracePath(TileNode startNode, TileNode endNode)
    {
        List<TileNode> path = new List<TileNode>();
        TileNode currentNode = endNode; // start at the end to trace back our steps

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        Vector3[] waypoints = SimplifyPath(path);
        // reverse the path because we traversed it backwards
        Array.Reverse(waypoints);

        return waypoints;
    }

    // remove any unnecessary waypoints and only have points when directions turn
    Vector3[] SimplifyPath(List<TileNode> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero; // set the old direction to nowhere

        for(int i = 1; i < path.Count; i++)
        {
            // new direction = difference between the points
            Vector2 directionNew = new Vector2(path[i - 1].aGridX - path[i].aGridX, path[i - 1].aGridY - path[i].aGridY);
            // add the waypoint if the direction changed
            if(directionNew != directionOld)
            {
                waypoints.Add(path[i-1].worldPosition); // add the node before as those are the corner points - this forces straight - grid-like movement
            }

            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }

    // Distance Heuristic
    int GetDistance(TileNode nodeA, TileNode nodeB)
    {
        // Manhattan Distance - I only care for 4 directional movement
        int distX = Mathf.Abs(nodeA.aGridX - nodeB.aGridX);
        int distY = Mathf.Abs(nodeA.aGridY - nodeB.aGridY);

        return distX + distY;

        // useful for 8 directional movement as it modifies the diagonals to be higher cost, but lower than going to it in a 4 directional way
        // multiple the straight directions by 10 and the diagonals by 14 (1.5straightcost - 1)
        //if (distX > distY)
        //    return 14 * distY + 10 * (distX - distY);
        //return 14 * distX + 10 * (distY - distX);
    }
}
