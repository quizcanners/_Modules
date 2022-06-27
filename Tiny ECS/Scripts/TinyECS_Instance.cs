using QuizCanners.Utils;
using System;
using System.Diagnostics;

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
            {
                if (typeof(T) == typeof(ITinyECSworld))
                    throw new ArgumentException("Shouldn't be using {0} as a World Link. Use {1}".F(nameof(ITinyECSworld), controller.GetType().ToPegiStringType()));

                instance = new World<T>(controller);
            }
            return instance;
        }
    }
}