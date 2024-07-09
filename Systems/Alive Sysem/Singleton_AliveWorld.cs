using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.AliveWorld
{
    [ExecuteAlways]
    [AddComponentMenu(Alive.ADD_COMPONENT_MENU + "/Manager")]
    public class Singleton_AliveWorld : Singleton.BehaniourBase
    {
        [SerializeField] private SO_AliveWorld_Config Config;

        public Alive.Creature.State GetState(string key) 
        {
            if (!Config) 
            {
                Debug.LogError("Config not assigned", this);
                return null;
            }

            if (!Config.AllCreatures.TryGetValue(key, out var prot)) 
            {
                Debug.LogError("Creature prototype {0} not found".F(key), this);
                return null;
            }

           return new Alive.Creature.State(prot);
        }

        #region Inspector
        public override string ToString() => "Alive World";

        private readonly pegi.EnterExitContext _context = new();
        private readonly LoopLock _inspectionLoopLock = new();
       
        public override void Inspect()
        {
            base.Inspect();

            if (_inspectionLoopLock.Unlocked)
            {
                using (_inspectionLoopLock.Lock()) 
                {
                    Alive.Inspect.All();
                }

                return;
            }

            pegi.Nl();

            using (_context.StartContext())
            {
                "Config".PegiLabel().Edit_Enter_Inspect(ref Config).Nl();
            }
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_AliveWorld))] internal class Singleton_AliveWorldDrawer : PEGI_Inspector_Override { }
}
