using UnityEngine;

namespace CircuitOneStroke.Data
{
    public enum DiodeMode
    {
        None,
        AtoB,
        BtoA
    }

    [System.Serializable]
    public class EdgeData
    {
        public int id;
        public int a;
        public int b;
        public DiodeMode diode = DiodeMode.None;
        [Tooltip("-1 = normal wire, >= 0 = gate group id")]
        public int gateGroupId = -1;
        [Tooltip("Used when gateGroupId >= 0")]
        public bool initialGateOpen = true;
    }
}
