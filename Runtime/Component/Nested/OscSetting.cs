using extOSC;
using extOSC.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UniRx;
using UnityEngine;

namespace Somekasu.DollyDoll
{
    [Serializable]
    public class Osc : IDisposable
    {
        [SerializeField]
        internal int VrcPort = 9000;
        [SerializeField]
        internal int UnityPort = 9001;
        [SerializeField]
        internal string VrcHost = "127.0.0.1";

        internal readonly BoolReactiveProperty ImportOnSave = new(false);
        internal readonly BoolReactiveProperty ForceImport = new(false);
        internal readonly BoolReactiveProperty LoadOnExport = new(false);
        internal readonly BoolReactiveProperty IsActive = new(false);

        private DollyDoll _dollyDoll;
        private OSCTransmitter _oscTransmitter;
        private OSCReceiver _oscReceiver;
        private readonly List<IOSCBind> _oscBinds = new();

        internal void Initialize(DollyDoll dollyDoll)
        {
            _dollyDoll = dollyDoll;
            this.AddTo(dollyDoll);

            // 起動時に万が一残っていた場合は消す
            if (_oscTransmitter != null)
            {
                UnityEngine.Object.DestroyImmediate(_oscTransmitter);
                _oscTransmitter = null;
            }
            if (_oscReceiver != null)
            {
                UnityEngine.Object.DestroyImmediate(_oscReceiver);
                _oscReceiver = null;
            }

            _oscBinds.Add(new OSCBind("/dolly/ExportLocal", message =>
            {
                if (message.ToString(out string filePath))
                {
                    // 非ascii文字を含むパスが文字化けするため置換
                    string myDocument = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    string cameraPathDir = Path.Combine(myDocument, "VRChat", "CameraPaths");
                    filePath = Regex.Replace(filePath, @".*CameraPaths", cameraPathDir);
                    _dollyDoll.JSONPath = filePath;
                    _dollyDoll.Reload();
                }
            }));

            // 上書き時インポートに関する設定の更新イベント
            IsActive.Merge(ImportOnSave)
                .Subscribe(_ =>
                {
                    if (IsActive.Value && ImportOnSave.Value)
                    {
                        if (_oscTransmitter == null)
                        {
                            _oscTransmitter = _dollyDoll.gameObject.AddComponent<OSCTransmitter>();
                            _oscTransmitter.RemoteHost = VrcHost;
                            _oscTransmitter.RemotePort = VrcPort;
                            _oscTransmitter.Connect();
                        }
                    }
                    else if (!IsActive.Value || !ImportOnSave.Value)
                    {
                        if (_oscTransmitter != null)
                        {
                            UnityEngine.Object.DestroyImmediate(_oscTransmitter);
                            _oscTransmitter = null;
                        }
                    }
                })
                .AddTo(_dollyDoll);

            // エクスポート時読込に関する設定の更新イベント
            IsActive.Merge(LoadOnExport)
                .Subscribe(_ =>
                {
                    if (IsActive.Value && LoadOnExport.Value)
                    {
                        if (_oscReceiver == null)
                        {
                            _oscReceiver = _dollyDoll.gameObject.AddComponent<OSCReceiver>();
                            _oscReceiver.LocalPort = UnityPort;
                            _oscBinds.ForEach(bind => _oscReceiver.Bind(bind));
                            _oscReceiver.Connect();
                        }
                    }
                    else if (!IsActive.Value || !LoadOnExport.Value)
                    {
                        if (_oscReceiver != null)
                        {
                            UnityEngine.Object.DestroyImmediate(_oscReceiver);
                            _oscReceiver = null;
                        }
                    }
                })
                .AddTo(_dollyDoll);
        }

        internal void Bind(string path, UnityEngine.Events.UnityAction<extOSC.OSCMessage> callback)
        {
            var bind = new OSCBind(path, callback);
            _oscBinds.Add(bind);
            if (_oscReceiver != null)
            {
                _oscReceiver.Bind(bind);
            }
        }

        internal void SendImportRequest()
        {
            if (_oscTransmitter != null)
            {
                if (ForceImport.Value)
                {
                    var msg = new OSCMessage("/dolly/Play");
                    msg.AddValue(OSCValue.Bool(false));
                    _oscTransmitter.Send(msg);
                }
                var message = new OSCMessage("/dolly/Import");
                string jsonPath = _dollyDoll.JSONPath;
                if (Regex.IsMatch(jsonPath, @"[^\u0000-\u007F]"))
                {
                    if (Directory.Exists("myDocuments"))
                    {
                        string myDocument = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                        string relativePath = Path.GetRelativePath(myDocument, jsonPath);
                        jsonPath = Path.GetFullPath(Path.Combine("myDocuments", relativePath));
                    }
                    else
                    {
                        MyLog.LogError(I18n.G("osc/error/nonAscii"));
                    }
                }
                message.AddValue(OSCValue.String(jsonPath));
                _oscTransmitter.Send(message);
            }
        }

        public void Dispose()
        {
            if (_oscTransmitter != null)
            {
                _oscTransmitter.Close();
                _oscTransmitter = null;
            }
            if (_oscReceiver != null)
            {
                _oscReceiver.Close();
                _oscReceiver = null;
            }
            _oscBinds.Clear();
        }
    }
}
