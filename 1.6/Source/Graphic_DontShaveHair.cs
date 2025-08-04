using System;
using UnityEngine;
using Verse;

namespace ShowHair;

public class Graphic_DontShaveHair : Graphic_Multi
{
	internal bool isDifferentFromMulti;

	public override void Init(GraphicRequest req)
	{
		data = req.graphicData;
		path = req.path;
		maskPath = req.maskPath;
		color = req.color;
		colorTwo = req.colorTwo;
		drawSize = req.drawSize;
		string basePath = req.path[..req.path.LastIndexOf("_", StringComparison.Ordinal)];
		Texture2D?[] array = new Texture2D?[mats.Length];
		array[0] = ContentFinder<Texture2D>.Get($"{req.path}_north", false);
		array[1] = ContentFinder<Texture2D>.Get($"{req.path}_east", false);
		array[2] = ContentFinder<Texture2D>.Get($"{req.path}_south", false);
		array[3] = ContentFinder<Texture2D>.Get($"{req.path}_west", false);
		if (array[0] == null & array[1] == null && array[2] == null && array[3] == null)
		{
#if DEBUG
			Log.Message($"{req.path} does not have any textures.");
#endif
			isDifferentFromMulti = false;
			return;
		}
#if DEBUG
		Log.Message($"{req.path} has textures.");
#endif
		isDifferentFromMulti = true;
		array[0] ??= ContentFinder<Texture2D>.Get($"{basePath}_north", false);
		array[1] ??= ContentFinder<Texture2D>.Get($"{basePath}_east", false);
		array[2] ??= ContentFinder<Texture2D>.Get($"{basePath}_south", false);
		array[3] ??= ContentFinder<Texture2D>.Get($"{basePath}_west", false);
		if (array[0] == null)
		{
			if (array[2] != null)
			{
				array[0] = array[2];
				drawRotatedExtraAngleOffset = 180f;
			}
			else if (array[1] != null)
			{
				array[0] = array[1];
				drawRotatedExtraAngleOffset = -90f;
			}
			else if (array[3] != null)
			{
				array[0] = array[3];
				drawRotatedExtraAngleOffset = 90f;
			}
			else
			{
				array[0] = ContentFinder<Texture2D>.Get(req.path, reportFailure: false);
			}
		}

		if (array[0] == null)
		{
			Log.Error($"Failed to find any textures at {req.path} while constructing {this.ToStringSafe()}");
			mats[0] = mats[1] = mats[2] = mats[3] = BaseContent.BadMat;
			return;
		}

		if (array[2] == null)
		{
			array[2] = array[0];
		}

		if (array[1] == null)
		{
			if (array[3] != null)
			{
				array[1] = array[3];
				eastFlipped = DataAllowsFlip;
			}
			else
			{
				array[1] = array[0];
			}
		}

		if (array[3] == null)
		{
			if (array[1] != null)
			{
				array[3] = array[1];
				westFlipped = DataAllowsFlip;
			}
			else
			{
				array[3] = array[0];
			}
		}

		Texture2D[] array2 = new Texture2D[mats.Length];
		if (req.shader.SupportsMaskTex())
		{
			string text = maskPath.NullOrEmpty() ? path : maskPath;
			string text2 = maskPath.NullOrEmpty() ? "m" : string.Empty;
			array2[0] = ContentFinder<Texture2D>.Get(text + "_north" + text2, reportFailure: false);
			array2[1] = ContentFinder<Texture2D>.Get(text + "_east" + text2, reportFailure: false);
			array2[2] = ContentFinder<Texture2D>.Get(text + "_south" + text2, reportFailure: false);
			array2[3] = ContentFinder<Texture2D>.Get(text + "_west" + text2, reportFailure: false);
			if (array2[0] == null)
			{
				if (array2[2] != null)
				{
					array2[0] = array2[2];
				}
				else if (array2[1] != null)
				{
					array2[0] = array2[1];
				}
				else if (array2[3] != null)
				{
					array2[0] = array2[3];
				}
			}

			if (array2[2] == null)
			{
				array2[2] = array2[0];
			}

			if (array2[1] == null)
			{
				if (array2[3] != null)
				{
					array2[1] = array2[3];
				}
				else
				{
					array2[1] = array2[0];
				}
			}

			if (array2[3] == null)
			{
				if (array2[1] != null)
				{
					array2[3] = array2[1];
				}
				else
				{
					array2[3] = array2[0];
				}
			}
		}

		for (int i = 0; i < mats.Length; i++)
		{
			MaterialRequest req2 = new()
			{
				mainTex = array[i],
				shader = req.shader,
				color = color,
				colorTwo = colorTwo,
				maskTex = array2[i],
				shaderParameters = req.shaderParameters,
				renderQueue = req.renderQueue
			};
			mats[i] = MaterialPool.MatFrom(req2);
		}
	}

	public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
	{
		return GraphicDatabase.Get<Graphic_DontShaveHair>(path, newShader, drawSize, newColor, newColorTwo, data,
			maskPath);
	}
}