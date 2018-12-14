
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Samples.Helpers;
using YamlDotNet.Serialization;

public class ResourceVersion
{
	[System.Serializable]
	public class FileVersion
	{
		public string path = "";
		public string crc = "";
	}

	public List<FileVersion> versionList = new List<FileVersion>();
}

public class BuildAssetbundle : MonoBehaviour
{
	private const string outputDir 				= "Output";
	private const string assetDir 				= "Asset";
	private const string assetPackList 			= "AssetPack";
	private const string assetPackPrefix 		= "Pack_";
	private const string assetbundleExt			= ".unity3d";
	private const string assetbundleManifestExt = ".manifest";
	private const string versionListFileName 	= "version.txt";

	private static BuildAssetBundleOptions buildOption = 
		BuildAssetBundleOptions.ChunkBasedCompression
		//		| BuildAssetBundleOptions.ForceRebuildAssetBundle
		| BuildAssetBundleOptions.StrictMode
		| BuildAssetBundleOptions.DeterministicAssetBundle;

	class Asset
	{
		public string FileName { get; set; }
		public string FileDir { get; set; }
		public string ExportName { get; set; }
	}

	class AssetPack
	{
		public List<string> FilePathList = new List<string> ();
		public string FilePackDir { get; set; }
		public string ExportName { get; set; }
	}

	enum BuildType
	{
		All 		= 0,

		iOS 		= 1,
		Android,
		Windows,
	}

	[MenuItem("Asset/Build AssetBundles(All)")]
	static void BuildAll()
	{
		var fileList = GetAssetList();
//		BuildAsset (BuildType.All, fileList);

		var filePackList = GetAssetPackList();
//		BuildAssetPack (BuildType.All, filePackList);

		CreateAssetbundleVersionFile(BuildType.iOS);
		CreateAssetbundleVersionFile(BuildType.Android);
		CreateAssetbundleVersionFile(BuildType.Windows);
	}

	[MenuItem("Asset/Build AssetBundles(iOS Only)")]
	static void BuildForiOS()
	{
		var fileList = GetAssetList();
		BuildAsset (BuildType.iOS, fileList);

		var filePackList = GetAssetPackList();
		BuildAssetPack (BuildType.iOS, filePackList);

		CreateAssetbundleVersionFile(BuildType.iOS);
	}

	[MenuItem("Asset/Build AssetBundles(Android Only)")]
	static void BuildForAndroid()
	{
		var fileList = GetAssetList();
		BuildAsset (BuildType.Android, fileList);

		var filePackList = GetAssetPackList();
		BuildAssetPack (BuildType.Android, filePackList);

		CreateAssetbundleVersionFile(BuildType.Android);
	}

	[MenuItem("Asset/Build AssetBundles(Windows Only)")]
	static void BuildForWindows()
	{
		var fileList = GetAssetList();
		BuildAsset (BuildType.Windows, fileList);

		var filePackList = GetAssetPackList();
		BuildAssetPack (BuildType.Windows, filePackList);

		CreateAssetbundleVersionFile(BuildType.Windows);
	}

	private static bool BuildAsset(BuildType buildType, Dictionary<string, List<Asset>> assetList)
	{
		// 
		foreach (var key in assetList.Keys) {

			CreateOutputDirectory(BuildType.Android, key);
			CreateOutputDirectory(BuildType.iOS, key);
			CreateOutputDirectory(BuildType.Windows, key);

			List<AssetBundleBuild> buildAssetList = new List<AssetBundleBuild>();
			foreach (var o in assetList[key]) {
				string filePath = "Assets/" + o.FileDir + "/" + o.FileName;
				AssetBundleBuild buildAsset = new AssetBundleBuild();
				buildAsset.assetBundleName = o.ExportName;
				buildAsset.assetNames = new string[] {filePath};
				buildAssetList.Add (buildAsset);
			}

			if (buildAssetList.Count == 0) {
				Debug.LogWarning ("Why is folder empty???");
				continue;
			}

			string iosOutputDir = GetOutputDir(BuildType.iOS, key);
			string androidOutputDir = GetOutputDir(BuildType.Android, key);
			string windowsOutputDir = GetOutputDir(BuildType.Windows, key);
			if (buildType == BuildType.iOS) {
				BuildPipeline.BuildAssetBundles (iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
			} else if (buildType == BuildType.Android) {
				BuildPipeline.BuildAssetBundles (androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
			} else if (buildType == BuildType.Windows) {
				BuildPipeline.BuildAssetBundles (windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
			} else if (buildType == BuildType.All) {
				BuildPipeline.BuildAssetBundles (iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
				BuildPipeline.BuildAssetBundles (androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
				BuildPipeline.BuildAssetBundles (windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
			}
		}

		return true;
	}

	private static bool BuildAssetPack(BuildType buildType, Dictionary<string, List<AssetPack>> assetList)
	{
		foreach (var key in assetList.Keys) {

			CreateOutputDirectory(BuildType.Android, key);
			CreateOutputDirectory(BuildType.iOS, key);
			CreateOutputDirectory(BuildType.Windows, key);

			List<AssetBundleBuild> buildAssetList = new List<AssetBundleBuild>();
			foreach (var o in assetList[key]) {
				AssetBundleBuild buildAsset = new AssetBundleBuild();
				buildAsset.assetBundleName = o.ExportName;
				buildAsset.assetNames = o.FilePathList.ToArray();
				buildAssetList.Add (buildAsset);
			}

			if (buildAssetList.Count == 0) {
				Debug.LogWarning ("Why is folder empty???");
				continue;
			}

			string iosOutputDir = GetOutputDir(BuildType.iOS, key);
			string androidOutputDir = GetOutputDir(BuildType.Android, key);
			string windowsOutputDir = GetOutputDir(BuildType.Windows, key);
			if (buildType == BuildType.iOS) {
				BuildPipeline.BuildAssetBundles (iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
			} else if (buildType == BuildType.Android) {
				BuildPipeline.BuildAssetBundles (androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
			} else if (buildType == BuildType.Windows) {
				BuildPipeline.BuildAssetBundles (windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
			} else if (buildType == BuildType.All) {
				BuildPipeline.BuildAssetBundles (iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
				BuildPipeline.BuildAssetBundles (androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
				BuildPipeline.BuildAssetBundles (windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
			}
		}

		return true;
	}

	/// <summary>
	/// create: resource version file
	/// </summary>
	private static void CreateAssetbundleVersionFile(BuildType buildType)
	{
		string outputForDir = GetOutputDir(buildType, "");
		string [] fileList = Directory.GetFiles(outputForDir, "*" + assetbundleManifestExt, System.IO.SearchOption.AllDirectories);
		StreamWriter yamlWriter = new StreamWriter (Path.Combine(outputForDir, versionListFileName));
		ResourceVersion resourceVersion = new ResourceVersion ();

		foreach (var file in fileList) {

			// load assetbundle.manifest (yaml format)
			string fullPath = Application.dataPath + "/../" + file;
			StreamReader reader = new StreamReader (fullPath);
			string content = reader.ReadToEnd ();
			var stringReader = new StringReader(content);
			reader.Close();

			var yaml = new YamlStream();
			yaml.Load(stringReader);

			// Examine the stream
			var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;
			var crcNode = rootNode.Children [new YamlScalarNode ("CRC")];
			string assetbundle = file.Replace (assetbundleManifestExt, "");
			if (assetbundle.Contains(assetbundleExt) && File.Exists (assetbundle)) {
				assetbundle = assetbundle.Replace (outputForDir, "");
				ResourceVersion.FileVersion fileVersion = new ResourceVersion.FileVersion();
				fileVersion.path = assetbundle;
				fileVersion.crc = ((YamlScalarNode)crcNode).Value;
				resourceVersion.versionList.Add (fileVersion);
			}
		}

		YamlDotNet.Serialization.Serializer yamlSerializer = new YamlDotNet.Serialization.Serializer();
		string yamlStr = yamlSerializer.Serialize (resourceVersion);
		yamlWriter.Write(yamlStr);
		Debug.Log (yamlStr);
		string jsonStr = JsonUtility.ToJson(resourceVersion);
		Debug.Log (jsonStr);
//		yamlWriter.Write (jsonStr);

		yamlWriter.Close ();
	}

	private static string GetAssetbundleRootPath()
	{
		return Application.dataPath;
	}

	/// <summary>
	/// output: dir
	/// </summary>
	private static string GetOutputDir(BuildType buildType, string subDir)
	{
		switch (buildType) 
		{
		case BuildType.Android:
			return Path.Combine(outputDir, "Android") + "/" + subDir;
		case BuildType.iOS: 
			return Path.Combine(outputDir, "iOS") + "/" + subDir;
		default:
			return Path.Combine(outputDir, "Windows") + "/" + subDir;
		}
	}

	private static void CreateOutputDirectory(BuildType buildType, string subDir)
	{
		// create output root dir
		if (!Directory.Exists (outputDir)) {
			Directory.CreateDirectory (outputDir);
		}

		// create output root dir 
		string androidDir = GetOutputDir (buildType, "");
		if (!Directory.Exists (androidDir)) {
			Directory.CreateDirectory (androidDir);
		}

		// create output sub dirs
		string [] dirList = subDir.Split ('/');
		string curDir = androidDir;
		foreach (var d in dirList) {
			curDir = Path.Combine(curDir, d);
			if (!Directory.Exists (curDir)) {
				Directory.CreateDirectory (curDir);
			}
		}
	}

	/// <summary>
	/// seach: single asset
	/// </summary>
	private static Dictionary<string, List<Asset>> GetAssetList()
	{
		List<Asset> list = SearchAsset(assetDir);
		Dictionary<string, List<Asset>> retList = new Dictionary<string, List<Asset>>();

		foreach (var o in list) {
			if (retList.ContainsKey(o.FileDir)) {
				retList [o.FileDir].Add(o);
			} else {
				retList [o.FileDir] = new List<Asset>();
				retList [o.FileDir].Add(o);
			}
		}
		return retList;
	}

	private static List<Asset> SearchAsset(string currentDir)
	{
		List<Asset> retList = new List<Asset>();
		List<Asset> rootFileList = GetAssetFileList (currentDir);
		retList.AddRange(rootFileList);

		// sub sirectory search
		string findDir = GetAssetbundleRootPath() + "/" + currentDir;
		string [] dirList = Directory.GetDirectories(findDir, "*", System.IO.SearchOption.TopDirectoryOnly);
		foreach (var dirPath in dirList) {
			FileInfo fileInfo = new FileInfo(dirPath);
			string subDirectory = currentDir + "/" + fileInfo.Name;
			List<Asset> subDirList = SearchAsset (subDirectory);
			retList.AddRange(subDirList);
		}
		return retList;
	}

	private static List<Asset> GetAssetFileList(string dirPath)
	{
		List<Asset> assetList = new List<Asset>();

		string findDir = GetAssetbundleRootPath () + "/" + dirPath;
		string [] fileList = Directory.GetFiles (findDir);
		foreach (var file in fileList) {
			if (System.IO.Path.GetExtension (file) == ".meta")
				continue;
			
			Asset asset = new Asset();
			asset.FileDir = dirPath;
			asset.FileName = new FileInfo(file).Name;
			asset.ExportName = Path.GetFileNameWithoutExtension (asset.FileName) + assetbundleExt;
			assetList.Add (asset);
		}
		return assetList;
	}

	/// <summary>
	/// search: pack asset
	/// </summary>
	private static Dictionary<string, List<AssetPack>> GetAssetPackList()
	{
		List<AssetPack> assetList = SearchPackingAsset(assetPackList);
		Dictionary<string, List<AssetPack>> retList = new Dictionary<string, List<AssetPack>>();

		foreach (var o in assetList) {
			if (retList.ContainsKey(o.FilePackDir)) {
				retList [o.FilePackDir].Add(o);
			} else {
				retList [o.FilePackDir] = new List<AssetPack>();
				retList [o.FilePackDir].Add(o);
			}
		}
		return retList;
	}

	private static List<AssetPack> SearchPackingAsset(string currentDir)
	{
		List<AssetPack> assetList = new List<AssetPack>();

		string findDir = GetAssetbundleRootPath() + "/" + currentDir;
		string [] dirList = Directory.GetDirectories(findDir, "*", System.IO.SearchOption.TopDirectoryOnly);
		foreach (var dirPath in dirList) {

			FileInfo fileInfo = new FileInfo(dirPath);
			string dirName = fileInfo.Name;
			string subDirectory = currentDir + "/" + dirName;
			if (dirName.Contains(assetPackPrefix)) {
				AssetPack assetPack = CreatePackingAsset (subDirectory);
				if (assetPack != null) {
					assetList.Add (assetPack);
				}
			} else {
				List<AssetPack> subDirList = SearchPackingAsset(subDirectory);
				assetList.AddRange (subDirList);
			}
		}

		return assetList;
	}

	private static AssetPack CreatePackingAsset(string currentDir)
	{
		List<string> assetFileList = GetPackingFilePathList(currentDir);
		if (assetFileList.Count == 0) {
			return null;
		}

		AssetPack assetPack = new AssetPack();
		assetPack.FilePathList = assetFileList;

		int index = currentDir.LastIndexOf ("/");
		assetPack.FilePackDir = index < 0 ? currentDir : currentDir.Substring(0, index);

		string [] dirList = currentDir.Split ('/');
		assetPack.ExportName = dirList[dirList.Length - 1].Replace(assetPackPrefix, "") + assetbundleExt;

		return assetPack;
	}

	private static List<string> GetPackingFilePathList(string currentDir)
	{
		List<string> retList = new List<string> ();

		// add: current directory files
		string findDir = GetAssetbundleRootPath () + "/" + currentDir;
		string [] fileList = Directory.GetFiles (findDir);
		foreach (var file in fileList) {
			if (System.IO.Path.GetExtension (file) == ".meta")
				continue;
			
			FileInfo fileInfo = new FileInfo(file);
			string filePath = Path.Combine (currentDir, fileInfo.Name);
			filePath = "Assets/" + filePath;
			retList.Add(filePath);
		}

		// add: sub directory files
		string [] dirList = Directory.GetDirectories (findDir);
		foreach (var dir in dirList) {
			FileInfo fileInfo = new FileInfo(dir);
			string dirName = fileInfo.Name;
			string subDirectory = currentDir + "/" + dirName;
			List<string> retSubFileList = GetPackingFilePathList (subDirectory);
			retList.AddRange (retSubFileList);
		}
		return retList;
	}
}