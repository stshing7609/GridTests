﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Transform target;    // where are we trying to reach
    public float speed = 20;    // movement speed
    Vector3[] path;             // the path of points to follow
    int targetindex;            // how far along the path are we

    private void Start()
    {
        PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] newPath, bool pathSuccess)
    {
        if(pathSuccess)
        {
            path = newPath;
            StopCoroutine("FollowPath"); // stop it first in case we're already following a path
            StartCoroutine("FollowPath");
        }
    }

    // follow the path point by point
    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while(true)
        {
            if(transform.position == currentWaypoint)
            {
                targetindex++;
                if (targetindex >= path.Length)
                    yield break; // exit the coroutine
                currentWaypoint = path[targetindex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed*Time.deltaTime);
            yield return null;
        }
    }

    // draw black cubes and lines along the path
    public void OnDrawGizmos()
    {
        if(path != null)
        {
            for(int i = targetindex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one);

                if (i == targetindex)
                    Gizmos.DrawLine(transform.position, path[i]);
                else
                    Gizmos.DrawLine(path[i - 1], path[i]);
            }
        }
    }
}
