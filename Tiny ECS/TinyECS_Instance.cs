using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public interface ITinyECSworld
    {
        string WorldName { get; }

        void Inspect(IEntity entity);
    }

    public static class WorldExtensions
    {
        public static World<T> GetWorld<T>(this T controller) where T : ITinyECSworld => WorldSingleton<T>.GetInstance(controller);
    }

    internal static class WorldSingleton<T> where T : ITinyECSworld
    {
        internal static World<T> instance;
        internal static World<T> GetInstance(T controller)
        {
            if (instance == null)
                instance = new World<T>(controller);

            return instance;
        }
    }
}