using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Binary Heap Tree
// ensure the item in question implements the IHeapItem interface so that we can compare them
public class Heap<T> where T : IHeapItem<T>
{
    T[] items;
    int currentItemCount;

    public int Count { get => currentItemCount; }

    public Heap(int maxSize)
    {
        items = new T[maxSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;

        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;

        // set first item to the last
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    // if we want to change the priority in the item
    public void UpdateItem(T item)
    {
        // only sort up because priority only ever increases in pathfinding
        SortUp(item);
    }

    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    // left child: 2n + 1
    // right child: 2n + 2
    void SortDown(T item)
    {
        while(true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0; // the index to swap in the heap

            // find if any child has lower priority
            if (childIndexLeft < currentItemCount)
            {
                swapIndex = childIndexLeft;
                if (childIndexRight < currentItemCount)
                {
                    // check if left has a lower priority than right
                    if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                    {
                        swapIndex = childIndexRight;
                    }
                }

                // check if the item has lower priority than the swap index
                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else // otherwise it's in the right place
                    return;
            }
            else // if the parent has no children, just exit
                return;
        }
    }

    // parent: (n-1)/2
    void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while(true)
        {
            T parentItem = items[parentIndex];
            // see if the item has a lower fcost (higher priority) than the parentItem
            // can only return -1, 0, 1 based on priority
            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);
            else
                break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}
