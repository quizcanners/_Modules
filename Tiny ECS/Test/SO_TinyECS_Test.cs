using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    public class SO_TinyECS_Test : MonoBehaviour, IPEGI
    {
        public const string FILE_NAME = "Tiny ECS Test";

        private readonly Zoo zoo = new();
        private readonly SomeQuestLogicWorld quest = new();

        private class SomeQuestLogicWorld : ITinyECSworld, IPEGI
        {
            public string WorldName => "Al Quests";

            private struct Quest :  IPEGI_ListInspect
            {
                public int progress;

                public void InspectInList(ref int edited, int index)
                {
                    progress.ToString().PegiLabel().Write();
                }
            }

            private struct CompletedQuest 
            {

            }

            public void Inspect(IEntity entity)
            {
                entity.InspectComponent<Quest>();
                entity.InspectComponent<CompletedQuest>();

            }

            public void Inspect()
            {
                var world = this.GetWorld();

                world.Nested_Inspect();

                if ("Create Test".PegiLabel().Click().Nl()) 
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var ent = world.CreateEntity("Quest 0");

                        ent.AddComponent<Quest>();
                        ent.AddComponent<CompletedQuest>();

                        var ent1 = world.CreateEntity("Quest Complete");

                        ent1.AddComponent<Quest>();
                    }
                }

                if ("Run".PegiLabel().Click().Nl())
                {
                    this.GetWorld().WithAll<Quest>().AddFilter<CompletedQuest>().Run((ref Quest q) => q.progress += 2);

                    this.GetWorld().WithAll<Quest>().Run((ref Quest q) =>
                    {
                        q.progress -= 1;
                    });
                }
            }
        }

        private class Zoo : ITinyECSworld, IPEGI
        {
            public string WorldName => "Zoo World";

            private struct AnimalComponent 
            {
                public string Sound;
            }

            private struct PositionComponent :  IPEGI_ListInspect
            {
                public Vector3 Position;

                public void InspectInList(ref int edited, int index)
                {
                    "Position".PegiLabel(70).Edit(ref Position).Nl();
                }
            }

            private struct SpeedComponent :  IPEGI_ListInspect
            {
                public float Speed;

                public void InspectInList(ref int edited, int index)
                {
                    "Speed".PegiLabel(60).Edit(ref Speed).Nl();
                }
            }

            private struct Tag 
            {

            }

            #region Inspector

            public void Inspect(IEntity entity)
            {
                entity.InspectComponent<AnimalComponent>();
                entity.InspectComponent<PositionComponent>();
                entity.InspectComponent<Tag>();
            }

            [SerializeField] private pegi.EnterExitContext _context = new();

            public void Inspect()
            {
                using (_context.StartContext()) 
                {
                    this.GetWorld().Enter_Inspect();

                    if (_context.IsAnyEntered == false)
                    {
                        if ("+ Test Entities".PegiLabel().Click().Nl())
                        {
                            for (int i = 0; i < 1000; i++)
                            {
                                var world = this.GetWorld();

                                World<Zoo>.Entity e0 = world.CreateEntity("Running Cat " + i);
                                e0.AddComponent<PositionComponent>();
                                e0.AddComponent<SpeedComponent>();
                                e0.AddComponent<AnimalComponent>();

                                World<Zoo>.Entity e1 = world.CreateEntity("Sitting Cat");
                                e1.AddComponent<PositionComponent>();
                                e1.AddComponent<AnimalComponent>();

                                world.Destroy(e1);
                            }
                        }

                        if ("Run Systems".PegiLabel().Click().Nl())
                            SystemTest();

                        Timer.Nested_Inspect().Nl();
                    }
                }
            }

            QcDebug.TimeProfiler.DictionaryOfParallelTimers Timer => QcDebug.TimeProfiler.Instance["ECS_Test"];

            void SystemTest()
            {
                var zooWorld = this.GetWorld();

                MeasureSystem("ref Animal", () =>
                zooWorld.WithAll<AnimalComponent>().Run((ref AnimalComponent animal) => animal.Sound = "Meow"));

                MeasureSystem("ref pos ref speed", () =>
                zooWorld.WithAll<PositionComponent, SpeedComponent>().Run((ref PositionComponent pos, SpeedComponent speed) => pos.Position.x += 2 + speed.Speed));

                MeasureSystem("ref pos, check speed", () =>
                zooWorld.WithAll<PositionComponent>().Run((ref PositionComponent pos) => pos.Position.x += 2));

                void MeasureSystem(string name, Action action)
                {
                    //Task.Run(() =>
                    //{
                    using (Timer.Last(name).Start(operationsCount: zooWorld.GetCount()))
                        action.Invoke();
                    //});
                }
            }

            public override string ToString() => "Controller for " + this.GetWorld().GetNameForInspector();

            #endregion
        }

        #region Inspector

        private readonly pegi.EnterExitContext _context = new(playerPrefId: "SelTstECS"); 

        public void Inspect()
        {
            pegi.Nl();
            using (_context.StartContext())
            {
                zoo.Enter_Inspect().Nl();

                quest.Enter_Inspect().Nl();


                if (_context.IsAnyEntered == false && "Destroy Worlds".PegiLabel().Click())
                    zoo.GetWorld().ClearWorld();
            }
        }


        #endregion
    }

    [PEGI_Inspector_Override(typeof(SO_TinyECS_Test))] internal class SO_TinyECS_TestDrawer : PEGI_Inspector_Override { }
}
