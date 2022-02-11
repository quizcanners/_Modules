using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Quiz Canners/" + Singleton_GameController.PROJECT_NAME + "/" + FILE_NAME)]
    public partial class SO_PersistentGameData : ScriptableObject, ICfg
    {
        public const string FILE_NAME = "Game States";

        [SerializeField] private CfgData _cfg;
        private readonly QcFile.RelativeLocation _saveLocation = new QcFile.RelativeLocation(folderName: "Data", fileName: FILE_NAME, asBytes: false);

        #region Saving & Loading

        public CfgEncoder Encode() => new CfgEncoder();
        public void DecodeTag(string key, CfgData data)
        {
            /*  switch (key) 
              {

              }*/
        }

        public void Save()
        {
            _cfg = Encode().CfgData;
            QcFile.Save.ToPersistentPath.JsonTry(objectToSerialize: this, _saveLocation);
        }
        public void Load()
        {
            var tmp = this;
            QcFile.Load.FromPersistentPath.TryOverrideFromJson(_saveLocation, ref tmp);
            this.Decode(_cfg);
        }

        #endregion

    }
}