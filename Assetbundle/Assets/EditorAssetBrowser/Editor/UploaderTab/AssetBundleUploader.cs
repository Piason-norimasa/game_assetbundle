
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace AssetBundleBrowser
{

    class AssetBundleUploader
    {
        public bool IsInUpload { get; private set; } = false;

        private string _baseUrl = "";
        private List<string> _uploadAssetFileList;
        private List<UploadToServerTask> _uploaderTaskList = new List<UploadToServerTask>();
        private AssetBundleUploaderTab.UploaderTarget _targetPlatform = AssetBundleUploaderTab.UploaderTarget.All;

        internal void OnUpdate()
        {
            if (!IsInUpload) {
                return;
            }

            var count = _uploaderTaskList.Where(o => o.IsExecute == true).Count();
            if (count > 0) {
                return;
            }

            int taskCount = CreateUploadTask();
            if (taskCount == 0) {
                IsInUpload = false;
                Debug.Log("Finish upload assetbundles");
            }
        }

        public void Execute(string baseUrl, AssetBundleUploaderTab.UploaderTarget targetPlatform, string versionFile, List<string> uploadFileList)
        {
            IsInUpload = true;
            _uploadAssetFileList = new List<string>(uploadFileList);
            _targetPlatform = targetPlatform;

            // upload: version file
            _baseUrl = baseUrl;
            var task = UploadToServerTask.UploadVersionFile(targetPlatform, baseUrl, versionFile);
            _uploaderTaskList.Add(task);
            task.Execute();            
        }

        private int CreateUploadTask()
        {
            int count = 0;
            for (int i = 0; i < 5 && i < _uploadAssetFileList.Count; i++) {

                var file = _uploadAssetFileList.PopFirst();

                // upload: assetbundle
                var task = UploadToServerTask.UploadAssetFile(_targetPlatform, _baseUrl, file);
                _uploaderTaskList.Add(task);
                count++;
                task.Execute();
            }

            return count;
        }
    }

    class UploadToServerTask
    {
        public bool IsExecute { get; private set; } = true;

        private string _url = "";
        private string _file = "";
        private string _filePath = "";
        private bool _isVersionFile = false;

        public static UploadToServerTask UploadVersionFile(AssetBundleUploaderTab.UploaderTarget targetPlatform, string url, string file)
        {
            var task = new UploadToServerTask();
            task._filePath = "";
            task._file = file;
            task._url = task.GetAssetVersionFileUrl(url, targetPlatform);
            task._isVersionFile = true;

            return task;
        }

        public static UploadToServerTask UploadAssetFile(AssetBundleUploaderTab.UploaderTarget targetPlatform, string url, string filePath)
        {
            var task = new UploadToServerTask();
            task._filePath = filePath;
            task._file = task.ConvertToUploadingFilePath(filePath, targetPlatform);
            task._url = url + "/" + task._file;
            task._isVersionFile = false;

            return task;
        }

        public void Execute()
        {
            if (_isVersionFile) {
                IEnumerator it = UploadVersionFile();
                while (it.MoveNext()) { }
            } else {
                IEnumerator it = UploadAssetFile();
                while (it.MoveNext()) { }
            }

            IsExecute = false;
        }

        private string GetAssetVersionFileUrl(string baseUrl, AssetBundleUploaderTab.UploaderTarget targetPlatform)
        {
            string url = baseUrl;
            switch (targetPlatform) {
                case AssetBundleUploaderTab.UploaderTarget.Android: url += "/Android/"; break;
                case AssetBundleUploaderTab.UploaderTarget.iOS: url += "/iOS/"; break;
                case AssetBundleUploaderTab.UploaderTarget.Windows: url += "/StandaloneWindows/"; break;
                default: break;
            }
            url += AssetBundleList.VersionFileName;

            return url;
        }

        private string ConvertToUploadingFilePath(string filePath, AssetBundleUploaderTab.UploaderTarget targetPlatform)
        {
            string[] splitStr = { "build/" };
            string[] pathList = filePath.Split(splitStr, StringSplitOptions.None);

            string retPath = pathList[pathList.Length - 1];
            switch (targetPlatform) {
                case AssetBundleUploaderTab.UploaderTarget.Android: retPath = "Android/build/" + retPath; break;
                case AssetBundleUploaderTab.UploaderTarget.iOS: retPath = "iOS/build/" + retPath; break;
                case AssetBundleUploaderTab.UploaderTarget.Windows: retPath += "StandaloneWindows/build/" + retPath; break;
                default: break;
            }

            return retPath;
        }

        private IEnumerator UploadVersionFile()
        {
            FtpWebRequest request = FtpWebRequest.Create(_url) as FtpWebRequest;
            request.Credentials = new NetworkCredential(AssetBundleUploaderTab.FtpUserName, AssetBundleUploaderTab.FtpUserPassword);
            request.EnableSsl = true;
            request.Method = WebRequestMethods.Ftp.UploadFile;

            byte[] binFile = System.Text.Encoding.UTF8.GetBytes(_file);
            request.ContentLength = binFile.Length;

            Stream requestStream = null;
            FtpWebResponse ftpResponse = null;
            try {

                requestStream = request.GetRequestStream();
                requestStream.Write(binFile, 0, binFile.Length);
                ftpResponse = (FtpWebResponse)request.GetResponse();
                Debug.Log("Success Upload: version file  \nURL:" + _url);
            } catch (Exception e) {

                Debug.LogError("Error uploading file: " + e.Message + "\nURL:" + _url);
            } finally {

                if (requestStream != null) {
                    requestStream.Close();
                }
                if (ftpResponse != null) {
                    ftpResponse.Close();
                }
            }

            yield break;
        }

        private IEnumerator UploadAssetFile()
        {
            byte [] data = Script.Utility.FileUtil.Read(_filePath);

            FtpWebRequest request = FtpWebRequest.Create(_url) as FtpWebRequest;
            request.Credentials = new NetworkCredential(AssetBundleUploaderTab.FtpUserName, AssetBundleUploaderTab.FtpUserPassword);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.EnableSsl = true;
            request.ContentLength = data.Length;

            Stream requestStream = null;
            FtpWebResponse ftpResponse = null;
            try {

                requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                ftpResponse = (FtpWebResponse)request.GetResponse();
                Debug.Log("Success Upload: version file  \nURL:" + _url);
            } catch (Exception e) {

                Debug.LogError("Error uploading file: " + e.Message + "\nURL:" + _url);
            } finally {

                if (requestStream != null) {
                    requestStream.Close();
                }
                if (ftpResponse != null) {
                    ftpResponse.Close();
                }
            }

            yield break;
        }
    }
}
