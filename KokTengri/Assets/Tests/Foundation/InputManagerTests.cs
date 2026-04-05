using System;
using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KokTengri.Tests.Foundation
{
    public class InputManagerTests
    {
        private GameObject _gameObject;
        private InputManager _inputManager;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(InputManagerTests));
            _inputManager = _gameObject.AddComponent<InputManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_gameObject);
            }
        }

        [Test]
        public void UpdateMoveDirection_DelegatesMoveInputToConsumers()
        {
            var expectedMoveDirection = new Vector2(1f, -1f);
            var eventRaised = false;
            var receivedMoveDirection = Vector2.zero;

            _inputManager.OnMove += moveDirection =>
            {
                eventRaised = true;
                receivedMoveDirection = moveDirection;
            };

            _inputManager.UpdateMoveDirection(expectedMoveDirection);

            Assert.That(_inputManager.MoveDirection, Is.EqualTo(expectedMoveDirection));
            Assert.That(eventRaised, Is.True);
            Assert.That(receivedMoveDirection, Is.EqualTo(expectedMoveDirection));
        }
    }

    public class InputProviderInjectionTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Constructor_WithInjectedProvider_TracksMoveDirectionChanges()
        {
            var mockInputProvider = new MockInputProvider();
            var consumer = new InputConsumer(mockInputProvider);
            var expectedMoveDirection = new Vector2(0.25f, 0.75f);

            mockInputProvider.PushMove(expectedMoveDirection);

            Assert.That(consumer.LastMoveDirection, Is.EqualTo(expectedMoveDirection));
        }

        [Test]
        public void Constructor_WithNullProvider_GracefullyDefaultsToZero()
        {
            var consumer = new InputConsumer(null);

            Assert.That(consumer.LastMoveDirection, Is.EqualTo(Vector2.zero));
            Assert.That(() => consumer.Refresh(), Throws.Nothing);
            Assert.That(consumer.LastMoveDirection, Is.EqualTo(Vector2.zero));
        }

        private sealed class InputConsumer
        {
            private readonly IInputProvider _inputProvider;

            public InputConsumer(IInputProvider inputProvider)
            {
                _inputProvider = inputProvider;

                if (_inputProvider != null)
                {
                    LastMoveDirection = _inputProvider.MoveDirection;
                    _inputProvider.OnMove += HandleMove;
                }
            }

            public Vector2 LastMoveDirection { get; private set; }

            public void Refresh()
            {
                if (_inputProvider == null)
                {
                    LastMoveDirection = Vector2.zero;
                    return;
                }

                LastMoveDirection = _inputProvider.MoveDirection;
            }

            private void HandleMove(Vector2 moveDirection)
            {
                LastMoveDirection = moveDirection;
            }
        }

        private sealed class MockInputProvider : IInputProvider
        {
            public Vector2 MoveDirection { get; private set; }

            public event Action<Vector2> OnMove;

            public void PushMove(Vector2 moveDirection)
            {
                MoveDirection = moveDirection;
                OnMove?.Invoke(moveDirection);
            }
        }
    }
}
