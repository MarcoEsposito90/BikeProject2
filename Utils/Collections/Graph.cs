using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Graph<Key, Type>
{

    /* ------------------------------------------------------------------------------------ */
    /* -------------------------- CONSTRUCTOR, FIELDS ------------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region FIELDS_CONSTRUCTOR

    /* ------------------------------------------------------------------------------------ */
    public Dictionary<Key, GraphItem> nodes { get; private set; }
    public List<Link> links { get; private set; }
    private object synchVariable;


    /* ------------------------------------------------------------------------------------ */
    public GraphItem this[Key key]
    {
        get
        {
            return nodes[key];
        }
        private set
        {
            nodes[key] = value;
        }
    }


    /* ------------------------------------------------------------------------------------ */
    public Graph()
    {
        nodes = new Dictionary<Key, GraphItem>();
        links = new List<Link>();
        synchVariable = new object();
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS


    /* ----------------------------------------------------------------------------------------- */
    public GraphItem createItem(Key key, Type item)
    {
        GraphItem node = null;
        lock (nodes)
        {
            if (!nodes.TryGetValue(key, out node))
                node = new GraphItem(item);

            nodes.Add(key, node);
        }

        return node;
    }

    /* ----------------------------------------------------------------------------------------- */
    public bool containsItem(Key key)
    {
        lock(nodes)
            return nodes.ContainsKey(key);
    }


    /* ----------------------------------------------------------------------------------------- */
    public void removeItem(Key key)
    {
        lock (nodes)
        {
            if (!nodes.ContainsKey(key))
                return;

            List<Link> toBeRemoved = nodes[key].links;
            foreach (Link l in toBeRemoved)
            {
                if (l.from.Equals(nodes[key]))
                    l.to.links.Remove(l);
                else
                    l.from.links.Remove(l);

                links.Remove(l);
            }
        }
        
    }


    /* ----------------------------------------------------------------------------------------- */
    public Link createLink(Key from, Key to)
    {
        lock (nodes)
        {
            if (!nodes.ContainsKey(from) || !nodes.ContainsKey(to))
            {
                Debug.Log("WARNING! object missing (Graph.createLink)");
                return null;
            }

            GraphItem g1 = nodes[from];
            GraphItem g2 = nodes[to];
            Link l = new Link(g1, g2);

            if (!g1.isLinkedTo(g2))
                g1.links.Add(l);

            if (!g2.isLinkedTo(g1))
                g2.links.Add(l);

            if (!links.Contains(l))
                links.Add(l);

            return l;
        }
    }
    
    #endregion


    /* ------------------------------------------------------------------------------------ */
    /* -------------------------- GRAPH ITEM ---------------------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region GRAPH_ITEM

    public class GraphItem
    {
        public Type item { get; private set; }
        public List<Link> links
        {
            get;
            private set;
        }

        public GraphItem(Type item)
        {
            this.item = item;
            links = new List<Link>();
        }

        public bool isLinkedTo(GraphItem other)
        {
            foreach (Link l in links)
                if (l.from.Equals(other) || l.to.Equals(other))
                    return true;

            return false;
        }
    }

    #endregion


    /* ------------------------------------------------------------------------------------ */
    /* -------------------------- LINK ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region LINK

    public class Link
    {
        public GraphItem from;
        public GraphItem to;

        public Link(GraphItem from, GraphItem to)
        {
            this.from = from;
            this.to = to;
        }
    }

    #endregion
}
