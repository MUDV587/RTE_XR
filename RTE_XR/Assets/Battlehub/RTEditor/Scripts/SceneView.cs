using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class SceneView : RuntimeWindow
    {
        protected override void AwakeOverride()
        {
            ActivateOnAnyKey = true;
            WindowType = RuntimeWindowType.Scene;
            base.AwakeOverride();   
        }

        protected virtual void Start()
        {
            if (!GetComponent<SceneViewInput>())
            {
                gameObject.AddComponent<SceneViewInput>();
            }

            if (!GetComponent<SceneViewImpl>())
            {
                gameObject.AddComponent<SceneViewImpl>();
            }
        }

        protected override void SetCullingMask(Camera camera)
        {
            CameraLayerSettings settings = Editor.CameraLayerSettings;
            camera.cullingMask &= (settings.RaycastMask | 1 << settings.AllScenesLayer);
        }

        protected override void ResetCullingMask(Camera camera)
        {
            CameraLayerSettings settings = Editor.CameraLayerSettings;
            camera.cullingMask |= ~(settings.RaycastMask | 1 << settings.AllScenesLayer);
        }
    }
}
