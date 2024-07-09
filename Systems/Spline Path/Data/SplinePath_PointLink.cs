using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Modules.SplinePath
{
    public static partial class Spline
    {
        [Serializable]
        internal class Link : IPEGI_ListInspect, IPEGI
        {
            [SerializeField] private Point.Id _start = new();
            [SerializeField] private Point.Id _end = new();
            [SerializeField] internal BezierCurve curve = new();

            public float Width
            {
                get
                {
                    var s = Start;
                    var e = End;

                    if (s != null && e != null)
                    {
                        return Mathf.Min(s.Radius, e.Radius) * 2;
                    }

                    return 2;
                }
            }

            public Point Start => _start.GetEntity();
            public Point End =>_end.GetEntity();

            public bool TryGetLocalPosition(Unit unit, out Vector3 outPos)
            {
                if (!TryGetPoints(unit, out Point start, out Point end, out bool swapped))
                {
                    outPos = Vector3.zero;
                    return false;
                }

                var pos = curve.GetPoint(unit.Progress, start.localPosition, end.localPosition, inverted: swapped);

                if (unit.LinkOffsetFraction.magnitude > 0)
                {
                    float theWidth = Mathf.Lerp(start.Radius, end.Radius, swapped ? 1- unit.Progress : unit.Progress);
                    pos += unit.LinkOffsetFraction.ToVector3XZ() * theWidth;
                }

                outPos = pos;
                return true;
            }

            public Vector3 GetLocalNormal(Unit unit) 
            {
                bool swapped = !unit.startPoint.Equals(_start);

                return curve.GetNormal(unit.Progress, Start.localPosition, End.localPosition, inverted: swapped);
            }

            public void Move(Unit unit, Vector3 vector, out float leftoverFraction) 
            {
                if (!TryGetPoints(unit, out Point start, out Point end, out bool swapped)) 
                {
                    leftoverFraction = 1;
                    return;
                }

                var worldNormal = unit.GetTransform().TransformDirection(curve.GetNormal(unit.Progress, start.localPosition, end.localPosition, inverted: swapped));

                float forward = Vector3.Dot(worldNormal, vector.normalized);

                float direction;

              ///  if (unit.previousDirection == 0 || Mathf.Abs(forward) > 0.33f)
                //{
                    direction = forward > 0f ? 1f : -1f;
                    unit.previousWorldDirection = direction;
              //  }
              //  else
                  //  direction = unit.previousDirection;

                if (swapped)
                    direction = -direction;

                float moveAmount =  (vector.magnitude / Length);

                unit.Progress += direction * moveAmount;

                if (unit.Progress < 0 || unit.Progress > 1) 
                {
                    float extra = Mathf.Abs(unit.Progress - 0.5f) - 0.5f;

                    leftoverFraction = Mathf.Abs(extra/moveAmount);

                    unit.Progress = Mathf.Clamp01(unit.Progress);
                }
                else 
                {
                    leftoverFraction = 0;
                }
            }

            public bool Contains(Point point) =>
                (_start.TryGetEntity(out var entA) && entA == point) ||
                (_end.TryGetEntity(out var entB) && entB == point);

            public bool IsValid => Start != null && End != null;

            private bool TryGetPoints(Unit unit, out Point start, out Point end, out bool swapped)
            {
                start = Start;
                end = End;

                if (start == null || end == null)
                {
                    swapped = false;
                    return false;
                }

                swapped = !_start.Equals(unit.startPoint);

                return true;
            }

            public Point.Id GetDifferentFrom(Point.Id point) => point.Equals(_start) ? _end : _start;

            private readonly Gate.Vector3Value _positionDirty = new();
            private float feetDistance = new();
            public float Length 
            {
                get 
                {
                    if (Start == null || End == null) 
                    {
                        return feetDistance;    
                    }

                    var a = Start.localPosition;
                    var b = End.localPosition;

                    if ( _positionDirty.TryChange(a + b)) 
                    {
                        feetDistance = curve.CalculateLength(start: a, end: b);
                    }

                    return feetDistance;
                }
            }

            #region Inspector

            public void InspectInList(ref int edited, int index)
            {
                _start.InspectSelectPart();

                Icon.Swap.Click().OnChanged(()=> 
                {
                    (_start, _end) = (_end, _start);
                    curve.SwapVectors();
                    
                });

                _end.InspectSelectPart();

                if (Icon.Enter.Click())
                    edited = index;
            }

            public void OnSceneDraw(Transform root)
            {
                if (Start != null && End != null)
                {
                    switch (s_editMode)
                    {
                        case EditMode.Curves:
                            pegi.Handle.Bazier(curve, Start.GetWorldPosition(root), End.GetWorldPosition(root), Color.white, width: Width); break;
                        default:
                            pegi.Handle.Line(Start.GetWorldPosition(root), End.GetWorldPosition(root), Color.white, thickness: Width);
                            break;
                    }

                    curve.OnSceneDraw();
                } 
            }

            public override string ToString() => "{0} -> {1}".F(Start.GetNameForInspector(), End.GetNameForInspector());

            void IPEGI.Inspect()
            {
               
            }

            #endregion

            public Link() { }

            public Link(Point.Id a, Point.Id b) 
            {
                _start = a;
                _end = b;
            }

            [Serializable]
            public class Id : SmartId.IntGeneric<Link>
            {
                public Id() { } 
                public Id(Link link) 
                {
                    SetEntity(link);
                }
                protected override List<Link> GetEnities() => ( CurrentRoot && CurrentRoot.config) ? CurrentRoot.config.links : null;
            }
        }
    }
}
