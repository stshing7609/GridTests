using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

// require a tileboard to use pathfinding
[RequireComponent(typeof(TileBoard))]
public class Pathfinding : MonoBehaviour
{
    //public Transform seeker, target;  // was used beofre the request manager to move a seeker object to a target
    PathRequestManager requestManager;  // the request manager
    TileBoard grid;                     // the grid

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

        // allow us to toggle the highlighting of the walkable and nonwalkable tiles
        if (Input.GetKeyUp(KeyCode.Return))
        {
            grid.toggleValueHighlighting = !grid.toggleValueHighlighting;
        }
    }

    // starts the coroutine to find a path
    public void StartFindPath(Vector3 startPos, Vector3 targetPos)
    {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    // finds a path from the start to the end
    IEnumerator FindPath(Vector3 start, Vector3 end)
    {
        Stopwatch sw = new Stopwatch(); // stopwatch for efficiency diagnostics
        sw.Start();

        Vector3[] waypoints = new Vector3[0];   // the waypoints on the path - will be properly set up later
        bool pathSuccess = false;               // we have not successfully found a path yet

        // get the nodes on the grid based on the world positions
        TileNode startNode = grid.NodeFromWorldPosition(start);
        TileNode targetNode = grid.NodeFromWorldPosition(end);

        // only pathfind if both the start and end are walkable
        if (startNode.walkable && targetNode.walkable)
        {
            //List<TileNode> openSet = new List<TileNode>(); // what nodes are available to check
            Heap<TileNode> openSet = new Heap<TileNode>(grid.MaxSize); // use a Binary Heap instead of List so that we only compare to parent rather than iterate everything
            HashSet<TileNode> closedSet = new HashSet<TileNode>(); // what nodes have been checked

            openSet.Add(startNode); // add the start node
            while (openSet.Count > 0)
            {
                TileNode currentNode = openSet.PopFirst(); // openSet[0]
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

                // if we find the node we want, then retrace the path and exit the loop because that's our path
                if (currentNode == targetNode)
                {
                    sw.Stop();
                    print("Path found: " + sw.ElapsedMilliseconds + "ms");
                    pathSuccess = true;
                    break;
                }

                // check each neighbor
                foreach (TileNode neighbor in grid.GetNeighbors(currentNode))
                {
                    // if the neighbor is not walkable (value of 1) or it's in the closed set, don't check it
                    if (!neighbor.walkable || closedSet.Contains(neighbor))
                        continue;

                    // get path from the current node to the neighbor. Also add weight value here
                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor) + neighbor.movementPenalty; // add the current node's gCost to find out how far it is from the start in total
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
                        else // if it is in the open set, update it's values so we get the correct path
                            openSet.UpdateItem(neighbor);
                    }
                }
            }
        }

        yield return null; // wait a frame then try again
        // if we found a path, set the waypoints to the retrace
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
            pathSuccess = waypoints.Length > 0; // in case we move the target
        }
        requestManager.FinishedProcessPath(waypoints, pathSuccess); // give the requestmanager the data
    }

    // go through the path
    Vector3[] RetracePath(TileNode startNode, TileNode endNode)
    {
        List<TileNode> path = new List<TileNode>(); // holds the ENTIRE path - we haven't set up waypoints yet
        TileNode currentNode = endNode; // start at the end to trace back our steps

        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        // add the start node to the path to ensure we are restricted to 4-directional movement
        // in other wods, the SimplifyPath method will be forced to check the start position against the first movement in the path
        path.Add(startNode);

        Vector3[] waypoints = SimplifyPath(path); // simplify the path and put it in a Vector3[] that will be the path's waypoints
        // reverse the path because we traversed it backwards
        Array.Reverse(waypoints);

        return waypoints;
    }

    // remove any unnecessary points and only have points when directions turn (called waypoints)
    Vector3[] SimplifyPath(List<TileNode> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero; // set the old direction to not moving at first

        for(int i = 1; i < path.Count; i++)
        {
            // new direction = difference between the points (x and y can only be values of -1, 0, and 1 here to show the direction)
            // since we're doing this in a grid like fashion (1,1) and (-1,-1) cannot be possible values
            Vector2 directionNew = new Vector2(path[i - 1].aGridX - path[i].aGridX, path[i - 1].aGridY - path[i].aGridY);
            // add the waypoint if the direction changed
            if(directionNew != directionOld)
            {
                waypoints.Add(path[i-1].worldPosition); // add the node before as those are the corner points - this forces straight - grid-like movement
            }

            directionOld = directionNew; // set the old direction to the new one
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
