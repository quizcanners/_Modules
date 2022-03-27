using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public interface IComponentData
    {

    }

    public struct PositionComponent : IComponentData, IPEGI_ListInspect
    {
        public Vector3 Position;

        public void InspectInList(ref int edited, int index)
        {
            "Position".PegiLabel(70).Edit(ref Position).Nl();
        }
    }

    public struct SpeedComponent : IComponentData, IPEGI_ListInspect
    {
        public float Speed;

        public void InspectInList(ref int edited, int index)
        {
            "Speed".PegiLabel(60).Edit(ref Speed).Nl();
        }
    }

   
}