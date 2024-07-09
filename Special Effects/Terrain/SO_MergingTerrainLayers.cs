using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = Utils.QcUnity.SO_CREATE_MENU + "Terrain/" + FILE_NAME)]
    public class SO_MergingTerrainLayers : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Merging Terrain Textures";

        public List<ChannelSetsForDefaultMaps> mergeSubMasks = new();


        #region Inspector
        private readonly pegi.CollectionInspectorMeta _collectionMeta = new("Merge Sub Masks");

        void IPEGI.Inspect()
        {
            _collectionMeta.Edit_List(mergeSubMasks).Nl();
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(SO_MergingTerrainLayers))] internal class SO_MergingTerrainLayers_Drawer : PEGI_Inspector_Override { }
}
