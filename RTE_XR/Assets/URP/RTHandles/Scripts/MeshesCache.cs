using Battlehub.RTCommon;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTHandles.URP
{
    public interface IMeshesCache
    {
        bool AutoRefresh
        {
            get;
            set;
        }

        bool IsEmpty
        {
            get;
        }

        IList<RenderMeshesBatch> Batches
        {
            get;
        }

        void AddBatch(Mesh mesh, Material material, Matrix4x4[] matrices);
        void RemoveBatch(Mesh mesh);

        void Add(Mesh mesh, Transform transform);
        void SetMaterial(Mesh mesh, Material material);
        void Remove(Mesh mesh, Transform transform);

        void Refresh(int maxBatchSize = 128);
        void Clear();
    }

    public class RenderMeshesBatch
    {
        public readonly Mesh Mesh;
        public readonly Material Material;
        
        protected Matrix4x4[] m_matrices;
        public Matrix4x4[] Matrices
        {
            get { return m_matrices; }
        }

        public RenderMeshesBatch(Mesh mesh, Material material, Matrix4x4[] matrices)
        {
            Mesh = mesh;
            Material = material;
            m_matrices = matrices;
        }

        public virtual void Refresh()
        {

        }
    }

    public class MeshesCache : MonoBehaviour, IMeshesCache
    {
        public class RenderTransformedMeshesBatch : RenderMeshesBatch
        {
            public readonly List<Transform> Transforms = new List<Transform>();

            public RenderTransformedMeshesBatch(Mesh mesh, Material material) : base(mesh, material, new Matrix4x4[0])
            {
            }

            public override void Refresh()
            {
                for (int i = Transforms.Count - 1; i >= 0; --i)
                {
                    if (Transforms[i] == null)
                    {
                        Transforms.RemoveAt(i);
                    }
                }

                if (Transforms.Count != m_matrices.Length)
                {
                    m_matrices = new Matrix4x4[Transforms.Count];
                }

                for (int i = Transforms.Count - 1; i >= 0; --i)
                {
                    Transform transform = Transforms[i];
                    m_matrices[i] = transform.localToWorldMatrix;
                }
            }
        }

        public bool AutoRefresh
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public bool IsEmpty
        {
            get { return m_batches.Count == 0; }
        }

        
        private readonly List<RenderMeshesBatch> m_batches = new List<RenderMeshesBatch>();
        public IList<RenderMeshesBatch> Batches
        {
            get { return m_batches; }
        }

        private Dictionary<Mesh, Tuple<Material, List<Transform>>> m_meshToData = new Dictionary<Mesh, Tuple<Material, List<Transform>>>();
        private readonly Dictionary<Mesh, RenderMeshesBatch> m_meshToBatch = new Dictionary<Mesh, RenderMeshesBatch>();
        private void Awake()
        {
            IOC.Register<IMeshesCache>(name, this);
        }

        private void OnDestroy()
        {
            IOC.Unregister<IMeshesCache>(name, this);
        }

        private void Update()
        {
            for(int i = 0; i < m_batches.Count; ++i)
            {
                m_batches[i].Refresh();
            }
        }

        public void AddBatch(Mesh mesh, Material material, Matrix4x4[] matrices)
        {
            m_meshToBatch.Add(mesh, new RenderMeshesBatch(mesh, material, matrices));
        }

        public void RemoveBatch(Mesh mesh)
        {
            m_meshToBatch.Remove(mesh);
        }
   
        public void Add(Mesh mesh, Transform transform)
        {
            Tuple<Material, List<Transform>> data;
            if (!m_meshToData.TryGetValue(mesh, out data))
            {
                data = new Tuple<Material, List<Transform>>(null, new List<Transform>());
                m_meshToData.Add(mesh, data);
            }
            data.Item2.Add(transform);
        }

        public void SetMaterial(Mesh mesh, Material material)
        {
            m_meshToData[mesh] = new Tuple<Material, List<Transform>>(material, m_meshToData[mesh].Item2);
        }

        public void Remove(Mesh mesh, Transform transform)
        {
            Tuple<Material, List<Transform>> data;
            if (m_meshToData.TryGetValue(mesh, out data))
            {
                data.Item2.Remove(transform);
                if (data.Item2.Count == 0)
                {
                    m_meshToData.Remove(mesh);
                }
            }
        }

        public void Clear()
        {
            m_meshToData.Clear();
            m_batches.Clear();
        }

        public void Refresh(int maxBatchSize = 128)
        {
            m_batches.Clear();

            foreach (KeyValuePair<Mesh, Tuple<Material, List<Transform>>> kvp in m_meshToData)
            {
                if(kvp.Key == null)
                {
                    continue;
                }

                Tuple<Material, List<Transform>> data = kvp.Value;

                RenderTransformedMeshesBatch batch = new RenderTransformedMeshesBatch(kvp.Key, data.Item1);
                m_batches.Add(batch);

                int index = 0;
                for (int i = 0; i < data.Item2.Count; ++i)
                {
                    if (index == maxBatchSize)
                    {
                        batch = new RenderTransformedMeshesBatch(kvp.Key, data.Item1);
                        m_batches.Add(batch);
                        index = 0;
                    }

                    batch.Transforms.Add(data.Item2[i]);
                    index++;
                }
            }

            foreach(KeyValuePair<Mesh, RenderMeshesBatch> kvp in m_meshToBatch)
            {
                if (kvp.Key == null)
                {
                    continue;
                }

                m_batches.Add(kvp.Value);
            }
        }
    }
}
