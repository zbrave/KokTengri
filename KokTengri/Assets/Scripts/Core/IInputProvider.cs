using System;
using UnityEngine;

namespace KokTengri.Core
{
    public interface IInputProvider
    {
        Vector2 MoveDirection { get; }

        event Action<Vector2> OnMove;
    }
}
