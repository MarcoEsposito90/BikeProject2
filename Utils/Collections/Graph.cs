using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Graph<Key, Type>
{

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CONSTRUCTOR, FIELDS ------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region FIELDS_CONSTRUCTOR

    private Dictionary<Key, GraphItem> nodes;
    private object synchVariable;

    public Graph()
    {
        nodes = new Dictionary<Key, GraphItem>();
        synchVariable = new object();
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS


    /* ----------------------------------------------------------------------------------------- */
    public GraphItem addItem(Key key, Type item, Dictionary<Key, Type> links)
    {
        GraphItem node = new GraphItem(item);
        lock (synchVariable)
        {

            if (!nodes.TryGetValue(key, out node))
                node = new GraphItem(item);

            foreach (Key linkKey in links.Keys)
            {

                GraphItem toLink = null;

                if (!nodes.ContainsKey(linkKey))
                {
                    toLink = new GraphItem(links[linkKey]);
                    nodes.Add(linkKey, toLink);
                }
                else
                    toLink = nodes[linkKey];

                if (!node.isLinkedTo(toLink))
                    node.addLink(toLink);

                if (!toLink.isLinkedTo(node))
                    toLink.addLink(node);

            }

            if (!nodes.ContainsKey(key))
                nodes.Add(key, node);
        }


        return node;
    }


    /* ----------------------------------------------------------------------------------------- */
    public bool linkExists(Key key1, Key key2)
    {
        GraphItem item1 = null;
        GraphItem item2 = null;

        lock (synchVariable)
        {
            if (!nodes.ContainsKey(key1))
                return false;

            nodes.TryGetValue(key1, out item1);

            if (!nodes.ContainsKey(key2))
                return false;
            nodes.TryGetValue(key2, out item2);

        }

        return item1.isLinkedTo(item2) && item2.isLinkedTo(item1);
    }


    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- GRAPH ITEM --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region GRAPH_ITEM

    public class GraphItem
    {
        public Type item { get; private set; }
        public List<GraphItem> links
        {
            get;
            private set;
        }

        public GraphItem(Type item)
        {
            this.item = item;
            links = new List<GraphItem>();
        }

        public void addLink(GraphItem item)
        {
            links.Add(item);
        }

        public bool isLinkedTo(GraphItem other)
        {
            return links.Contains(other);
        }
    }

    #endregion
}
