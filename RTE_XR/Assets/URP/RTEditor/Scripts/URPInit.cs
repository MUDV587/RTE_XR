using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor.URP
{
    public class URPInit : EditorExtension
    {
        [SerializeField]
        private GameObject m_prefab = null;

        private GameObject m_instance;
        
        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            m_instance = Instantiate(m_prefab, transform, false);
            m_instance.name = m_prefab.name;

            //IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            //Canvas backgroundCanvas = appearance.UIBackgroundScaler.GetComponent<Canvas>();
            //Canvas foregroundCanvas = appearance.UIForegroundScaler.GetComponent<Canvas>();

            //if(backgroundCanvas.worldCamera != null && foregroundCanvas.worldCamera != null)
            //{
            //    ICameraUtility cameraUtility = IOC.Resolve<ICameraUtility>();
            //    cameraUtility.Stack(backgroundCanvas.worldCamera, foregroundCanvas.worldCamera);
            //}
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            Destroy(m_instance);   
        }
    }
}

