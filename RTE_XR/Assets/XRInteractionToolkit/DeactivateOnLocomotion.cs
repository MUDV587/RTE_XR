using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Battlehub.XRInteractionToolkit
{
    public class DeactivateOnLocomotion : MonoBehaviour
    {
        [SerializeField]
        private LocomotionProvider[] m_providers = null;

        [SerializeField]
        private GameObject[] m_targets = null;

        private bool[] m_activeSelf = null;

        private void Awake()
        {
            foreach(LocomotionProvider provider in m_providers)
            {
                provider.startLocomotion += OnStartLocomotion;
                provider.endLocomotion += OnEndLocomotion;
            }

            m_activeSelf = new bool[m_targets.Length];
        }

        private void OnDestroy()
        {
            foreach (LocomotionProvider provider in m_providers)
            {
                provider.startLocomotion -= OnStartLocomotion;
                provider.endLocomotion -= OnEndLocomotion;
            }
        }

        private void OnStartLocomotion(LocomotionSystem obj)
        {
            Debug.Log("StartLocomotion");
            for(int i = 0; i < m_targets.Length; ++i)
            {
                GameObject target = m_targets[i];
                m_activeSelf[i] = target.activeSelf;
                target.SetActive(false);
            }
        }

        private void OnEndLocomotion(LocomotionSystem obj)
        {
            Debug.Log("EndLocomotion");
            for (int i = 0; i < m_targets.Length; ++i)
            {
                GameObject target = m_targets[i];
                target.SetActive(m_activeSelf[i]);
            }
        }
    }

}
