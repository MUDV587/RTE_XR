using UnityEngine;

namespace Battlehub.RTCommon
{
    public class SpriteGizmo : RTEComponent
    {
        public Material Material;
        private static Mesh m_quadMesh;
        private Vector3 m_position;
        private Quaternion m_rotation;

        [SerializeField, HideInInspector]
        private SphereCollider m_collider;
        private SphereCollider m_destroyedCollider;
        private IRuntimeGraphicsLayer m_graphicsLayer;
        [SerializeField]
        private float m_scale = 1.0f;
        public float Scale
        {
            get { return m_scale; }
            set
            {
                if(m_scale != value)
                {
                    m_scale = value;
                    UpdateCollider();
                    Refresh();
                }
            }
        }
        protected override void AwakeOverride()
        {
            if(m_quadMesh == null)
            {
                m_quadMesh = RuntimeGraphics.CreateQuadMesh();
            }

            base.AwakeOverride();
            m_graphicsLayer = Window.IOCContainer.Resolve<IRuntimeGraphicsLayer>();
        }

        private void Update()
        {
            if(m_position != transform.position || m_rotation != transform.rotation)
            {
                m_position = transform.position;
                m_rotation = transform.rotation;
                Refresh();
            }
        }

        private void Refresh()
        {
            m_graphicsLayer.RemoveMesh(m_quadMesh);
            m_graphicsLayer.AddMesh(m_quadMesh, Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * m_scale), Material);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            m_graphicsLayer.RemoveMesh(m_quadMesh);
        }

        private void OnEnable()
        {
            m_collider = GetComponent<SphereCollider>();

            if (m_collider == null || m_collider == m_destroyedCollider)
            {
                m_collider = gameObject.AddComponent<SphereCollider>();
            }
            if (m_collider != null)
            {
                if (m_collider.hideFlags == HideFlags.None)
                {
                    m_collider.hideFlags = HideFlags.HideInInspector;
                }

                UpdateCollider();
            }
        }

        private void OnDisable()
        {
            if(m_collider != null)
            {
                Destroy(m_collider);
                m_destroyedCollider = m_collider;
                m_collider = null;
            }
        }

        private void UpdateCollider()
        {
            if(m_collider != null)
            {
                m_collider.radius = 0.25f * m_scale;
            }
        }

      
    }
}

