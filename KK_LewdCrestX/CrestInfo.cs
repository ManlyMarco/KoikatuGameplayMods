using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_LewdCrestX
{
    public sealed class CrestInfo
    {
        private readonly AssetBundle _bundle;
        public readonly bool Implemented;
        public readonly string Description;
        public readonly CrestType Id;
        public readonly string Name;
        private Texture2D _tex;

        public CrestInfo(string id, string name, string description, AssetBundle bundle)
        {
            Id = (CrestType)Enum.Parse(typeof(CrestType), id);
            Implemented = LewdCrestXPlugin.ImplementedCrestTypes.Contains(Id);
            Name = name;
            Description = description;
            _bundle = bundle;
        }

        public Texture2D GetTexture()
        {
            if (_tex == null && Id > CrestType.None)
            {
                _tex = _bundle.LoadAsset<Texture2D>(Id.ToString()) ??
                       throw new Exception("Crest tex asset not found - " + Id);
                Object.DontDestroyOnLoad(_tex);
            }

            return _tex;
        }
    }
}