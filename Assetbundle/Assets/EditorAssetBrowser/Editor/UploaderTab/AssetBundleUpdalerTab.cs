
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using AssetBundleBrowser.AssetBundleDataSource;
using Script.Utility;

namespace AssetBundleBrowser
{

    [System.Serializable]
    internal class AssetBundleUploaderTab
    {
        internal enum UploaderTarget
        {
            All = 0,

            Windows,
            Android,
            iOS,
        }

        internal enum ServerType
        {
            DevelopmentServer = 0,

            StagingServer,
            ProductonServer,
        }

        class ResourceServer
        {
            public string ServerName { get; set; } = "";
            public string Url { get; set; } = "";

            public ResourceServer(string name, string url)
            {
                ServerName = name;
                Url = url;
            }
        }

        private ResourceServer [] _resourceServerList = new ResourceServer []
        {
            new ResourceServer("Development Server", "ftp://static-zk.igg.com/AssetBundles"),
            new ResourceServer("Staging Server",     "ftp://static-zk.igg.com/AssetBundles"),
            new ResourceServer("Production Server",  "ftp://static-zk.igg.com/AssetBundles"),
        };

        public static string FtpUserName = "user";
        public static string FtpUserPassword = "password";

        private Vector2 _scrollPosition;
        private Vector2 _listBoxPos = Vector2.zero;
        private AssetBundleList _currentUploaderData = new AssetBundleList();
        private UploaderTarget _currentUploaderTarget = UploaderTarget.Android;
        private Dictionary<UploaderTarget, AssetBundleUploader> _uploaderList = new Dictionary<UploaderTarget, AssetBundleUploader>();
        private ServerType _serverType = ServerType.DevelopmentServer;

        internal AssetBundleUploaderTab()
        {

        }

        internal void OnEnable(EditorWindow parent)
        {
            _currentUploaderData.Refresh(_currentUploaderTarget);
            _uploaderList.Add(UploaderTarget.Windows, new AssetBundleUploader());
            _uploaderList.Add(UploaderTarget.Android, new AssetBundleUploader());
            _uploaderList.Add(UploaderTarget.iOS, new AssetBundleUploader());

            // check: certification setting
            System.Net.ServicePointManager.ServerCertificateValidationCallback += AcceptAllCertificatePolicy;
        }

        public static bool AcceptAllCertificatePolicy(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
		}

        internal void OnDisable()
        {

        }

        internal void OnUpdate()
        {
            _uploaderList[UploaderTarget.Windows].OnUpdate();
            _uploaderList[UploaderTarget.Android].OnUpdate();
            _uploaderList[UploaderTarget.iOS].OnUpdate();
        }

        private bool IsUploaderPlaying()
        {
            if (_uploaderList[UploaderTarget.Windows].IsInUpload ||
                _uploaderList[UploaderTarget.Android].IsInUpload ||
                _uploaderList[UploaderTarget.iOS].IsInUpload) {
                return true;

            }
            return false;
        }

        public string ExecuteShell(string args)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            string filePath = Application.dataPath + "/../AssetBundles/upload.sh";
            p.StartInfo.FileName = filePath;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = args;
            p.Start();

            string results = p.StandardOutput.ReadToEnd();
            Debug.Log(results);
            p.WaitForExit();
            p.Close();

            return results;
        }

        internal void OnGUI()
        {
            if (IsUploaderPlaying()) {

                GUILayout.BeginArea(new Rect(0, 0, 800, 400));
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Now is Uploading..................");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(50.0f);

            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            centeredStyle.alignment = TextAnchor.UpperCenter;

            // Uploader list
            var listStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            GUILayout.Label(new GUIContent("Uploader List"), centeredStyle);
            EditorGUILayout.BeginVertical(GUILayout.Height(200.0f));
            _listBoxPos = EditorGUILayout.BeginScrollView(_listBoxPos, GUI.skin.box);
            _currentUploaderData.CurrentUploadList.ForEach(o => {
                EditorGUILayout.LabelField(o, listStyle);
           });
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // uploade: platform target
            GUILayout.Space(10.0f);
            GUILayout.Label("select platform");
            UploaderTarget target = (UploaderTarget)EditorGUILayout.EnumPopup(_currentUploaderTarget);
            if (target != _currentUploaderTarget) {
                _currentUploaderTarget = target;
                _currentUploaderData.Refresh(target);
            }

            List<string> serverDisplayList = new List<string>();
            foreach (var o in _resourceServerList) {
                string str = o.ServerName + "   " + WWW.EscapeURL(o.Url);
                serverDisplayList.Add(str);
            }

            // uploade: target server
            GUILayout.Space(10.0f);
            GUILayout.Label("upload server");
            _serverType = (ServerType)EditorGUILayout.Popup((int)_serverType, serverDisplayList.ToArray());

            GUILayout.Space(60.0f);
            GUILayout.Label("FTP Account");
            FtpUserName = GUILayout.TextField(FtpUserName);
            FtpUserPassword = GUILayout.TextField(FtpUserPassword);

            GUILayout.Space(10.0f);
            if (!string.IsNullOrEmpty(FtpUserName) && !string.IsNullOrEmpty(FtpUserPassword)) {
                if (GUILayout.Button("Uploade AssetBundles", GUILayout.MaxWidth(300.0f))) {
                    UploadProcess();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UploadProcess()
        {
            // second: upload to server
            switch (_currentUploaderTarget) {
                case UploaderTarget.Android:
                case UploaderTarget.iOS:
                case UploaderTarget.Windows: {

                    // first: create asset version list
                    var versionFile = _currentUploaderData.CreateAssetVersionFile(_currentUploaderTarget);

                    _uploaderList[_currentUploaderTarget].Execute(
                        _resourceServerList[(int)_serverType].Url,
                        _currentUploaderTarget,
                        versionFile,
                        _currentUploaderData.CurrentUploadList);
                    break;
                }
                case UploaderTarget.All: {
                    
                    // windows
                    var versionFile = _currentUploaderData.CreateAssetVersionFile(UploaderTarget.Windows);
                    var uploadAssetList = _currentUploaderData.GetUploadFileList(UploaderTarget.Windows);
                    if (uploadAssetList.Count > 0) {
                        _uploaderList[UploaderTarget.Windows].Execute(
                            _resourceServerList[(int)_serverType].Url,
                            UploaderTarget.Windows,
                            versionFile,
                            uploadAssetList);
                    }

                    // Android
                    versionFile = _currentUploaderData.CreateAssetVersionFile(UploaderTarget.Android);
                    uploadAssetList = _currentUploaderData.GetUploadFileList(UploaderTarget.Android);
                    if (uploadAssetList.Count > 0) {
                        _uploaderList[UploaderTarget.Android].Execute(
                            _resourceServerList[(int)_serverType].Url,
                            UploaderTarget.Android,
                            versionFile,
                            uploadAssetList);
                    }

                    // iOS
                    versionFile = _currentUploaderData.CreateAssetVersionFile(UploaderTarget.iOS);
                    uploadAssetList = _currentUploaderData.GetUploadFileList(UploaderTarget.iOS);
                    if (uploadAssetList.Count > 0) {
                        _uploaderList[UploaderTarget.iOS].Execute(
                            _resourceServerList[(int)_serverType].Url,
                            UploaderTarget.Android,
                            versionFile,
                            uploadAssetList);
                    }

                    break;
                }
            }
        }
    }
}
