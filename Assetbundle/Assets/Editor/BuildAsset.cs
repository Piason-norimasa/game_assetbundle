
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
	public class FileVersion
	{
		public string Path { get; set; } = "";
		public string Hash { get; set; } = "";
	}

	public List<FileVersion> VersionList { get; private set; } = new List<FileVersion>();
}

public class BuildAssetbundle : MonoBehaviour
{
	private const string _outputDir              = "Output";
	private const string _assetDir               = "Asset";
	private const string _assetPackList          = "AssetPack";
	private const string _assetPackPrefix        = "Pack_";
	private const string _assetbundleExt         = ".unity3d";
	private const string _assetbundleManifestExt = ".manifest";
	private const string _versionListFileName    = "version.txt";

	private static BuildAssetBundleOptions buildOption =
		BuildAssetBundleOptions.ChunkBasedCompression
		//		| BuildAssetBundleOptions.ForceRebuildAssetBundle
		| BuildAssetBundleOptions.StrictMode
		| BuildAssetBundleOptions.DeterministicAssetBundle;

	class Asset
	{
		public string FileName { get; set; } = string.Empty;
		public string FileDir { get; set; } = string.Empty;
		public string ExportName { get; set; } = string.Empty;

		public string Hash { get; set; } = string.Empty;
	}
	
	class AssetPack
	{
		public List<string> FilePathList { get; set; } = new List<string>();
		public string FilePackDir { get; set; } = string.Empty;
		public string ExportName { get; set; } = string.Empty;

		public string Hash { get; set; } = string.Empty;
	}

	enum BuildType
	{
		Start = 0,
		
		iOS = Start,
		Android,
		Windows,

		End = Windows,
	}
	
	[MenuItem("Asset/Build AssetBundles(All)")]
	static void BuildAll()
	{
		Dictionary<BuildType, List<AssetBundleManifest>> manifestList = new Dictionary<BuildType, List<AssetBundleManifest>>();
		
		for (int i = (int)BuildType.Start; i < (int)BuildType.End; i++) {
			
			var fileList = GetAssetList();
			BuildAsset((BuildType)i, fileList);

			var filePackList = GetAssetPackList();
			BuildAssetPack((BuildType)i, filePackList);

			CreateAssetbundleVersionFile(BuildType.iOS, fileList, filePackList);
		}
	}

	[MenuItem("Asset/Build AssetBundles(iOS Only)")]
	static void BuildForiOS()
	{
		var fileList = GetAssetList();
		BuildAsset(BuildType.iOS, fileList);

		var filePackList = GetAssetPackList();
		BuildAssetPack(BuildType.iOS, filePackList);

		CreateAssetbundleVersionFile(BuildType.iOS, fileList, filePackList);
	}

	[MenuItem("Asset/Build AssetBundles(Android Only)")]
	static void BuildForAndroid()
	{
		var fileList = GetAssetList();
		BuildAsset(BuildType.Android, fileList);

		var filePackList = GetAssetPackList();
		BuildAssetPack(BuildType.Android, filePackList);

		CreateAssetbundleVersionFile(BuildType.Android, fileList, filePackList);
	}

	[MenuItem("Asset/Build AssetBundles(Windows Only)")]
	static void BuildForWindows()
	{
		var fileList = GetAssetList();
		BuildAsset (BuildType.Windows, fileList);
		
		var filePackList = GetAssetPackList();
		BuildAssetPack(BuildType.Windows, filePackList);

		CreateAssetbundleVersionFile(BuildType.Windows, fileList, filePackList);
	}
	
	private static bool BuildAsset(BuildType buildType, Dictionary<string, List<Asset>> assetList)
	{
		foreach (var key in assetList.Keys) {

			CreateOutputDirectory(buildType, key);

			List<AssetBundleBuild> buildAssetList = new List<AssetBundleBuild>();
			var asset = assetList[key][0];
			string filePath = "Assets/" + asset.FileDir + "/" + asset.FileName;
			AssetBundleBuild buildAsset = new AssetBundleBuild();
			buildAsset.assetBundleName = asset.ExportName;
			buildAsset.assetNames = new string[] { filePath };
			buildAssetList.Add(buildAsset);

			if (buildAssetList.Count == 0) {
				Debug.LogWarning("Why is folder empty???");
				continue;
			}
			
			string iosOutputDir = GetOutputDir(BuildType.iOS, key);
			string androidOutputDir = GetOutputDir(BuildType.Android, key);
			string windowsOutputDir = GetOutputDir(BuildType.Windows, key);
			if (buildType == BuildType.iOS) {
				var manifest = BuildPipeline.BuildAssetBundles(iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
				asset.Hash = manifest.GetAssetBundleHash(asset.ExportName).ToString();
			} else if (buildType == BuildType.Android) {
				var manifest = BuildPipeline.BuildAssetBundles(androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
				asset.Hash = manifest.GetAssetBundleHash(asset.ExportName).ToString();
			} else if (buildType == BuildType.Windows) {
				var manifest = BuildPipeline.BuildAssetBundles(windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
				asset.Hash = manifest.GetAssetBundleHash(asset.ExportName).ToString();
			}
		}

		return true;
	}

	private static bool BuildAssetPack(BuildType buildType, Dictionary<string, List<AssetPack>> assetList)
	{
		foreach (var key in assetList.Keys) {

			CreateOutputDirectory(buildType, key);

			List<AssetBundleBuild> buildAssetList = new List<AssetBundleBuild>();
			var asset = assetList[key][0];
			AssetBundleBuild buildAsset = new AssetBundleBuild();
			buildAsset.assetBundleName = asset.ExportName;
			buildAsset.assetNames = asset.FilePathList.ToArray();
			buildAssetList.Add(buildAsset);

			if (buildAssetList.Count == 0) {
				Debug.LogWarning("Why is folder empty???");
				continue;
			}
			
			string iosOutputDir = GetOutputDir(BuildType.iOS, key);
			string androidOutputDir = GetOutputDir(BuildType.Android, key);
			string windowsOutputDir = GetOutputDir(BuildType.Windows, key);
			if (buildType == BuildType.iOS) {
				var manifest = BuildPipeline.BuildAssetBundles(iosOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.iOS);
				var hash = manifest.GetAssetBundleHash(asset.ExportName);
				asset.Hash = hash.ToString();
				
			} else if (buildType == BuildType.Android) {
				var manifest = BuildPipeline.BuildAssetBundles(androidOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.Android);
				asset.Hash = manifest.GetAssetBundleHash(asset.ExportName).ToString();

			} else if (buildType == BuildType.Windows) {
				var manifest = BuildPipeline.BuildAssetBundles(windowsOutputDir, buildAssetList.ToArray(), buildOption, BuildTarget.StandaloneWindows64);
				asset.Hash = manifest.GetAssetBundleHash(asset.ExportName).ToString();

			}
		}
		
		return true;
	}

	/// <summary>
	/// create: resource version file
	/// </summary>
	private static void CreateAssetbundleVersionFile(BuildType buildType, Dictionary<string, List<Asset>> assetList, Dictionary<string, List<AssetPack>> assetPackList)
	{
		string outputForDir = GetOutputDir(buildType, "");

		StreamWriter yamlWriter = new StreamWriter(Path.Combine(outputForDir, _versionListFileName));
		ResourceVersion resourceVersion = new ResourceVersion();

		foreach (var list in assetList) {

			var asset = list.Value[0];
			ResourceVersion.FileVersion fileVersion = new ResourceVersion.FileVersion();
			fileVersion.Path = list.Key + "/" + asset.ExportName;
			fileVersion.Hash = asset.Hash;
			resourceVersion.VersionList.Add(fileVersion);
		}

		foreach (var list in assetPackList) {

			var asset = list.Value[0];
			ResourceVersion.FileVersion fileVersion = new ResourceVersion.FileVersion();
			fileVersion.Path = list.Key + "/" + asset.ExportName;
			fileVersion.Hash = asset.Hash;
			resourceVersion.VersionList.Add(fileVersion);
		}
		
		YamlDotNet.Serialization.Serializer yamlSerializer = new YamlDotNet.Serialization.Serializer();
		string yamlStr = yamlSerializer.Serialize(resourceVersion);
		Debug.Log(yamlStr);
		
		yamlWriter.Write(yamlStr);
		yamlWriter.Close ();
	}

	private static string GetAssetbundleRootPath()
	{
		return Application.dataPath;
	}

	///	<summary>
	///	output:	dir
	///	</summary>
	private static string GetOutputDir(BuildType buildType, string subDir)
	{
		switch (buildType) 
		{
		case BuildType.Android:
			return Path.Combine(_outputDir, "Android") + "\\" + subDir;
		case BuildType.iOS:
			return Path.Combine(_outputDir, "iOS") + "\\" + subDir;
		default:
			return Path.Combine(_outputDir, "Windows") + "\\" + subDir;
		}
	}

	private static void CreateOutputDirectory(BuildType buildType, string subDir)
	{
		// create output root dir
		if (!Directory.Exists(_outputDir)) {
			Directory.CreateDirectory(_outputDir);
		}
		
		// create output root dir
		string androidDir = GetOutputDir(buildType, "");
		if (!Directory.Exists(androidDir)) {
			Directory.CreateDirectory(androidDir);
		}

		// create output sub dirs
		string[] dirList = subDir.Split('/');
		string curDir = androidDir;
		foreach (var d in dirList) {
			curDir = Path.Combine(curDir, d);
			if (!Directory.Exists(curDir)) {
				Directory.CreateDirectory(curDir);
			}
		}
	}

	/// <summary>
	///	seach: single asset
	///	</summary>
	private static Dictionary<string, List<Asset>> GetAssetList()
	{
		List<Asset> list = SearchAsset(_assetDir);
		Dictionary<string, List<Asset>> retList = new Dictionary<string, List<Asset>>();

		foreach (var o in list) {
			if (retList.ContainsKey(o.FileDir)) {
				retList[o.FileDir].Add(o);
			} else {
				retList[o.FileDir] = new List<Asset>();
				retList[o.FileDir].Add(o);
			}
		}
		return retList;
	}

	private static List<Asset> SearchAsset(string currentDir)
	{
		List<Asset> retList = new List<Asset>();
		List<Asset> rootFileList = GetAssetFileList(currentDir);
		retList.AddRange(rootFileList);
		
		// sub sirectory search
		string findDir = GetAssetbundleRootPath() + "/" + currentDir;
		string[] dirList = Directory.GetDirectories(findDir, "*", System.IO.SearchOption.TopDirectoryOnly);
		foreach (var dirPath in dirList) {
			FileInfo fileInfo = new FileInfo(dirPath);
			string subDirectory = currentDir + "/" + fileInfo.Name;
			List<Asset> subDirList = SearchAsset(subDirectory);
			retList.AddRange(subDirList);
		}
		return retList;
	}

	private static List<Asset> GetAssetFileList(string dirPath)
	{
		List<Asset> assetList = new List<Asset>();

		string findDir = GetAssetbundleRootPath() + "/" + dirPath;
		string[] fileList = Directory.GetFiles(findDir);
		foreach (var file in fileList) {
			if (System.IO.Path.GetExtension(file) == ".meta")
				continue;

			Asset asset = new Asset();
			asset.FileDir = dirPath;
			asset.FileName = new FileInfo(file).Name;
			asset.ExportName = Path.GetFileNameWithoutExtension (asset.FileName) + _assetbundleExt;
			assetList.Add (asset);
		}
		return assetList;
	}

	/// <summary>
	///	search:	pack asset
	///	</summary>
	private static Dictionary<string, List<AssetPack>> GetAssetPackList()
	{
		List<AssetPack> assetList = SearchPackingAsset(_assetPackList);
		Dictionary<string, List<AssetPack>> retList = new Dictionary<string, List<AssetPack>>();

		foreach (var o in assetList) {
			if (retList.ContainsKey(o.FilePackDir)) {
				retList[o.FilePackDir].Add(o);
			} else {
				retList[o.FilePackDir] = new List<AssetPack>();
				retList[o.FilePackDir].Add(o);
			}
		}
		return retList;
	}
	
	private static List<AssetPack> SearchPackingAsset(string currentDir)
	{
		List<AssetPack> assetList = new List<AssetPack>();
		
		string findDir = GetAssetbundleRootPath() + "/" + currentDir;
		string[] dirList = Directory.GetDirectories(findDir, "*", System.IO.SearchOption.TopDirectoryOnly);
		foreach (var dirPath in dirList) {

			FileInfo fileInfo = new FileInfo(dirPath);
			string dirName = fileInfo.Name;
			string subDirectory = currentDir + "/" + dirName;

			if (dirName.Contains(_assetPackPrefix)) {
				AssetPack assetPack = CreatePackingAsset(subDirectory);
				if (assetPack != null) {
					assetList.Add(assetPack);
				}
			} else {
				List<AssetPack> subDirList = SearchPackingAsset(subDirectory);
				assetList.AddRange(subDirList);
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

		int index = currentDir.LastIndexOf("/");
		assetPack.FilePackDir = index < 0 ? currentDir : currentDir.Substring(0, index);

		string[] dirList = currentDir.Split('/');
		assetPack.ExportName = dirList[dirList.Length - 1].Replace(_assetPackPrefix, "") + _assetbundleExt;

		return assetPack;
	}

	private static List<string> GetPackingFilePathList(string currentDir)
	{
		List<string> retList = new List<string> ();

		// add:	current	directory files
		string findDir = GetAssetbundleRootPath() + "/" + currentDir;
		string[] fileList = Directory.GetFiles(findDir);
		foreach (var file in fileList) {
			if (System.IO.Path.GetExtension(file) == ".meta")
				continue;
			
			FileInfo fileInfo = new FileInfo(file);
			string filePath = Path.Combine(currentDir, fileInfo.Name);
			filePath = "Assets/" + filePath;
			retList.Add(filePath);
		}

		// add:	sub	directory files
		string[] dirList = Directory.GetDirectories(findDir);
		foreach (var dir in dirList) {
			FileInfo fileInfo = new FileInfo(dir);
			string dirName = fileInfo.Name;
			string subDirectory = currentDir + "/" + dirName;
			List<string> retSubFileList = GetPackingFilePathList(subDirectory);
			retList.AddRange(retSubFileList);
		}

		return retList;
	}
}