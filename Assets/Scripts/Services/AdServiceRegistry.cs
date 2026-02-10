namespace CircuitOneStroke.Services
{
    /// <summary>
    /// IAdService 해상용 레지스트리. 부트스트랩에서 등록 후 UI는 Instance만 사용.
    /// </summary>
    public static class AdServiceRegistry
    {
        public static IAdService Instance { get; set; }
    }
}
