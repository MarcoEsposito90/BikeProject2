using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockingQueue<Type>{

    Queue<Type> queue;

    public BlockingQueue()
    {
        queue = new Queue<Type>();
    }


    public bool isEmpty()
    {
        lock (queue)
        {
            return queue.Count == 0;
        }
    }

    public Type Dequeue()
    {
        lock (queue) {
            return queue.Dequeue();
        }
    }

    public void Enqueue(Type value)
    {
        lock (queue)
        {
            queue.Enqueue(value);
        }
    }
}
