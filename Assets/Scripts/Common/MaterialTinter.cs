using UnityEngine;

namespace bison.core.Common
{
    public static class MaterialTinter
    {
        private static MaterialPropertyBlock propertyBlock;

        public static void Apply(GameObject target, Color color, bool should_Multiply)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (IsEffectRenderer(renderer)) continue;

                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null) continue;

                    int colorPropertyId = FindColorProperty(material);
                    if (colorPropertyId < 0) continue;

                    renderer.GetPropertyBlock(propertyBlock, materialIndex);

                    Color finalColor = should_Multiply ? material.GetColor(colorPropertyId) * color : color;
                    propertyBlock.SetColor(colorPropertyId, finalColor);

                    renderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }

        private static bool IsEffectRenderer(Renderer renderer)
        {
            return renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer;
        }

        private static int FindColorProperty(Material material)
        {
            if (material.HasProperty(ShaderProperties.BaseColor)) return ShaderProperties.BaseColor;
            if (material.HasProperty(ShaderProperties.LegacyColor)) return ShaderProperties.LegacyColor;
            return -1;
        }
    }
}
