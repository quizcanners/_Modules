using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.DetectiveInvestigations
{
    public static partial class Detective
    {
        [Serializable]
        public class Case_State : IPEGI
        {
            [SerializeField] private SO_Detective_Case_Prototype.Id _caseId;

            public void Inspect()
            {



            }
        }
    }
}