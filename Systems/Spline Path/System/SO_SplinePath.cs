using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Modules.SplinePath
{
    using static Spline;

    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Spline/" + FILE_NAME)]
    public partial class SO_SplinePath : ScriptableObject, IPEGI, IGotName
    {
        public const string FILE_NAME = "Pulse Path Configs";

        [SerializeField] internal Point.SerializableDictionary points = new();
        [SerializeField] internal List<Link> links = new();
        [SerializeField] private string _name = "Pulse Arena";

        [NonSerialized] private List<Point.Id> _spawnPoints;

        internal Point.Id GetRandomStartPoint() 
        {
            if (_spawnPoints.IsNullOrEmpty()) 
            {
                _spawnPoints = new List<Point.Id>();
                foreach (var pair in points) 
                {
                    var p = pair.Value;
                    if (p.role == Point.Role.Spawner)
                        _spawnPoints.Add(p.GetId());
                }
            }

            if (_spawnPoints.Count == 0)
                return null;

            return _spawnPoints.GetRandom();

        }

        #region Inspector
        [SerializeField] private pegi.EnterExitContext conext = new();
        [SerializeField] private pegi.CollectionInspectorMeta _linksMeta = new("Links");
        [SerializeField] private pegi.CollectionInspectorMeta _pointMeta = new("Points");

        internal static SO_SplinePath s_inspected;

        public string NameForInspector { get => _name; set => _name = value; }

        void IPEGI.Inspect()
        {
            s_inspected = this;

            var changes = pegi.ChangeTrackStart();

            using (conext.StartContext())
            {
                _pointMeta.Enter_Dictionary(points).Nl();
                _linksMeta.Enter_List(links).Nl();

                if (conext.IsAnyEntered == false)
                {
                    Point.InspectSelected();

                    if ("Clear All".PegiLabel().ClickConfirm(confirmationTag: "ClrSplPnts"))
                    {
                        points.Clear();
                        links.Clear();
                    }

                    if (Application.isPlaying == true && "Refresh Spawn points cache".PegiLabel().Click().Nl())
                        _spawnPoints = null;
                }

            }

            if (changes)
            {
                _spawnPoints = null;
                Debug.Log("Was made dirty", this);
            }
        }

        public void OnSceneDraw(Transform root)
        {
            var changes = pegi.ChangeTrackStart();
            s_inspected = this;

            foreach (KeyValuePair<string, Point> p in points)
                p.Value.OnSceneDraw(root);

            foreach (var l in links)
                l.OnSceneDraw(root);

            if (s_editMode == EditMode.AddPoints && pegi.Handle.TryGetRayFromMouse(out var ray))
            {
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    pegi.Handle.Label(s_editMode.ToString().SimplifyTypeName(), hit.point, offset: Vector3.up * 2);

                    if (pegi.Handle.IsLeftMouseButtonDown())
                    {
                        var newPoint = new Point()
                        {
                            localPosition = root.InverseTransformPoint(hit.point),
                            role = points.Count==0 ? Point.Role.Spawner : Point.Role.Transit,
                            Radius = 1,
                        };

                        string key = (points.Count == 0 ? "Start" : "") + " ({0})".F(points.Count + 1);

                        while (points.ContainsKey(key)) 
                        {
                            key += "B";
                        }

                        newPoint.NameForInspector = key;

                        points.Add(key, newPoint);
                    }
                }
            }


            if (changes)
            {
                pegi.Handle.SceneSetDirty(this);
                Debug.Log("Setting scene dirty", this);
            }
        }
        #endregion
    }
}