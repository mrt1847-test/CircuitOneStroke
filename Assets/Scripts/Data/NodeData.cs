using UnityEngine;

namespace CircuitOneStroke.Data
{
    public enum NodeType
    {
        Bulb,
        Switch
    }

    [System.Serializable]
    public class NodeData
    {
        public int id;
        public Vector2 pos;
        public NodeType nodeType;
        [Tooltip("Used when nodeType is Switch")]
        public int switchGroupId;
    }
}
