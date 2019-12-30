using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Binary Heap Tree
// ensure the item in question implements the IHeapItem interface so that we can compare them
// Generic so it can be used with any type as long as they type implements the IHeapItem interface
public class Heap<T> where T : IHeapItem<T>
{
    T[] items;  // all items in the heap
    int currentItemCount;   // how many items are in the heap currently

    public int Count { get => currentItemCount; }

    // set up the heap - it takes a max size because arrays are fixed sizes
    public Heap(int maxSize)
    {
        items = new T[maxSize];
    }

    // Add an item to the heap
    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;  // set the heap index
        items[currentItemCount] = item;     // add the item to the heap

        SortUp(item);                       // sort up the heap since it will always be added at the bottom, but in case it's not in the right place
        currentItemCount++;                 // increment the count
    }

    // Pop the first node in the heap and give it to us
    public T PopFirst()
    {
        T firstItem = items[0]; // get the item we're popping
        currentItemCount--;     // decrement the count

        // set first item to the last because we'll sort it back down to the right place - this will leave whichever leaf of the original first node has the highest priority as the new head
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    // if we want to change the priority of the item - like when searching for the neighbors in A*
    public void UpdateItem(T item)
    {
        // only sort up because priority only ever increases in pathfinding
        SortUp(item);
    }

    // check if an item is in the heap
    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    // sort a node down the tree
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

                // check if the item has lower priority than the swap index and swap it if true
                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else // otherwise it's in the right place
                    return;
            }
            else // if the parent has no children, just exit
                return;
        }
    }

    // sort a node up a tree
    // parent: (n-1)/2
    void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while(true)
        {
            T parentItem = items[parentIndex];
            // see if the item has a lower fcost (higher priority) than the parentItem
            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);
            else
                break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    // swaps two items in the heap
    void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

// interface to allow us to track position within the binary heap tree
public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}
