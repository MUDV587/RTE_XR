using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Battlehub.XRInteractionToolkit
{
    public class ILineRenderableProxy : MonoBehaviour, ILineRenderable
    {
        private ILineRenderable m_renderable;

        [SerializeField]
        private GameObject m_lineRenderable;

        private void Awake()
        {
            m_renderable = m_lineRenderable.GetComponent<ILineRenderable>();
        }

        public bool GetLinePoints(ref Vector3[] linePoints, ref int noPoints)
        {
            return m_renderable.GetLinePoints(ref linePoints, ref noPoints);
        }

        public bool TryGetHitInfo(ref Vector3 position, ref Vector3 normal, ref int positionInLine, ref bool isValidTarget)
        {
            return m_renderable.TryGetHitInfo(ref position, ref normal, ref positionInLine, ref isValidTarget);
        }
    }

}

