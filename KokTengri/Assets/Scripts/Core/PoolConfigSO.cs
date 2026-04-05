using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "PoolConfig", menuName = "KokTengri/Core/Pool Config")]
    public sealed class PoolConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(0)]
        public int InitialSize { get; private set; } = 8;

        [field: SerializeField, Min(1)]
        public int MaxSize { get; private set; } = 32;

        [field: SerializeField]
        public PoolOverflowPolicy OverflowPolicy { get; private set; } = PoolOverflowPolicy.Expand;

        [field: SerializeField]
        public GameObject Prefab { get; private set; }
    }
}
