using QuizCanners.Inspect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    internal class PrivateSingletonTest : MonoBehaviour, IPEGI
    {
        public void Inspect()
        {
            "Works".PegiLabel().Nl();
        }


    }

    [PEGI_Inspector_Override(typeof(PrivateSingletonTest))] internal class PrivateSingletonTestDrawer : PEGI_Inspector_Override { }
}
