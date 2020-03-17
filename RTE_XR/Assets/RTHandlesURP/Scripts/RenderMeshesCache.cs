using Battlehub.RTCommon;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTHandles.URP
{
    public class RenderMeshesBatch
    {
        public readonly Mesh Mesh;
        public readonly List<Transform> Transforms = new List<Transform>();

        private Matrix4x4[] m_matrices = new Matrix4x4[0];
        public Matrix4x4[] Matrices
        {
            get { return m_matrices; }
        }

        public RenderMeshesBatch(Mesh mesh)
        {
            Mesh = mesh;
        }

        public void Refresh()
        {
            for (int i = Transforms.Count - 1; i >= 0; --i)
            {
                if (Transforms[i] == null)
                {
                    Transforms.RemoveAt(i);
                }
            }

            if(Transforms.Count != m_matrices.Length)
            {
                m_matrices = new Matrix4x4[Transforms.Count];
            }

            for (int i = Transforms.Count - 1; i >= 0; --i)
            {
                m_matrices[i] = Transforms[i].localToWorldMatrix;
            }
        }
    }

    public interface IRenderMeshesCache
    {
        Material Material
        {
            get;
        }

        RenderMeshesBatch[] Batches
        {
            get;
        }
        void Add(Mesh mesh, Transform transform);
        void Remove(Mesh mesh, Transform transform);
        void Refresh(int maxBatchSize = 128);
        void Clear();
    }

    public class RenderMeshesCache : MonoBehaviour, IRenderMeshesCache
    {
        private RenderMeshesBatch[] m_batches = new RenderMeshesBatch[0];
        public RenderMeshesBatch[] Batches
        {
            get { return m_batches; }
        }

        [SerializeField]
        private Material m_material = null;
        public Material Material
        {
            get { return m_material; }
        }

        private Dictionary<Mesh, List<Transform>> m_meshToTransform = new Dictionary<Mesh, List<Transform>>();
        private void Awake()
        {
            IOC.RegisterFallback<IRenderMeshesCache>(this);
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<IRenderMeshesCache>(this);
        }

        private void Update()
        {
            for(int i = 0; i < m_batches.Length; ++i)
            {
                m_batches[i].Refresh();
            }
        }

        public void Add(Mesh mesh, Transform transform)
        {
            List<Transform> transforms;
            if (!m_meshToTransform.TryGetValue(mesh, out transforms))
            {
                transforms = new List<Transform>();
                m_meshToTransform.Add(mesh, transforms);
            }
            transforms.Add(transform);
        }

        public void Remove(Mesh mesh, Transform transform)
        {
            List<Transform> transforms;
            if (m_meshToTransform.TryGetValue(mesh, out transforms))
            {
                transforms.Remove(transform);
                if (transforms.Count == 0)
                {
                    m_meshToTransform.Remove(mesh);
                }
            }
        }

        public void Clear()
        {
            m_meshToTransform.Clear();
        }

        public void Refresh(int maxBatchSize = 128)
        {
            List<RenderMeshesBatch> batches = new List<RenderMeshesBatch>();
            foreach (KeyValuePair<Mesh, List<Transform>> kvp in m_meshToTransform)
            {
                if(kvp.Key == null)
                {
                    continue;
                }

                RenderMeshesBatch batch = new RenderMeshesBatch(kvp.Key);
                batches.Add(batch);

                List<Transform> transforms = kvp.Value;
                int index = 0;
                for (int i = 0; i < transforms.Count; ++i)
                {
                    if (index == maxBatchSize)
                    {
                        batch = new RenderMeshesBatch(kvp.Key);
                        batches.Add(batch);
                        index = 0;
                    }

                    batch.Transforms.Add(transforms[i]);
                    index++;
                }
            }

            m_batches = batches.ToArray();
        }
    }
}
