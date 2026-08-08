using System.IO;
using UnityEditor;
using UnityEngine;

namespace Treeline.LevelEditorTools
{
    /// <summary>
    /// 손전등용 라이트 쿠키를 절차적으로 굽는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 쿠키는 조명 앞에 끼우는 흑백 마스크다. 스포트라이트 원뿔 안의 밝기를 위치마다
    /// 깎아 준다. 이것이 없으면 원뿔 안이 전부 같은 밝기라 「손전등」이 아니라
    /// 「원뿔 모양으로 밝은 구역」으로 보인다.
    /// </para>
    /// <para>
    /// 실제 손전등의 특징 셋을 넣는다 — 가운데 <b>핫스팟</b>, 가장자리로 가파른
    /// <b>감쇠</b>, 반사판이 만드는 희미한 <b>동심 링</b>. 여기에 아주 약한 얼룩을
    /// 얹어 완벽한 원이 주는 인공적인 느낌을 없앤다.
    /// </para>
    /// <para>
    /// 텍스처 한 장이라 <b>씬에 묶이지 않는다.</b> 어느 씬의 어느 <see cref="Light"/> 든
    /// 같은 에셋을 참조하면 된다 — 2장 손전등, 6장 손전등, 1장 서치라이트까지.
    /// 광원을 늘리지 않으므로 WebGL 에서 비용이 없다.
    /// </para>
    /// </remarks>
    public static class FlashlightCookie
    {
        private const string Dir = "Assets/Game/Levels/Lighting";
        private const string Path = Dir + "/FlashlightCookie.png";
        private const int Size = 512;

        [MenuItem("Tools/Treeline/B — 2장/손전등 쿠키 굽기")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
            {
                AssetDatabase.CreateFolder("Assets/Game/Levels", "Lighting");
            }

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            Color[] px = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // 중심에서의 거리를 0~1 로. 1 이 원뿔 가장자리다
                    float nx = (x + 0.5f) / Size * 2f - 1f;
                    float ny = (y + 0.5f) / Size * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);

                    float v;
                    if (r >= 1f)
                    {
                        // 원 밖은 완전히 0 이어야 한다. 안 그러면 사각형 모서리가 보인다
                        v = 0f;
                    }
                    else
                    {
                        // 핫스팟 — 가운데 20% 는 거의 최대
                        float core = 1f - Smooth(0.10f, 0.62f, r);

                        // 바깥 halo — 핫스팟 밖으로 약하게 퍼진다
                        float halo = (1f - Smooth(0.35f, 1.00f, r)) * 0.42f;

                        v = Mathf.Clamp01(core + halo);

                        // 반사판 링 — 아주 약하게. 있는지 모를 정도가 맞다
                        v *= 1f + 0.045f * Mathf.Sin(r * 26f);

                        // 얼룩 — 완벽한 원의 인공적인 느낌을 깬다
                        float blot = Mathf.PerlinNoise(nx * 2.3f + 11.7f, ny * 2.3f + 4.1f);
                        v *= Mathf.Lerp(0.88f, 1f, blot);

                        // 가장자리를 확실히 0 으로 물린다
                        v *= 1f - Smooth(0.93f, 1f, r);
                    }

                    v = Mathf.Clamp01(v);
                    // IMPORTANT: URP 는 스포트 쿠키에서 **RGB** 를 읽는다(색 쿠키도 된다).
                    // 알파에만 넣으면 조명에 아무 영향이 없고 프리뷰도 온통 흰색이라
                    // 잘못된 것을 눈으로 확인할 수조차 없다.
                    px[y * Size + x] = new Color(v, v, v, 1f);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings();

            Debug.Log($"손전등 쿠키를 구웠다: {Path}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
        }


        /// <summary>
        /// GLSL 의 <c>smoothstep</c>. <paramref name="x"/> 를 [e0,e1] 구간에서 0~1 로 재사상한다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <see cref="Mathf.SmoothStep"/> 를 쓰면 안 된다. 이름은 같지만
        /// **from 과 to 사이를 보간**하는 다른 함수다. <c>Mathf.SmoothStep(0.93f, 1f, 0f)</c>
        /// 는 0 이 아니라 0.93 을 돌려준다. 이걸 GLSL 처럼 쓰면 마스크가 통째로
        /// 어두워지는데, 모양은 맞고 밝기만 틀려서 알아채기 어렵다.
        /// </remarks>
        private static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 쿠키로 쓰려면 임포트 설정이 맞아야 한다. 기본값으로 두면 그냥 그림이다.
        /// </summary>
        private static void ApplyImportSettings()
        {
            TextureImporter imp = AssetImporter.GetAtPath(Path) as TextureImporter;
            if (imp == null)
            {
                Debug.LogError("임포터를 못 찾았다: " + Path);
                return;
            }

            // Unity 6 의 URP 는 스포트라이트 쿠키로 평범한 2D 텍스처를 그대로 받는다.
            // 예전에 있던 TextureImporterSettings.cookieLightType 은 없어졌다.
            imp.textureType = TextureImporterType.Default;

            // IMPORTANT: Repeat 이면 원뿔 밖으로 무늬가 타일링되어 벽에 격자가 생긴다
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;

            // 밝기 마스크라 색 공간 보정을 받으면 안 된다
            imp.sRGBTexture = false;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.mipmapEnabled = true;

            imp.SaveAndReimport();
        }
    }
}
