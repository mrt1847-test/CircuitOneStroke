namespace CircuitOneStroke.Core
{
    /// <summary>Hard fail cause for level attempt.</summary>
    public enum FailReason
    {
        Incomplete,
        RevisitNode,
        Timeout,
        Other
    }
}
