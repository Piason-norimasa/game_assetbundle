
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AssetBundleBrowser.AssetBundleDataSource;


namespace AssetBundleBrowser
{

    class AssetBundleList
    {
        public static readonly string VersionFileName = "assetbundlelist.json";
        public static readonly string AssetBundleFolder = "AssetBundles";

        [Serializable]
        public class AssetData
        {
            public string Hash  = "";
            public string Path  = "";
        }
        [Serializable]
        public class AssetVersion
        {
            public string CreatedTime = "";
            public List<AssetData> Items = new List<AssetData>();
        }

        public List<string> CurrentUploadList { get; private set; }  = new List<string>();
        public AssetBundleUploaderTab.UploaderTarget SelectTarget { get; private set; } = AssetBundleUploaderTab.UploaderTarget.Android;

        public void Refresh(AssetBundleUploaderTab.UploaderTarget target)
        {
            SelectTarget = target;
            CurrentUploadList = GetUploadFileList();
        }

        private string GetVersionFile(AssetBundleUploaderTab.UploaderTarget target)
        {
            string filePath = GetBuildAssetFolderPath(target) + "/" + VersionFileName;
            string file = FileUtil.ReadText(filePath);

            return file;
        }

        private string GetBuildAssetFolderPath(AssetBundleUploaderTab.UploaderTarget target)
        {
            string filePath = "";
            switch (target)
            {
                case AssetBundleUploaderTab.UploaderTarget.iOS:
                {
                    filePath = Application.dataPath + "/../" + AssetBundleFolder + "/iOS";
                    break;
                }
                case AssetBundleUploaderTab.UploaderTarget.Android:
                {
                    filePath = Application.dataPath + "/../" + AssetBundleFolder + "/Android";
                    break;
                }
                case AssetBundleUploaderTab.UploaderTarget.Windows:
                {
                    filePath = Application.dataPath + "/../" + AssetBundleFolder + "/StandaloneWindows";
                    break;
                }
                default:
                {
                    Debug.Assert(false, "Invalid target");
                    break;
                }
            }
            return filePath;
        }

        // get: upload asset list
        private List<string> GetUploadFileList()
        {
            List<string> retList = new List<string>();

            switch (SelectTarget)
            {
                case AssetBundleUploaderTab.UploaderTarget.iOS:
                case AssetBundleUploaderTab.UploaderTarget.Android:
                case AssetBundleUploaderTab.UploaderTarget.Windows:
                {
                    string path = GetBuildAssetFolderPath(SelectTarget) + "/build";
                    var list = GetUploadFileList(path);
                    retList.AddRange(list);
                    break;
                }
                case AssetBundleUploaderTab.UploaderTarget.All:
                default:
                {
                    string path = GetBuildAssetFolderPath(AssetBundleUploaderTab.UploaderTarget.iOS) + "/build";
                    var list = GetUploadFileList(path);
                    retList.AddRange(list);

                    string path2 = GetBuildAssetFolderPath(AssetBundleUploaderTab.UploaderTarget.Android) + "/build";
                    var list2 = GetUploadFileList(path2);
                    retList.AddRange(list2);

                    string path3 = GetBuildAssetFolderPath(AssetBundleUploaderTab.UploaderTarget.Windows) + "/build";
                    var list3 = GetUploadFileList(path3);
                    retList.AddRange(list3);
                    break;
                }
            }

            return retList;
        }

        private List<string> GetUploadFileList(string rootPath)
        {
            List<string> fileList = new List<string>();
            FileUtil.GetAllFiles(rootPath, ref fileList, "*");
            
            List<string> retList = new List<string>();
            foreach (var o in fileList) {
                if (o.IndexOf(".manifest") >= 0 || 
                    o.IndexOf(".json") >= 0 ||
                    o.IndexOf(".meta") >= 0) {
                    continue;
                }

                string filePath = o.Replace("\\", "/");
                retList.Add(filePath);
            }

            return retList;
        }

        public List<string> GetUploadFileList(AssetBundleUploaderTab.UploaderTarget targetPlatform)
        {
            string path = GetBuildAssetFolderPath(targetPlatform) + "/build";

            return GetUploadFileList(path);            
        }

        // Create: version file
        public string CreateAssetVersionFile(AssetBundleUploaderTab.UploaderTarget targetPlatform)
        {
            AssetVersion assetVersion = new AssetVersion();
            assetVersion.CreatedTime = DateTime.Now.ToString();

            var fileList = GetUploadFileList(targetPlatform);

            bool isEnd = false;
            while (true)
            {
                int index = 0;
                for (int i = 0; i < 5; i++) {

                    int j = index * 5 + i;
                    if (j >= fileList.Count) {
                        isEnd = true;
                        break;
                    }
                    
                    AssetData assetData = new AssetData();
                    string filePath = fileList[j];
                    string[] splitStr = { "build/" };
                    string[] pathList = filePath.Split(splitStr, StringSplitOptions.None);
                    assetData.Path = "build/" + pathList[pathList.Length - 1];
                    
                    Hash128 hash = new Hash128();
                    BuildPipeline.GetHashForAssetBundle(filePath, out hash);
                    assetData.Hash = hash.ToString();
                    assetVersion.Items.Add(assetData);
                }

                GC.Collect();
                if (isEnd) {
                    break;
                } else {
                    index++;
                }
            }

            string versionFile = JsonUtility.ToJson(assetVersion, true);
 
            // save version file
            string rootDir = GetBuildAssetFolderPath(targetPlatform);
            if (!FileUtil.IsExistDirectory(rootDir)) {
                FileUtil.CreateDirectory(rootDir);
            }
            
            string savePath = rootDir + "/" + VersionFileName;
            FileUtil.WriteText(versionFile, savePath);

            return versionFile;
        }
    }
}
