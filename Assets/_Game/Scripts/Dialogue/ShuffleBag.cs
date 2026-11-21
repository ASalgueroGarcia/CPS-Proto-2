using UnityEngine;
using System.Collections.Generic;

public class ShuffleBag<T>
{
    private List<T> bag;
    private int index;

    public ShuffleBag() { 
        bag = new List<T>();
        index = 0;
    }


    public void Add(T item, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
            bag.Add(item);

        Shuffle();
    }

    public void Set(List<T> items)
    {
        bag = new List<T>(items);
    }

    public void Remove(T item, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            if(bag.Remove(item) == false)
                break;
        }
        Shuffle();
    }

    public void Clear()
    {
        bag.Clear();
        index = 0;
    }
    
    public T GetNext()
    {
        if (bag.Count == 0)
            throw new System.InvalidOperationException("The bag is empty.");

        if(index == 0)
            Shuffle();
        
        T item = bag[index];
        index = (index + 1) % bag.Count;
        return item;
    }

    /// <summary>
    /// Shuffles the items in the bag using the Fisher-Yates shuffle algorithm.
    /// </summary>
    public void Shuffle()
    {
        for(int i = bag.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            T temp = bag[i];
            bag[i] = bag[randomIndex];
            bag[randomIndex] = temp;
        }
    }

    public int Count
    {
        get { return bag.Count; }
    }

    public bool IsEmpty
    {
        get { return bag.Count == 0; }
    }

    public List<T> GetAllItems()
    {
        return new List<T>(bag);
    }
}
