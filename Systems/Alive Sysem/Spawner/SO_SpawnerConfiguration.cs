using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;


namespace QuizCanners.AliveWorld
{
    using static Alive.Spawner;

    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Alive/" + FILE_NAME)]
    public class SO_SpawnerConfiguration : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Spawner Config";

        [SerializeField] public List<Prototype> Spawns;
        [SerializeField] public Alive.Creature.Activity InitialState;
        [SerializeField] internal int monstersToSpawn = 10;
        [SerializeField] internal float spawnDelay = 1f;
        [SerializeField] internal int maxSimultaneousMonsters = 5;

        public bool TryGetSpawnPoint(out Vector3 point, PointRequest request) 
        {
            point = Vector3.zero;

            var root = request.root;

            if (!root.spawnDelayGate.WillAllowIfTimePassed(spawnDelay))
                return false;

            if (request.CurrentMonsterCount >= maxSimultaneousMonsters)
                return false;

            if (root.monstersSpawned >= monstersToSpawn)
            {
                root.allSpawned = true;
                return false;
            }

            List<State> states = root.states;

            while (states.Count < Spawns.Count) 
            {
                states.Add(new State());
            }

            root.spawnIterator %= Spawns.Count;

            while (root.spawnIterator < Spawns.Count) 
            {
                if (Spawns[root.spawnIterator].TryGetPoint(out point, states[root.spawnIterator], request))
                {
                    root.spawnIterator++;
                    root.monstersSpawned++;
                    root.spawnDelayGate.Update();
                    return true;
                }
                root.spawnIterator++;
            }

            root.spawnIterator = 0;

            return false;
        }

        #region Inspector

        pegi.CollectionInspectorMeta _collectionMeta; // = new("Spawns");

        void IPEGI.Inspect()
        {
            Spawns ??= new();

            _collectionMeta ??= new("Spawns");// = new("Spawns");)

            if (!_collectionMeta.IsAnyEntered) 
            {
                "To Spawn".PegiLabel().Edit(ref monstersToSpawn).Nl();
                "Delay".PegiLabel().Edit(ref spawnDelay).Nl();
                "Max Simultaneous".PegiLabel().Edit(ref maxSimultaneousMonsters).Nl();
                "Initial Mood".PegiLabel().Edit_Enum(ref InitialState).Nl();
            }

            _collectionMeta.Edit_List(Spawns).Nl();
        }

        public void OnSceneDraw(Transform parent)
        {
            var changes = pegi.ChangeTrackStart();

            foreach (var spawner in Spawns) 
            {
                spawner.OnSceneDraw(parent);
            }

            if (changes)
                this.SetToDirty();
        }
        #endregion

    }
}
