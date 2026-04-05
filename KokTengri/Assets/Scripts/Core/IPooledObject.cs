namespace KokTengri.Core
{
    public interface IPooledObject
    {
        bool IsActive { get; }

        void OnPoolCreate();
        void OnPoolTake();
        void OnPoolReturn();
        void OnPoolDestroy();
    }
}
