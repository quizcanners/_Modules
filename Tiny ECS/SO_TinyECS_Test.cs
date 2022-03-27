using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.TinyECS
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Tiny ECS/" + FILE_NAME)]
    public class SO_TinyECS_Test : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Tiny ECS Test";

        private readonly Zoo zoo = new();
        private readonly SomeQuestLogicWorld quest = new SomeQuestLogicWorld();

        private class SomeQuestLogicWorld : ITinyECSworld, IPEGI
        {
            public string WorldName => "Al Quests";

            private struct Quest : IComponentData, IPEGI_ListInspect
            {
                public int progress;

                public void InspectInList(ref int edited, int index)
                {
                    progress.ToString().PegiLabel().Write();
                }
            }

            private struct CompletedQuest : IComponentData
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
                    this.GetWorld().RunSystem<Quest, CompletedQuest>((ref Quest q) =>
                    {
                        q.progress += 2;
                    });

                    this.GetWorld().RunSystem((ref Quest q) =>
                    {
                        q.progress -= 1;
                    });
                }
            }
        }


        private class Zoo : ITinyECSworld, IPEGI, IGotReadOnlyName
        {
            public string WorldName => "Zoo World";

            public struct AnimalComponent : IComponentData
            {
                public string Sound;
            }

            private struct Tag : IComponentData
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
                zooWorld.RunSystem((ref AnimalComponent animal) => animal.Sound = "Meow"));

                MeasureSystem("ref pos ref speed", () =>
                zooWorld.RunSystem((ref PositionComponent pos, ref SpeedComponent speed) => pos.Position.x += 2 + speed.Speed));

                MeasureSystem("ref pos, check speed", () =>
                zooWorld.RunSystem<PositionComponent, SpeedComponent>((ref PositionComponent pos) => pos.Position.x += 2));

                void MeasureSystem(string name, Action action)
                {
                    //Task.Run(() =>
                    //{
                    using (Timer.Last(name).Start(operationsCount: zooWorld.GetCount()))
                        action.Invoke();
                    //});
                }
            }

            public string GetReadOnlyName() => "Controller for " + this.GetWorld().GetNameForInspector();

            #endregion
        }

        #region Inspector

        [SerializeField] private pegi.EnterExitContext _context = new(playerPrefId: "SelTstECS"); 

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
