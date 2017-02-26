using UnityEngine;
using System.Collections;

public class ObjectCollisionHandler : MonoBehaviour {

    public bool acceptsSelfIntersection;
    public Vector2 gridPosition { get; set; }

    private int priority;
    public delegate void CollisionEvent(Vector2 gridPosition);
    public event CollisionEvent onCollision;

    void Start()
    {
        priority = GlobalInformation.getPriority(gameObject.tag);
    }


	void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (!other.gameObject.activeInHierarchy)
            return;

        string tag = other.gameObject.tag;
        int p = GlobalInformation.getPriority(tag);

        if (priority <= p)
        {
            if (priority == p && acceptsSelfIntersection)
                return;

            onCollision(gridPosition);
        }
    }
}
