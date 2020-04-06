using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Battlehub.RTCommon.URP
{
    public class CameraUtilityURP : MonoBehaviour //ICameraUtility
    {
        private void Awake()
        {
           //IOC.RegisterFallback<ICameraUtility>(this);
        }

        private void OnDestroy()
        {
           // IOC.UnregisterFallback<ICameraUtility>(this);
        }

        public void Stack(Camera baseCamera, Camera overlayCamera)
        {
            UniversalAdditionalCameraData overlayData = overlayCamera.GetComponent<UniversalAdditionalCameraData>();
            if(overlayData == null)
            {
                overlayData = overlayCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            overlayData.renderType = CameraRenderType.Overlay;
            UniversalAdditionalCameraData baseData = baseCamera.GetComponent<UniversalAdditionalCameraData>();
            if(baseData == null)
            {
                baseData = baseCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            baseData.cameraStack.Add(overlayCamera);
            overlayCamera.clearFlags = CameraClearFlags.Depth;
        }
    }

}

