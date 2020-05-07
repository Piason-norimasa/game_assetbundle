
using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;


namespace Utility
{

    public static class FileUtil
    {
        
        private static string _rootCacheDataPath = null;
        
        // Get: cache path 
        public static string GetCachePath(string rootDirName, string subRelativePath)
        {
            string retCachePath = "";
            string cacheRoot = GetCacheRootPath(Application.platform);
            
            if (Application.isEditor) {
                retCachePath = cacheRoot + "/../AppCache/" + rootDirName;
                if (!string.IsNullOrEmpty(subRelativePath)) {
                    retCachePath += "/" + subRelativePath;
                }
            } else {
                retCachePath = cacheRoot + "/" + rootDirName;
                if (!string.IsNullOrEmpty(subRelativePath)) {
                    retCachePath += "/" + subRelativePath;
                }
            }
            return retCachePath;
        }
        
        private static string GetCacheRootPath(RuntimePlatform platform)
        {
            if (Application.isEditor) {

                _rootCacheDataPath = Application.dataPath;
            } else if (_rootCacheDataPath == null) {

                switch (platform)
                {
                    case RuntimePlatform.Android: _rootCacheDataPath = Application.persistentDataPath; break;
                    case RuntimePlatform.IPhonePlayer: _rootCacheDataPath = Application.temporaryCachePath; break;
                    default:_rootCacheDataPath = Application.dataPath;
                    break;
                }
            }
            return _rootCacheDataPath;
        }

        // Read: binary data
        public static byte[] Read(string path)
        {
            if (!File.Exists(path)) {
                return null;
            }
            
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            BinaryReader reader = new BinaryReader(fileStream);
            reader.BaseStream.Position = 0;
            byte[] data = reader.ReadBytes((int)reader.BaseStream.Length);

            fileStream.Close();
            reader.Close();

            return data;
        }

        // Read: text data
        public static string ReadText(string path)
        {
            if (!File.Exists(path)) {
                return string.Empty;
            }
            
            Encoding utf8 = Encoding.GetEncoding("utf-8");
            StreamReader reader = new StreamReader(path, utf8);
            string text = reader.ReadToEnd();
            reader.Close();

            return text;
        }

        // Write: binary data
        public static bool Write(byte[] data, string path)
        {
            if (data == null) {
                return false;
            }

            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryWriter writer = new BinaryWriter(fileStream);

            bool ret = true;
            try {
                writer.Write(data);
            } catch (Exception) {
                ret = false;
            } finally {
                fileStream.Close();
                writer.Close();
            }

            return ret;
        }

        // Write: text data
        public static bool WriteText(string text, string path)
        {
            Encoding utf8 = Encoding.GetEncoding("utf-8");
            File.WriteAllText(path, text, utf8);

            return true;
        }
        
        public static bool IsExistFile(string path)
        {
            return File.Exists(path);
        }

        public static bool IsExistDirectory(string dir)
        {
            return Directory.Exists(dir);
        }

        // Create: directory
        public static void CreateDirectory(string path)
        {
            string [] pathList = path.Split('/');

            // create: root directory
            string tmpDir = pathList[0];
            Directory.CreateDirectory(tmpDir);
            if (pathList.Length <= 1) {
                return;
            }

            // reate: sub directory
            for (int i = 1; i < pathList.Length; i++) {
                tmpDir += "/" + pathList[i];
                Directory.CreateDirectory(tmpDir);
            }
        }

        // Delete: directory
        public static void DeleteDirectory(string dir)
        {
            if (IsExistDirectory(dir)) {
                // ※ディレクトリ内のファイル、サブディレクトリも消えるので使用時は要注意
                Directory.Delete(dir, true);
            }
        }
        
        // Delete: file
        public static void DeleteFile(string file_path)
        {
            if (IsExistFile(file_path)) {
                File.Delete(file_path);
            }
        }
        
        // Get: all files in directory
        public static void GetAllFiles(string dir, ref List<string> fileList, string pattern ="*")
        {
            if (!IsExistDirectory(dir)) {
                return;
            }
            
            string[] files = Directory.GetFiles(dir);
            if (files != null) {
                for (int i = 0; i < files.Length; i++) {
                    fileList.Add(files[i]);
                }
            }

            // search: in sub directories
            string[] sub_dirs = Directory.GetDirectories(dir);
            if (sub_dirs != null) {
                for (int i = 0; i < sub_dirs.Length; i++) {
                    GetAllFiles(sub_dirs[i], ref fileList, pattern);
                }
            }
        }

        // Convert: snake path to camel path
        public static string ConvertToCamelPath(string path)
        {
            string[] pathList = path.Split('/');
            if (pathList.Length <= 1) {
                return path;
            }
            
            string ret = "";
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            for (int i = 0; i < pathList.Length - 1; i++) {

                if (ret != "") {
                    ret += "/";
                }

                string tmpDir = pathList[i];
                string[] tmpDirList = tmpDir.Split('_');
                if (tmpDirList.Length > 1) {
                    for (int j = 0; j < tmpDirList.Length; j++) {
                        ret += textInfo.ToTitleCase(tmpDirList[j]);
                    }
                } else {
                    ret += tmpDirList[0];
                }
            }
            ret += "/" + pathList[pathList.Length - 1];

            return ret;
        }
    }
}
