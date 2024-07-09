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
        internal class Point : IGotName, IPEGI_ListInspect, IPEGI
        {
            [SerializeField] public float Radius = 1;
           
            [SerializeField] internal Vector3 localPosition;
            [SerializeField] internal Link.Id direction = new();
            [SerializeField] private string _name = "";
            [SerializeField] public Role role; 

            public Vector3 GetWorldPosition(Transform root) => root.TransformPoint(localPosition);
            public void SetWorldPosition(Vector3 newPosition, Transform root) => localPosition = root.InverseTransformPoint(newPosition);

            public enum Role { Transit, EndPoint, Spawner }

            public Id GetId() => new(this);

            public bool Equals(Point obj)
            {
                return obj.localPosition.Equals(localPosition) && obj._name.Equals(_name);
            }

            public List<Link> GetLinks() 
            {
                var allLinks = CurrentRoot.config.links;
                var filtered = new List<Link>();

                foreach (var l in allLinks)
                    if (l.Contains(this))
                        filtered.Add(l);

                return filtered;
            }

            #region Inspector
            public string NameForInspector
            {
                get => _name;
                set => _name = value;
            }

            public override string ToString() => "{0} {1}".F(_name, role.ToString().SimplifyTypeName());

            private static Id _selectedPoint;

            private bool IsSelected
            {
                get => _selectedPoint != null && _selectedPoint.Equals(GetId());
                set
                {
                    if (value)
                        _selectedPoint = GetId();
                    else 
                    {
                        if (IsSelected)
                            _selectedPoint = null;
                    }
                }
            }

            public static void InspectSelected() 
            {
                if (_selectedPoint != null)
                {
                    _selectedPoint.GetEntity().Nested_Inspect();
                }
                else
                    "No points selected".PegiLabel().Write_Hint();

                pegi.Nl();
            }

            public void OnSceneDraw(Transform root)
            {
                if (direction.TryGetEntity(out var link))
                {
                    if (link.GetDifferentFrom(GetId()).TryGetEntity(out var other))
                        pegi.Handle.Arrow(GetWorldPosition(root), other.GetWorldPosition(root), Color.green, _name.GetHashCode());
                }

                switch (s_editMode)
                { 
                    case EditMode.Move:

                        if (IsSelected)
                        {
                            pegi.Handle.DrawWireDisc(GetWorldPosition(root), 1, Vector3.up);

                            if (pegi.Handle.IsAlt() && pegi.Handle.TryGetLeftMouseClickPosition(out var hitPos))
                            {
                                SetWorldPosition(hitPos, root);
                            }
                        }
                        pegi.Handle.Position(GetWorldPosition(root), out var newPos).OnChanged(() =>
                        {
                            SetWorldPosition(newPos, root);
                            IsSelected = true;
                        });

                        break;

                    case EditMode.LinkPoints:

                        if (_selectedPoint == null)
                        {
                            if (Click(pegi.SceneDraw.HandleCap.Sphere))
                                IsSelected = true;

                            break;
                        }

                        if (IsSelected)
                        {
                            if (Click(pegi.SceneDraw.HandleCap.Rectangle))
                                IsSelected = false;

                            pegi.Handle.DrawWireDisc(GetWorldPosition(root), Radius, Vector3.up);

                            if (pegi.Handle.TryGetRayFromMouse(out var ray) && Physics.Raycast(ray, out var hit))
                            {
                                pegi.Handle.Line(GetWorldPosition(root), hit.point, Color.blue, thickness: 2);
                            }

                            break;
                        }


                        if (Click(pegi.SceneDraw.HandleCap.Sphere))
                        {
                            var newLink = new Link(_selectedPoint, GetId());

                            SO_SplinePath.s_inspected.links.Add(newLink);

                            _selectedPoint.GetEntity().direction = new Link.Id(newLink);

                            _selectedPoint = GetId();
                            //Create Line
                            //Or Destroy line
                        }

                        bool Click(pegi.SceneDraw.HandleCap cap) => pegi.Handle.Button(GetWorldPosition(root), label: _name, shape: cap);
                        
                        break;

                    default:

                        if (pegi.Handle.FreeMove(GetWorldPosition(root), out var movedPos))
                            SetWorldPosition(movedPos, root); // position = movedPos;
                        break;
                }

                pegi.Handle.Label(ToString(), GetWorldPosition(root), offset: Vector3.up * 2);

                if (direction.TryGetEntity(out var dir)) 
                {
                    pegi.Gizmo.Ray(GetWorldPosition(root), dir.curve.StartVector);
                }  
            }

            [SerializeField] private pegi.EnterExitContext context = new ();

            void IPEGI.Inspect()
            {
                using (context.StartContext())
                {
                    if (context.IsAnyEntered == false)
                    {
                        "Local Position".PegiLabel(60).Edit(ref localPosition).Nl();
                        "Radius".PegiLabel(60).Edit(ref Radius).Nl();
                        "Role".PegiLabel(60).Edit_Enum(ref role).Nl();

                        if (IsSelected) 
                        {
                            switch (s_editMode) 
                            {
                                case EditMode.Move:

                                    "Use Alt to move selected point to pointed area".PegiLabel().Write_Hint().Nl();

                                    break;
                            }
                        }

                        if (role != Role.EndPoint)
                        {
                            var lnk = direction.GetEntity();
                            if ("Direction".PegiLabel(60).Select(ref lnk, GetLinks()).Nl())
                                direction.SetEntity(lnk);
                        }
                    }
                }
            }

            public void InspectInList(ref int edited, int index)
            {
                this.inspect_Name();

                pegi.Edit_Enum(ref role);

                if (role != Role.EndPoint)
                {
                    var lnk = direction.GetEntity();
                    "->".PegiLabel(30).Select(ref lnk, GetLinks()).OnChanged(() => direction.SetEntity(lnk));
                }

                if (Icon.Enter.Click())
                    edited = index;
            }
            #endregion


            [Serializable]
            public class Id : SmartId.StringGeneric_Cached<Point>
            {
                private int _version;

                protected override bool IsDirty
                {
                    get => CurrentRoot ? _version != CurrentRoot.Version : true;
                    set 
                    {
                        if (value)
                            _version = -1;
                        else
                            _version = CurrentRoot.Version;
                    }
                }

                public Id() { }
                public Id(Point point)
                {
                    SetEntity(point);
                }
                protected override Dictionary<string, Point> GetEnities()
                {
                    var root = CurrentRoot;

                    if (!root)
                        return null;

                    if (!root.config)
                        return null;

                    return root.config.points;
                }
            }
   

            [Serializable]
            public class SerializableDictionary : SerializableDictionary<string, Point> { }
        }
    }
}