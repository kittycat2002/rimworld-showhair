using System.IO;
using UnityEditor;

namespace Editor.Scripts
{
	public static class CreateAssetBundles
	{
		private const string AssetBundleDirectory = "Assets/AssetBundles";

		[MenuItem("Assets/Build AssetBundles")]
		public static void BuildAllAssetBundles()
		{
			BuildFor(BuildTarget.StandaloneWindows64);
		}

		private static void BuildFor(BuildTarget target)
		{
			string fullDir = Path.Combine(AssetBundleDirectory, $"{target}");
			if (!Directory.Exists(fullDir))
			{
				Directory.CreateDirectory(fullDir);
			}
			BuildPipeline.BuildAssetBundles(fullDir, BuildAssetBundleOptions.ChunkBasedCompression, target);
		}
	}
}