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
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            Destroy(m_instance);   
        }
    }
}

