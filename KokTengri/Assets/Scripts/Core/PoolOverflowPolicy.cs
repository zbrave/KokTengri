namespace KokTengri.Core
{
    public enum PoolOverflowPolicy
    {
        Expand = 0,
        ReturnNull = 1,
        RecycleOldest = 2,
    }
}
