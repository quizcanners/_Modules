using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.Modules
{
    public static partial class AddAptable
    {
        [Serializable]
        public class SegmentCfg : IPEGI, INeedAttention
        {
            public C_AdaptableBlock.MegaVoxelRole role = new();
            public BlockSetting[] matrix = new BlockSetting[27];

            public static BlockSetting[] s_holder = new BlockSetting[27];
            public static readonly int[] cubeRotMtx =
            {
                2, 4, 6,
                -2, 0, 2,
                -6, -4, -2
            };

           
            public BlockSetting this[int dx, int dy, int dz]
            {
                get => matrix[GetIndex(dx, dy, dz)];
                set => matrix[GetIndex(dx, dy, dz)] = value;
                
            }

            private int GetIndex(int dx, int dy, int dz)
            {
                if ((Mathf.Abs(dx) > 1) || (Mathf.Abs(dy) > 1) || (Mathf.Abs(dz) > 1))
                {
                    Debug.LogError("AddApt Index is outside of bounds: X={0}, Y={1}, Z={2}".F(dx,dy,dz));
                    return 0;
                }

                return (dy + 1) * 9 + (dz + 1) * 3 + dx + 1;
            }

            public bool IsVisible =>  !(matrix[4] == BlockSetting.Full && matrix[10] == BlockSetting.Full && matrix[12] == BlockSetting.Full && matrix[14] == BlockSetting.Full
                          && matrix[16] == BlockSetting.Full && matrix[22] == BlockSetting.Full);
            

            public void SetAllTo(BlockSetting to)
            {
                for (var i = 0; i < 27; i++)
                    matrix[i] = to;
            }

            public void CopyFrom(SegmentCfg from)
            {
                for (var i = 0; i < 27; i++)
                    matrix[i] = from.matrix[i];
            }

            public void Spin()
            {
                for (var i = 0; i < 3; i++)
                    for (var j = 0; j < 9; j++)
                    {
                        var ind = i * 9 + j;
                        s_holder[ind] = matrix[ind + cubeRotMtx[j]];
                    }

                for (var i = 0; i < 27; i++)
                    matrix[i] = s_holder[i];
            }

            private static readonly SegmentCfg TmpCubeCfg = new();

            private static int GetSimilarityScore(SegmentCfg a, SegmentCfg b)
            {

                var sum = 0;
                for (var i = 0; i < 27; i++)
                {
                    var am = a.matrix[i];
                    var bm = b.matrix[i];
                    if (am == bm)
                    {
                        sum += (am == BlockSetting.Any ? 0 : 2);
                        continue;
                    }

                    if (Check(am, bm, BlockSetting.ExternalContent, BlockSetting.Full))
                        sum += 0;
                    else if (Check(am, bm, BlockSetting.ExternalContent, BlockSetting.Empty))
                        sum -= 1;
                    else if (am != BlockSetting.Any && bm != BlockSetting.Any)
                        return -1;
                }

                bool Check(BlockSetting a, BlockSetting b, BlockSetting targetX, BlockSetting targetY) => (a == targetX && b == targetY) || (a == targetY && b == targetX);
                

                return sum;
            }

            public int CompareWithWorld(SegmentCfg world, ref int rot)
            {
                TmpCubeCfg.CopyFrom(this);

                var maxScore = -1;

                for (var i = 0; i < 4; i++)
                {
                    var val = GetSimilarityScore(world, TmpCubeCfg);
                    if (val > maxScore)
                    {
                        maxScore = val;
                        rot = i;
                    }

                    if (i < 3) 
                        TmpCubeCfg.Spin();
                }

                return maxScore;
            }

            #region Inspector

            private int _inspectedY;

            void IPEGI.Inspect()
            {
                Icon.Refresh.Click("Rotate").OnChanged(Spin);

                "Elevation: Y = {0}".F(_inspectedY).PegiLabel().Write();

                pegi.Nl();

                for (int z = -1; z < 2; z++)
                {
                    for (int x = -1; x < 2; x++)
                    {
                        if (x == 0 && z == 0 & _inspectedY == 0)
                        {
                            Icon.Discord.Click("Center");
                        }
                        else
                        {
                            switch (this[x, _inspectedY, z])
                            {
                                case BlockSetting.Any: Icon.InActive.Click(toolTip: "Any").OnChanged(() => this[x, _inspectedY, z] = BlockSetting.Full); break;
                                case BlockSetting.Full: Icon.Active.Click(toolTip: "Full").OnChanged(() => this[x, _inspectedY, z] = BlockSetting.Empty); break;
                                case BlockSetting.Empty: Icon.Close.Click(toolTip: "Empty").OnChanged(() => this[x, _inspectedY, z] = BlockSetting.Any); break;
                            }
                        }
                    }

                    "   ".PegiLabel(20).Write();

                    int floor = -z;

                    if (_inspectedY > floor)
                        (floor == 0 ? Icon.Down : Icon.DownLast).Click(()=> _inspectedY= floor);
                    else if (_inspectedY == floor)
                        Icon.Back.Draw();
                    else 
                        (floor == 0 ? Icon.Up : Icon.UpLast).Click(() => _inspectedY= floor);

                    pegi.Nl();
                }
            }

            public string NeedAttention()
            {
                if (this[0, 0, 0] != BlockSetting.Any)
                    return "Center is not Any";

                return null;
            }

            #endregion
        }

  

        public enum BlockSetting
        {
            Any,
            Empty,
            Full,
            ExternalContent,
        }
    }
}