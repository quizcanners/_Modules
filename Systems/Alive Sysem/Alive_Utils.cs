using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace QuizCanners.AliveWorld
{
    public  static partial class Alive
    {

        public const string ADD_COMPONENT_MENU = QcUtils.QUIZCANNERS + "/Alive AI";

        public static Singleton_AliveWorld MGMT => Singleton.Get<Singleton_AliveWorld>();
        public static List<C_AliveRegion> s_regions = new();

        public static bool TryGetNearestRegion(Region.Type type, NavMeshAgent agent, out C_AliveRegion region) 
        {
            float nearest = float.MaxValue;

            Vector3 origin = agent.transform.position;

            region = null;

            NavMeshPath path = new();

            foreach (var r in s_regions) 
            {
                if (r.Type != type)
                    continue;

                if (Vector3.Distance(r.transform.position, origin) > nearest)
                    continue;

                if (!NavMesh.CalculatePath(origin, r.transform.position, NavMesh.AllAreas, path))
                    continue;

                float len = path.GetLength();

                if (len > nearest)
                    continue;

                region = r;
                nearest = len;
            }

            return region;
        }

        public static class Inspect
        {
            private static readonly pegi.EnterExitContext context = new();
            private static readonly pegi.CollectionInspectorMeta regionsMeta = new("Regions");


            internal static pegi.ChangesToken All()
            {
                var changes = pegi.ChangeTrackStart();

                using (context.StartContext()) 
                {
                    regionsMeta.Enter_List(s_regions).Nl();

                    MGMT.Enter_Inspect().Nl();
                }


                return changes;
            }
        }

        public static class Region 
        {
            public enum Type 
            {
                Trail,
                PatrolSpot,
                Camp,

            }
        }
    }
}
