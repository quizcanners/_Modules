using UnityEngine;

namespace QuizCanners.AliveWorld
{
    using Inspect;
    using System.Collections.Generic;
    using System;
    using QuizCanners.Utils;

    [ExecuteAlways]
    [AddComponentMenu(Alive.ADD_COMPONENT_MENU + "/Enemy Instance")]
    public class Inst_Alive_Spawner : MonoBehaviour, IPEGI, IPEGI_Handles
    {
        [SerializeField] internal SO_SpawnerConfiguration config;
        [NonSerialized] internal readonly List<Alive.Spawner.State> states = new();
        internal int monstersSpawned;
        internal Gate.UnityTimeScaled spawnDelayGate = new(Gate.InitialValue.Uninitialized);
        internal bool allSpawned;
        internal int spawnIterator;

        public Alive.Creature.Activity InitialState => config.InitialState;

        public virtual bool AllSpawned() => allSpawned;

        public virtual bool TryGetSpawnPoint(out Vector3 point, Alive.Spawner.PointRequest request) 
        {
            request.root = this;

            var result = config.TryGetSpawnPoint(out point, request);

            if (result)
                monstersSpawned++;

            return result;
        }


        private void OnEnable()
        {
            Alive.Spawner.s_activeSpawners.Add(this);
        }

        private void OnDisable()
        {
            Alive.Spawner.s_activeSpawners.Remove(this);
        }

        #region Inspector

        internal static Inst_Alive_Spawner inspected;

        public override string ToString()
        {
            if (!config)
                return "No config";

            var soName = config.ToString();

            if (!Application.isPlaying)
                return soName;

            if (allSpawned)
                return "{0} Done".F(soName);


            return "{0} {1}/{2}".F(soName, monstersSpawned, config.monstersToSpawn);
        }
        public virtual void Inspect()
        {
            inspected = this;

            pegi.EditorView.Lock_UnlockClick(this);

            if (Application.isPlaying) 
            {
                "Spawned".PegiLabel().Edit(ref monstersSpawned);

                if (monstersSpawned > 0 && Icon.Refresh.Click())
                    monstersSpawned = 0;

                pegi.Nl();
            }

            "Config".PegiLabel(60).Edit_Inspect(newSoName: "Spawner config", addSceneName: true, ref config);

            pegi.Nl();
        }

        public void OnSceneDraw()
        {
            if (config)
                config.OnSceneDraw(transform);
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Inst_Alive_Spawner))]
    internal class Inst_Alive_SpawnerDrawer : PEGI_Inspector_Override { }
}
