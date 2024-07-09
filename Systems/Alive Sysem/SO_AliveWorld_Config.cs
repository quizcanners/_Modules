using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.AliveWorld
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Alive/" + FILE_NAME)]
    public class SO_AliveWorld_Config : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Alive Config";

        public AliveDictionary AllCreatures = new();



        #region Inspector
        private readonly pegi.CollectionInspectorMeta _creaturesMeta = new("Creatures");
        
        void IPEGI.Inspect()
        {
            _creaturesMeta.Edit_Dictionary(AllCreatures).Nl();
        }

        #endregion

        [Serializable]
        public class AliveDictionary : SerializableDictionary<string, Alive.Creature.Prototype> { }
    }

    [PEGI_Inspector_Override(typeof(SO_AliveWorld_Config))]
    internal class SO_AliveWorld_ConfigDrawer : PEGI_Inspector_Override { }
}
