using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;

namespace QuizCanners.Modules.SplinePath
{
    public  static partial class Spline 
    {
        internal static EditMode s_editMode;

        internal static List<Inst_SplinePath> Instances = new();

       

        public static Inst_SplinePath CurrentRoot 
        {
            get
            {
                var inst = Instances.TryGet(Instances.Count - 1);
                if (inst)
                    return inst;
                return null;
            }
        }

        internal enum EditMode { AddPoints, Move, LinkPoints, Curves }
    
        internal enum PathType { Ground, Arial }
    }
}