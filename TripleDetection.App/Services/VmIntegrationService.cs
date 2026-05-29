using System;
using VM.Core;
using VM.PlatformSDKCS;
using System.Drawing;
using TripleDetection.Models;
using GlobalVariableModuleCs;

namespace TripleDetection.Services
{
    public class VmIntegrationService
    {
        private VmProcedure _procedure;
        private ImageStorageService _imageStorage;
        private bool _isSolutionLoad = false;
        private LoggingService _logService;

        public event EventHandler<DetectionResult> OnDetectionResult;

        public VmIntegrationService(ImageStorageService imageStorage, LoggingService logService)
        {
            _imageStorage = imageStorage;
            _logService = logService;
            VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
        }

        public void LoadSolution(string solPath)
        {
            VmSolution.Load(solPath);
            _isSolutionLoad = true;

            ProcessInfoList processList = VmSolution.Instance.GetAllProcedureList();
            if (processList.nNum > 0)
            {
                _procedure = VmSolution.Instance[processList.astProcessInfo[0].strProcessName] as VmProcedure;
            }
        }

        public void RunOnce()
        {
            _stopwatch.Restart();
            _procedure?.Run();
        }

        public void Stop()
        {
            if (_procedure != null)
            {
                _procedure.ContinuousRunEnable = false;
            }
        }

        public bool IsContinuousRun => _procedure?.ContinuousRunEnable ?? false;

        public void SetProcedure(string procedureName)
        {
            if (_isSolutionLoad && !string.IsNullOrEmpty(procedureName))
            {
                _procedure = VmSolution.Instance[procedureName] as VmProcedure;
            }
        }

        public void SetContinuousRun(bool enable)
        {
            if (_procedure != null)
            {
                _procedure.ContinuousRunEnable = enable;
            }
        }

        public VmProcedure GetProcedure()
        {
            return _procedure;
        }

        public System.Collections.Generic.List<string> GetAllProcedureNames()
        {
            var names = new System.Collections.Generic.List<string>();
            if (_isSolutionLoad)
            {
                var processList = VmSolution.Instance.GetAllProcedureList();
                for (int i = 0; i < processList.nNum; i++)
                {
                    names.Add(processList.astProcessInfo[i].strProcessName);
                }
            }
            return names;
        }

        public void SetGlobalVariableString(string name, string value)
        {
            if (_procedure != null)
            {
                var gvTool = _procedure.Modules["GlobalVariable"] as GlobalVariableModuleTool;
                if (gvTool != null)
                {
                    string defaultValue = gvTool.GetGlobalVar(name) ?? "null";
                    gvTool.SetGlobalVar(name, value);
                    _logService?.Log($"[VM GlobalVariable] {name}: '{defaultValue}' -> '{value}'");
                }
            }
        }

        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            if (workStatusInfo.nWorkStatus == 0 && workStatusInfo.nProcessID == 10000)
            {
                _stopwatch.Stop();
                try
                {
                    if (_procedure == null)
                    {
                        System.Diagnostics.Debug.WriteLine("VmIntegrationService: _procedure is null");
                        return;
                    }

                    var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                    if (ioNameInfos.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("VmIntegrationService: no outputs available");
                        return;
                    }

                    if (ioNameInfos[0].TypeName != IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                    {
                        System.Diagnostics.Debug.WriteLine($"VmIntegrationService: type mismatch, got {ioNameInfos[0].TypeName}");
                        return;
                    }

                    var outputResult = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name);
                    var stringVal = outputResult.astStringVal;
                    if (stringVal == null || stringVal.Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("VmIntegrationService: stringVal is null or empty");
                        return;
                    }

                    string strResult = stringVal[0].strValue;
                    if (strResult != null)
                    {
                        var result = ParseResult(strResult);
                        result.ElapsedMs = _stopwatch.ElapsedMilliseconds;
                        OnDetectionResult?.Invoke(this, result);
                    }
                }
                catch (Exception ex)
                {
                    dynamic vmEx = ex;
                    if (vmEx.errorCode != null)
                        System.Diagnostics.Debug.WriteLine($"VmIntegrationService VM Error: 0x{vmEx.errorCode:X}");
                    else
                        System.Diagnostics.Debug.WriteLine($"VmIntegrationService Error: {ex.Message}");
                }
                finally
                {
                    _stopwatch.Restart();
                }
            }
        }

        private DetectionResult ParseResult(string strResult)
        {
            var parts = strResult.Split(';');
            return new DetectionResult
            {
                IsOK = parts[0] == "1",
                CharCount = int.Parse(parts[1]),
                CodeInfo = parts[2],
                Confidence = double.Parse(parts[3]),
                DetectionTime = DateTime.Now
            };
        }
    }
}