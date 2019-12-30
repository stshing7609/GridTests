using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// require a Pathfinding component to use the request manager
[RequireComponent(typeof(Pathfinding))]
public class PathRequestManager : MonoBehaviour
{
    struct PathRequest
    {
        public Vector3 pathStart;                   // position where the path starts
        public Vector3 pathEnd;                     // position where the path ends
        public Action<Vector3[], bool> callback;    // a callback method that will require a vector3[] (the path) and if the path was successfully found and made

        public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
        }
    }

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>(); // do paths in a FIFO basis
    PathRequest currentPathRequest; // the current path being requested
    Pathfinding pathfinding;        // our pathfinding algorithm object

    bool isProcessingPath;          // are we currently processing a path

    // make this a singleton so that we don't have multiples of these running
    static PathRequestManager instance;

    private void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
    }

    // takes in where the path starts, where the path ends, and send a callback because we're splitting the path calls over several frames
    // Action<the path, have we started the path>
    // is static so it can be accessed by calling the Class from anywhere
    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    // try the next porcess in the Queue
    void TryProcessNext()
    {
        // don't start the path if we're already processing or the queue is empty
        if (!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd); // start the path searche
        }
    }

    // finish a path
    public void FinishedProcessPath(Vector3[] path, bool success)
    {
        currentPathRequest.callback(path, success); // do the callback now that the path has been found
        isProcessingPath = false;
        TryProcessNext(); // try any next process in the queue
    }
}
