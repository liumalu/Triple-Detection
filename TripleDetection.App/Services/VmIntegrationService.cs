using System;
using VM.Core;
using VM.PlatformSDKCS;
using System.Drawing;
using TripleDetection.Models;

namespace TripleDetection.Services
{
    public class VmIntegrationService
    {
        private VmSolution _vmSolution;
        private VmProcedure _procedure;
        private ImageStorageService _imageStorage;
        private bool _isSolutionLoad = false;

        public event EventHandler<DetectionResult> OnDetectionResult;

        public VmIntegrationService(ImageStorageService imageStorage)
        {
            _imageStorage = imageStorage;
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
            _procedure?.Run();
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

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            if (workStatusInfo.nWorkStatus == 1 && workStatusInfo.nProcessID == 10000)
            {
                try
                {
                    var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                    if (ioNameInfos.Count > 0 && ioNameInfos[0].TypeName == IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                    {
                        string strResult = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name).astStringVal[0].strValue;
                        var result = ParseResult(strResult);

                        // TODO: 获取图片并保存
                        // var image = GetCurrentImage();
                        // result.ImagePath = _imageStorage.SaveImage(image, result.IsOK);

                        OnDetectionResult?.Invoke(this, result);
                    }
                }
                catch (VmException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VM Error: 0x{ex.errorCode:X}");
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