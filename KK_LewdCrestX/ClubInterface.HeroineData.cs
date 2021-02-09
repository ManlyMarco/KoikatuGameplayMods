using KKAPI.Utilities;
using UnityEngine;

namespace KK_LewdCrestX
{
    internal static partial class ClubInterface
    {
        private sealed class HeroineData
        {
            public readonly SaveData.Heroine Heroine;
            public readonly LewdCrestXController Controller;
            private string _heroineName;
            private Texture2D _faceTex;

            public HeroineData(SaveData.Heroine heroine)
            {
                Heroine = heroine;
                Controller = heroine.GetCrestController();

                _heroineName = Heroine.parameter.fullname;
                TranslationHelper.TranslateAsync(_heroineName, s => _heroineName = s);
            }

            public string GetCrestName() => LewdCrestXPlugin.CrestInfos.TryGetValue(Controller.CurrentCrest, out var ci) ? ci.Name : "No crest";

            public string HeroineName => _heroineName;

            public Texture GetFaceTex()
            {
                if (_faceTex == null)
                {
                    var origTex = Heroine.charFile.facePngData.LoadTexture();
                    var scale = 84f / origTex.width;
                    _faceTex = origTex.ResizeTexture(TextureUtils.ImageFilterMode.Average, scale);
                    Object.Destroy(origTex);
                }

                return _faceTex;

            }

            public void Destroy()
            {
                Object.Destroy(_faceTex);
            }
        }
    }
}