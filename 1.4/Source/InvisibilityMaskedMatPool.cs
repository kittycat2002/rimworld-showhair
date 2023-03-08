using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShowHair
{
    public static class InvisibilityMaskedMatPool
    {
        public static MethodInfo method = AccessTools.TypeByName("MaterialAllocator").GetMethod("Create");
        // Token: 0x060013A5 RID: 5029 RVA: 0x00078238 File Offset: 0x00076438
        public static Material GetInvisibleMaskedMat(Material baseMat, Texture maskTexture)
        {
            Material material;
            if (!materials.TryGetValue(baseMat, out material))
            {
                material = (Material)method.Invoke(null, new object[]{ baseMat });
                material.shader = ShaderDatabase.Invisible;
                material.SetTexture(NoiseTex, TexGame.InvisDistortion);
                material.SetTexture(NoiseTex, TexGame.InvisDistortion);
                material.color = color;
                materials.Add(baseMat, material);
            }
            return material;
        }

        // Token: 0x04001054 RID: 4180
        private static Dictionary<Material, Material> materials = new Dictionary<Material, Material>();

        // Token: 0x04001055 RID: 4181
        private static Color color = new Color(0.75f, 0.93f, 0.98f, 0.5f);

        // Token: 0x04001056 RID: 4182
        private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");
        private static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
    }
}
