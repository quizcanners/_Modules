using System;

namespace QuizCanners.AliveWorld
{
    using Inspect;
    using System.Collections.Generic;
    using UnityEngine;

    public static partial class Alive {
        public static class Spawner {

            public static readonly List<Inst_Alive_Spawner> s_activeSpawners = new();

            [Serializable]
            public class Prototype : IPEGI
            {
                [SerializeField] private string name = "Spawn Point";
                [SerializeField] private Vector3 _localPosition = Vector3.one;
                [SerializeField] private float _startSpawnDistance = 10f;
                [SerializeField] private float _stopSpawnDistance = 3f;

                public bool TryGetPoint(out Vector3 point, State state, PointRequest request) 
                {
                    var pos = request.root.transform.TransformPoint(_localPosition);

                    var dist = Vector3.Distance(pos, request.CurrentPlayerPosition);

                    if (dist<_startSpawnDistance && dist > _stopSpawnDistance) 
                    {
                        point = pos;
                        return true;
                    }

                    point = Vector3.zero;
                    return false;
                }

                #region Inspector

                public override string ToString() => name;

                void IPEGI.Inspect()
                {
                    "Name".PegiLabel(40).Edit(ref name).Nl();
                    "Range".PegiLabel().Edit_Range(ref _stopSpawnDistance, ref _startSpawnDistance);
                }

                public void OnSceneDraw(Transform parent)
                {
                    var worldPos = parent.TransformPoint(_localPosition);

                    pegi.Handle.Label(ToString(), worldPos + Vector3.up);

                    if (pegi.Handle.Position(worldPos, out var newWorldPos)) 
                    {
                        _localPosition = parent.InverseTransformPoint(newWorldPos);
                    }
                }

                #endregion
            }

            public class State : IPEGI
            {
                public override string ToString() => "Point State";

                #region Inspector

                void IPEGI.Inspect()
                {

                }

                #endregion
            }

            public class PointRequest 
            {
                public int CurrentMonsterCount;
                public Vector3 CurrentPlayerPosition;

                internal Inst_Alive_Spawner root;


            }
        }
    }
}
