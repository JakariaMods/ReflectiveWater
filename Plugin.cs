using System;
using System.IO;
using VRage.Plugins;
using VRageRender;

namespace ReflectiveWaterShaders
{
	public class Plugin : IDisposable, IPlugin
	{
		public void Dispose()
		{
			ReflectiveWater.ReplaceOriginals();
		}

		public void Init(object gameInstance)
		{
			ReflectiveWater.CreateShaderFiles();
			ReflectiveWater.SwapFiles();
			MyRenderProxy.ReloadEffects();
		}

		public void Update()
		{
		}
	}

	public static class ReflectiveWater
	{
		static readonly string lighting = Path.Combine(MyShaderCompiler.ShadersPath, "Lighting/");
		static readonly string transparent = Path.Combine(MyShaderCompiler.ShadersPath, "Transparent/");

		public static void CreateShaderFiles()
		{
			if (!File.Exists(lighting + "EnvAmbient.hlsli.new"))
			{
				string newFile1 = File.ReadAllText(lighting + "EnvAmbient.hlsli");
				File.WriteAllText(lighting + "EnvAmbient.hlsli.original", newFile1);
				int start = newFile1.IndexOf("float3 AmbientDiffuse");
				newFile1 = newFile1.Insert(start, newText1);
				File.WriteAllText(lighting + "EnvAmbient.hlsli.new", newFile1);
			}

			if (!File.Exists(transparent + "Billboards.hlsl.new"))
			{
				string newFile2 = File.ReadAllText(transparent + "Billboards.hlsl");
				File.WriteAllText(transparent + "Billboards.hlsl.original", newFile2);
				newFile2 = newFile2.Replace("//#define REFLECTIVE", "#define REFLECTIVE");
				int start2 = newFile2.IndexOf("#ifdef REFLECTIVE");
				int end2 = newFile2.LastIndexOf("#ifdef OIT");
				newFile2 = newFile2.Remove(start2, end2 - start2);
				newFile2 = newFile2.Insert(start2, newText2);
				File.WriteAllText(transparent + "Billboards.hlsl.new", newFile2);
			}
		}

		public static void SwapFiles()
		{
			File.Copy(lighting + "EnvAmbient.hlsli.new", lighting + "EnvAmbient.hlsli", true);
			File.Copy(transparent + "Billboards.hlsl.new", transparent + "Billboards.hlsl", true);
		}

		public static void ReplaceOriginals()
		{
			File.Copy(lighting + "EnvAmbient.hlsli.original", lighting + "EnvAmbient.hlsli", true);
			File.Copy(transparent + "Billboards.hlsl.original", transparent + "Billboards.hlsl", true);
		}

		static readonly string newText1 =
			"float3 AmbientSpecularBillboard(float3 f0, float gloss, float3 N, float3 V, float farFactor)"
			+ "\n" + "{"
			+ "\n" + "    float nv = saturate(dot(N, V));"
			+ "\n" + "    float3 R = -reflect(V, N);"
			+ "\n" + "    R.x = -R.x;"
			+ "\n"
			+ "\n" + "    uint w, h, levels;"
			+ "\n" + "    SkyboxIBLCloseTex.GetDimensions(0, w, h, levels);"
			+ "\n" + "    levels -= frame_.Light.SkipIBLevels;"
			+ "\n" + "    float level = max((1 - gloss) * levels, 0) + frame_.Light.SkipIBLevels;"
			+ "\n" + "    float3 sample = SampleIBL(R, level, farFactor);"
			+ "\n"
			+ "\n" + "    return sample * f0;"
			+ "\n" + "}"
			+ "\n\n";

		static readonly string newText2 =
			"#ifdef REFLECTIVE"
			+ "\n"
			+ "\n" + "float reflective = BillboardBuffer[vertex.index].reflective;"
			+ "\n" + "	if (reflective )"
			+ "\n" + "	{"
			+ "\n" + "		float3 N = normalize(BillboardBuffer[vertex.index].normal);"
			+ "\n" + "		float3 viewVector = normalize(get_camera_position() - vertex.wposition);"
			+ "\n"
			+ "\n" + "		float3 reflectionSample = AmbientSpecularBillboard(0.04f, 0.95f, N, viewVector, -1);"
			+ "\n" + "		float4 color = CalculateColor(vertex, linearDepth, false, alphaCutout);"
			+ "\n" + "		float3 reflectionColor = lerp(color.xyz, reflectionSample, reflective);"
			+ "\n"
			+ "\n" + "		resultColor = float4(reflectionColor, max(color.w, reflective));"
			+ "\n" + "	}"
			+ "\n" + "	else"
			+ "\n" + "#endif"
			+ "\n" + "	{"
			+ "\n" + "		resultColor = CalculateColor(vertex, linearDepth, false, alphaCutout);"
			+ "\n" + "	}"
			+ "\n"
			+ "\n" + "#ifdef LIT_PARTICLE"
			+ "\n" + "	resultColor.xyz *= vertex.light;"
			+ "\n" + "#endif"
			+ "\n\n";
	}
}
