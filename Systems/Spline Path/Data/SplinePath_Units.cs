using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Modules.SplinePath
{
    public static partial class Spline
    {
        public class Unit : IPEGI_ListInspect, IPEGI, IPEGI_Handles
        {

            private readonly Inst_SplinePath _root;
            public float Progress;
            internal Point.Id startPoint = new();
            private Link.Id _link = new();
            internal Vector2 LinkOffsetFraction;

            private Vector3 _previousWorldPosition;

            public Transform GetTransform() => _root.transform;

            private enum UpdateState { Uninitialized, Moving, Finished }
            private UpdateState state;

            internal float previousWorldDirection;

            private readonly Gate.Frame _frameGate = new();

            Vector3 LocalPosition 
            {
                set 
                {
                    _previousWorldPosition = _root.transform.TransformPoint(value);
                }
            }

            public bool IsFinished => state == UpdateState.Finished;

            public Vector3 GetUpdatedLocalPosition(float speedPerSecond) => GetUpdatedWorldPosition(speedPerSecond, deltaTime: Time.deltaTime);
            
            public Vector3 GetUpdatedWorldPosition(float speedPerSecond, float deltaTime)
            {
                if (!_frameGate.TryEnter())
                    return _previousWorldPosition;

                if (Update(deltaTime: deltaTime, speedPerSecond, out Link path) && path.TryGetLocalPosition(this, out var localPos))
                {
                    LocalPosition = localPos;
                    return _previousWorldPosition;
                }

                if (startPoint.TryGetEntity(out Point point))
                    LocalPosition = point.localPosition;
                
                return _previousWorldPosition;
            }

            public Vector3 GetLastWorldPosition()
            {
                if (state == UpdateState.Uninitialized)
                    Update(deltaTime: 0, 1.25f, out _);

                if (TryGetPath(out Link link) && link.TryGetLocalPosition(this, out var localPos))
                {
                    LocalPosition = localPos;
                    return _previousWorldPosition;
                }
                
                Point start = startPoint.GetEntity();
                if (start != null)
                    LocalPosition = start.localPosition;
                
                return _previousWorldPosition;
            }
            
            private bool TryGetPath(out Link link) 
            {
                link = null;

                if (!TryGetStartPoint(out Point start))
                      return false;

                link = _link.GetEntity();

                if (link == null)
                {
                    link = start.direction.GetEntity();
                    if (link != null)
                    {
                        _link.SetEntity(link);
                    } else 
                    {
                        var anyLink = start.GetLinks().FirstOrDefault();
                        if (anyLink != null)
                        {
                            _link.SetEntity(anyLink);
                            link = anyLink;
                        } else 
                        {
                            state = UpdateState.Finished;
                        }
                    }
                }

                return link != null;

                bool TryGetStartPoint(out Point start)
                {
                    start = startPoint.GetEntity();

                    if (start != null)
                        return true;

                    var root = CurrentRoot;

                    if (!root)
                        return false;
                    
                    var cfg = root.config;

                    if (!cfg) 
                    {
                        return false;
                    }

                    Point.Id pnt = cfg.GetRandomStartPoint();

                    if (pnt != null)
                    {
                        startPoint.SetEntityId(pnt);
                    }
                    else
                    {
                        QcLog.ChillLogger.LogWarningOnce(() => "No starting points found for {0} {1}".F(nameof(SplinePath), nameof(Unit)), key: "No Spline Spawns");
                        return false;
                    }
                    
                    start = startPoint.GetEntity();

                    if (start == null)
                        return false;
                    
                    LinkOffsetFraction = UnityEngine.Random.insideUnitCircle;

                    return true;
                }

            }
            internal bool Update(float deltaTime, float speedPerTurn, out Link currentPath) 
            {
                switch (state) 
                {
                    case UpdateState.Finished: currentPath = null; return false;
                    case UpdateState.Uninitialized:
                        state = UpdateState.Moving; break;

                }

                if (!TryGetPath(out currentPath))
                {
                    state = UpdateState.Finished;
                    return false;
                }

                float moved = speedPerTurn * deltaTime;

                int maxIterations = 100;

                while (moved > 0)
                {
                    maxIterations--;
                    if (maxIterations < 0) 
                    {
                        Debug.LogError("Infinite motion loop");
                        break;
                    }

                    float linkLength = currentPath.Length;

                    Progress += (moved < linkLength) ? (moved / linkLength) : 1;

                    if (Progress <= 1)
                    {
                        break;
                    }
                    else
                    {
                        Point.Id newPointId = currentPath.GetDifferentFrom(startPoint);

                        if (newPointId.TryGetEntity(out var ent)) 
                        {
                            if (ent.role == Point.Role.EndPoint) 
                            {
                                state = UpdateState.Finished;
                                break;
                            }
                        }

                        moved = (Progress - 1) * linkLength;
                        Progress = 0;
                        if (!newPointId.TryGetEntity(out _)) 
                        {
                            state = UpdateState.Finished;
                            return false;
                        }

                        Link.Id newPath = newPointId.GetEntity().direction;

                        startPoint = newPointId;
                        _link = newPath;
                    
                        if (!TryGetPath(out Link nextPath))
                        {
                            return false;
                        }
                        currentPath = nextPath;
                    }
                }

                return true;
            }

            public bool TryMove (Vector3 vector) 
            {
                if (state == UpdateState.Uninitialized)
                    Update(deltaTime: 0, 1.25f, out _);

                if (_link == null)
                    return false;

                Link currentLink = _link.GetEntity();

                if (currentLink == null)
                    return false;
                
                currentLink.Move(this, vector, out float leftoverFraction);

                if (leftoverFraction > 0) 
                {
                    if (Progress > 0.99f)
                    {
                        startPoint = currentLink.GetDifferentFrom(startPoint);
                        Progress = 0;
                    }

                    previousWorldDirection = 1;

                    var links = startPoint.GetEntity().GetLinks();

                    if (links.Count > 1)
                    {
                        float closestDot = -10;

                        foreach (Link potentialLink in links)
                        {
                            if (currentLink == potentialLink)
                                continue;

                            var pathNormap = _root.transform.TransformDirection(potentialLink.GetLocalNormal(this));

                            float dot = Vector3.Dot(pathNormap, vector);

                            if (closestDot < dot)
                            {
                                closestDot = dot;
                                currentLink = potentialLink;
                            }
                        }

                        _link.SetEntity(currentLink);

                        vector *= leftoverFraction;

                        currentLink.Move(this, vector, out _);
                        
                    }

                }

                return true;
            }

            #region Inspector

            [SerializeField] private pegi.EnterExitContext context = new();

            private static Unit _selectedInEditor;

            void IPEGI.Inspect()
            {
                using (context.StartContext())
                {
                    if (context.IsAnyEntered == false)
                    {
                        "From".PegiLabel(50).Write();

                        startPoint.InspectSelectPart().Nl();//InspectInList_Nested().Nl();

                        var p = startPoint.GetEntity();

                        if (p != null)
                        {
                            var lnks = p.GetLinks();
                            Link l = _link.GetEntity();
                            if ("Link".PegiLabel(50).Select(ref l, lnks).Nl())
                                _link.SetEntity(l);
                        }

                        var lnk = _link.GetEntity();
                        if (lnk != null)
                        {
                            "Progress".PegiLabel(width: 60).Edit_01(ref Progress).Nl();
                        }
                    }
                }
            }

            public void InspectInList(ref int edited, int index)
            {
                "Progress".PegiLabel(width: 60).Edit_01(ref Progress).Nl();

                if (Icon.Enter.Click())
                    edited = index;
            }

            public void OnSceneDraw()
            {
                if (_link.TryGetEntity(out Link link)) 
                {
                    var p0 = link.Start;
                    var p1 = link.End;
                    if (p0 != null && p1 != null) 
                    {
                        var point = GetLastWorldPosition();

                        if (pegi.Handle.Button(point, Vector3.up, size: 0.2f, shape: pegi.SceneDraw.HandleCap.Cylinder))
                            _selectedInEditor = this;

                        if (this == _selectedInEditor) // progress > 0.2f && progress < 0.8f)
                        {
                            pegi.Handle.Label(this.GetNameForInspector(), point);
                        }
                    }
                }
            }

            #endregion

            public Unit(Inst_SplinePath root) 
            {
                _root = root;
            }

        }
    }
}