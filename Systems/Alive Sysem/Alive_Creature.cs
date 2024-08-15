using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace QuizCanners.AliveWorld
{
    public static partial class Alive
    {
        public static class Creature
        {
         
            public class State : IPEGI
            {
                public Activity Activity;
                public Combat Combat;
                public Prototype Prototype;

                public C_AliveRegion currentDestination;

                public float ThreadDetection01;

                public bool IsRunning => Activity == Activity.Combat;

                private bool IsPointInHearingRange(float distance, out float detectionRate, float noise) 
                {
                    float effectiveRange = Prototype.HearingRange * noise;

                    if (distance > effectiveRange) 
                    {
                        detectionRate = 0;
                        return false;
                    }

                    detectionRate = 1f - distance / effectiveRange;

                    return true;
                }

                public DetectionType IsPointInsideDetectionSector(Vector3 myPosition, Vector3 forward, Vector3 pointPosition, float noise, out float detectionRate) 
                {
                    var diff = pointPosition - myPosition;
                    var distance = diff.magnitude;

                    DetectionType detected = IsPointInHearingRange(distance, out detectionRate, noise: noise) ? DetectionType.Hearing : DetectionType.Undetected;

                    float EffectiveSightRange = (Activity == Activity.Combat ? 4f : 1f) * Prototype.SightRange;

                    if (distance > EffectiveSightRange)
                        return detected;

                    float angleSize01 = 1 - (Vector3.Dot(forward, diff.normalized) + 1) * 0.5f;
                    if (angleSize01 > Prototype.ViewWingAndle01) 
                        return detected;

                    detectionRate = Mathf.Max(detectionRate, 1f - distance / EffectiveSightRange);

                    return DetectionType.Vision;
                }

                public void DetectPlayerInstantly() 
                {
                    Activity = Activity.Combat;
                    Combat = Combat.Attacking;
                    ThreadDetection01 = 1;
                }

                private Gate.UnityTimeScaled _detectionUpdateTimer = new(Gate.InitialValue.Uninitialized);

                public bool TryDetectThreat(bool threatVisible, float detectionRate) 
                {
                    float deltaTime = Mathf.Clamp((float)_detectionUpdateTimer.GetSecondsDeltaAndUpdate(), 0, 0.5f);

                    if (deltaTime == 0)
                        return false;

                    if (threatVisible) 
                    {
                        ThreadDetection01 = Mathf.Min(1, ThreadDetection01 + deltaTime * detectionRate / Prototype.DetectionDuration);

                        if (ThreadDetection01 >= 0.99f && Activity != Activity.Combat)
                        {
                            currentDestination = null;
                            Activity = Activity.Combat;
                            Combat = Combat.Attacking;
                            return true;
                        }

                        return Activity == Activity.Combat;
                    } else 
                    {
                        ThreadDetection01 = Mathf.Max(0, ThreadDetection01 - deltaTime / Prototype.AlertCooldownDuration);

                        if (ThreadDetection01 < 0.01f)
                        {
                            switch (Activity)
                            {
                                case Activity.Combat: Activity = Activity.Patrol; break;
                            }
                            return false;
                        }

                        return true;
                    }
                }

                public bool TryGetNewTravelDestination(out Vector3 targtPosition, NavMeshAgent agent) 
                {
                    targtPosition = Vector3.zero;

                    switch (Activity)
                    {
                        case Activity.Combat:
                           
                            switch (Combat) 
                            {
                                case Combat.Attacking: return false;
                                default: return false;
                            }

                        case Activity.Patrol: TryGoTo(Region.Type.PatrolSpot); break;
                        case Activity.Rest: TryGoTo(Region.Type.Camp); break;

                        default: Debug.LogError(QcLog.CaseNotImplemented(Activity)); return false;
                    }

                    if (!currentDestination)
                        return false;

                    targtPosition = currentDestination.transform.position;
                    return true;
                    

                    bool TryGoTo(Region.Type type) 
                    {
                        if (currentDestination || TryGetNearestRegion(type, agent, out currentDestination))
                        {
                            return true;
                        }

                        return false;
                    }
                }

                public State (Prototype prototype) 
                {
                    Prototype = prototype;
                    Activity = Activity.Patrol;
                }

                #region Inspector
                public override string ToString() => "{0} {1}".F(Activity, Prototype.ToString());

                void IPEGI.Inspect()
                {
                    "Activity".PegiLabel(60).Edit_Enum(ref Activity).Nl();
                    if (Activity == Activity.Combat) 
                    {
                        "Combat".PegiLabel(60).Edit_Enum(ref Combat).Nl();
                    }

                }

                #endregion
            }

            [Serializable]
            public class Prototype : IPEGI, IGotName
            {
                [SerializeField] private string _name;
                public float Stamina_01 = 0.5f;
                public float Courage_01 = 0.5f;
                public float SightRange = 20;
                public float HearingRange = 10;
                public float ViewWingAndle01 = 0.3f;
                public float NotifyRange = 30f;
                public float DetectionDuration = 2f;
                public float AlertCooldownDuration = 10f;

                public string NameForInspector { get => _name; set => _name = value; }

                public override string ToString() => _name + " AI";

                void IPEGI.Inspect()
                {
                    pegi.TryReflectionInspect(this);

                    /*
                    "Stamina".PegiLabel().Edit_01(ref Stamina_01).Nl();
                    "Courage".PegiLabel().Edit_01(ref Courage_01).Nl();
                    "Viewing Angle".PegiLabel().Edit(ref ViewWingAndle01, 0, 1);*/
                }
            }

            public enum Activity
            {
                Patrol = 0,
                Rest = 1,
                Combat = 2,
            }

            public enum Combat
            {
                Searching = 0,
                Attacking = 1,
                Excaping = 2,
                LookingForCover = 3,
            }

            public enum DetectionType { Undetected, Hearing, Vision }

        }
    }
}