using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    [ExecuteAlways]
    public class Singleton_WaterController : Singleton.BehaniourBase
    {
        private readonly ShaderProperty.VectorValue WATER_POSITION = new("_qc_WaterPosition");
        private readonly Gate.Float _positionGate = new();


        private void LateUpdate()
        {
            if (_positionGate.TryChange(transform.position.y)) 
            {
                WATER_POSITION.GlobalValue = transform.position.ToVector4(0);
            }
        }

        public override void Inspect()
        {
            WATER_POSITION.Nested_Inspect();

        }

    }

    [PEGI_Inspector_Override(typeof(Singleton_WaterController))]
    internal class Singleton_WaterControllerDrawer : PEGI_Inspector_Override { }

}