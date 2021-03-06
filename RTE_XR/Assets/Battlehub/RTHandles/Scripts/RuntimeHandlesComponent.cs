﻿using UnityEngine;
using System.Collections;
using Battlehub.RTCommon;
using Battlehub.Utils;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Battlehub.RTHandles
{
    public enum RuntimeHandleAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        Screen = 8,
        Free = 16,
        Snap = 32,
    }

    [System.Serializable]
    public class RTHColors
    {
        public Color32 DisabledColor = new Color32(128, 128, 128, 128);
        public Color32 XColor = new Color32(187, 70, 45, 255);        
        public Color32 YColor = new Color32(139, 206, 74, 255);
        public Color32 ZColor = new Color32(55, 115, 244, 255);
        public Color32 AltColor = new Color32(192, 192, 192, 224);
        public Color32 AltColor2 = new Color32(0x38, 0x38, 0x38, 224);
        public Color32 SelectionColor = new Color32(239, 238, 64, 255);
        public Color32 SelectionAltColor = new Color(0, 0, 0, 0.1f);
        public Color32 BoundsColor = Color.green;
        public Color32 GridColor = new Color(1, 1, 1, 0.1f);
    }

    public interface IRuntimeHandlesComponent
    {
        RTHColors Colors
        {
            get;
            set;
        }

        float HandleScale
        {
            get;
            set;
        }

        float SelectionMargin
        {
            get;
            set;
        }
        
        bool InvertZAxis
        {
            get;
            set;
        }
        
        bool PositionHandleArrowOnly
        {
            get;
            set;
        }

        float SceneGizmoScale
        {
            get;
            set;
        }

        void ApplySettings();
    }
    
    /// <summary>
    /// GL Drawing routines for all handles, gizmos and grid
    /// </summary>
    public class RuntimeHandlesComponent : MonoBehaviour, IRuntimeHandlesComponent
    {
        [SerializeField]
        private RTHColors m_colors = new RTHColors();
        public RTHColors Colors
        {
            get { return m_colors; }
            set { m_colors = value; }
        }

        [SerializeField]
        private float m_handleScale = 1.0f;
        public float HandleScale
        {
            get { return m_handleScale; }
            set { m_handleScale = value; }
        }

        [SerializeField]
        private float m_selectionMargin = 1;
        public float SelectionMargin
        {
            get { return m_selectionMargin * m_handleScale; }
            set { m_selectionMargin = value; }
        }

        [SerializeField]
        private bool m_invertZAxis = false;
        public bool InvertZAxis
        {
            get { return m_invertZAxis; }
            set { m_invertZAxis = value; }
        }

        [SerializeField]
        private bool m_positionHandleArrowOnly = false;
        public bool PositionHandleArrowOnly
        {
            get { return m_positionHandleArrowOnly; }
            set { m_positionHandleArrowOnly = value; }
        }

        [SerializeField]
        private float m_sceneGizmoScale = 1.0f;
        public float SceneGizmoScale
        {
            get { return m_sceneGizmoScale; }
            set { m_sceneGizmoScale = value; }
        }
        
        public Vector3 Forward
        {
            get { return m_invertZAxis ? Vector3.back : Vector3.forward; }
        }

        protected Mesh Axes;
        protected Mesh Arrows;
        protected Mesh ArrowY;
        protected Mesh ArrowX;
        protected Mesh ArrowZ;
        protected Mesh SelectionArrowY;
        protected Mesh SelectionArrowX;
        protected Mesh SelectionArrowZ;
        protected Mesh DisabledArrowY;
        protected Mesh DisabledArrowX;
        protected Mesh DisabledArrowZ;
        protected Mesh Quads;
        protected Mesh WireQuads;
        protected Mesh Quad;

        protected Mesh SelectionCube;
        protected Mesh DisabledCube;
        protected Mesh CubeX;
        protected Mesh CubeY;
        protected Mesh CubeZ;
        protected Mesh CubeUniform;

        protected Mesh WireCircle;
        protected Mesh WireCircle11;

        protected Mesh SceneGizmoSelectedAxis;
        protected Mesh SceneGizmoXAxis;
        protected Mesh SceneGizmoYAxis;
        protected Mesh SceneGizmoZAxis;
        protected Mesh SceneGizmoCube;
        protected Mesh SceneGizmoSelectedCube;
        protected Mesh SceneGizmoQuad;

        protected Material m_shapesMaterialZTest;
        protected Material m_shapesMaterialZTest2;
        protected Material m_shapesMaterialZTest3;
        protected Material m_shapesMaterialZTest4;
        protected Material m_shapesMaterialZTestOffset;
        
        protected Material m_linesMaterial;
        protected Material m_linesClipMaterial;
        protected Material m_linesClipUsingClipPlaneMaterial;
        protected Material m_linesBillboardMaterial;
        protected Material m_xMaterial;
        protected Material m_yMaterial;
        protected Material m_zMaterial;
        protected Material m_unlitColorMaterial;

        private static RuntimeHandlesComponent m_instance;
        public static void InitializeIfRequired(ref RuntimeHandlesComponent handles)
        {
            if (handles == null)
            {
                if(m_instance == null)
                {
                    m_instance = FindObjectOfType<RuntimeHandlesComponent>();
                    if (m_instance == null)
                    {
                        IRTE rte = IOC.Resolve<IRTE>();
                        GameObject runtimeHandles = new GameObject("RuntimeHandlesComponent");
                        runtimeHandles.transform.SetParent(rte.Root.transform, false);
                        m_instance = runtimeHandles.AddComponent<RuntimeHandlesComponent>();
                    }
                }
                handles = m_instance;
            }
        }

        private float m_oldHandleScale;
        private bool m_oldInvertZAxis;

        protected virtual void Awake()
        {
            m_oldHandleScale = m_handleScale;
            m_oldInvertZAxis = m_invertZAxis;
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            if (m_instance == this)
            {
                m_instance = null;
            }
            Cleanup();
        }

        protected virtual void Update()
        {
            if(m_oldHandleScale != m_handleScale || m_oldInvertZAxis != m_invertZAxis)
            {
                m_oldHandleScale = m_handleScale;
                m_oldInvertZAxis = m_invertZAxis;
                ApplySettings();
            }
        }

        public void ApplySettings()
        {
            Cleanup();
            Initialize();
        }

        private void Initialize()
        {
            m_linesMaterial = new Material(Shader.Find("Battlehub/RTCommon/LineBillboard"));
            m_linesMaterial.color = Color.white;
            m_linesMaterial.SetFloat("_Scale", m_handleScale);
            m_linesMaterial.enableInstancing = true;

            m_linesClipMaterial = new Material(Shader.Find("Battlehub/RTHandles/LineBillboardClip"));
            m_linesClipMaterial.color = Color.white;
            m_linesClipMaterial.SetFloat("_Scale", m_handleScale);
            m_linesClipMaterial.enableInstancing = true;

            m_linesClipUsingClipPlaneMaterial = new Material(Shader.Find("Battlehub/RTHandles/VertexColorClipUsingClipPlane"));
            m_linesClipUsingClipPlaneMaterial.color = Color.white;
            m_linesClipUsingClipPlaneMaterial.enableInstancing = true;

            m_linesBillboardMaterial = new Material(Shader.Find("Battlehub/RTHandles/LineBillboardBillboard"));
            m_linesBillboardMaterial.color = Color.white;
            m_linesBillboardMaterial.SetFloat("_Scale", m_handleScale);
            m_linesBillboardMaterial.enableInstancing = true;

            m_shapesMaterialZTest = new Material(Shader.Find("Battlehub/RTHandles/Shape"));
            m_shapesMaterialZTest.color = new Color(1, 1, 1, 0);
            m_shapesMaterialZTest.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_shapesMaterialZTest.SetFloat("_ZWrite", 1.0f);
            m_shapesMaterialZTest.enableInstancing = true;

            m_shapesMaterialZTestOffset = new Material(Shader.Find("Battlehub/RTHandles/Shape"));
            m_shapesMaterialZTestOffset.color = new Color(1, 1, 1, 1);
            m_shapesMaterialZTestOffset.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_shapesMaterialZTestOffset.SetFloat("_ZWrite", 1.0f);
            m_shapesMaterialZTestOffset.SetFloat("_OFactors", -1.0f);
            m_shapesMaterialZTestOffset.SetFloat("_OUnits", -1.0f);
            m_shapesMaterialZTestOffset.enableInstancing = true;

            m_shapesMaterialZTest2 = new Material(Shader.Find("Battlehub/RTHandles/Shape"));
            m_shapesMaterialZTest2.color = new Color(1, 1, 1, 0);
            m_shapesMaterialZTest2.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_shapesMaterialZTest2.SetFloat("_ZWrite", 1.0f);
            m_shapesMaterialZTest2.enableInstancing = true;

            m_shapesMaterialZTest3 = new Material(Shader.Find("Battlehub/RTHandles/Shape"));
            m_shapesMaterialZTest3.color = new Color(1, 1, 1, 0);
            m_shapesMaterialZTest3.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_shapesMaterialZTest3.SetFloat("_ZWrite", 1.0f);
            m_shapesMaterialZTest3.enableInstancing = true;

            m_shapesMaterialZTest4 = new Material(Shader.Find("Battlehub/RTHandles/Shape"));
            m_shapesMaterialZTest4.color = new Color(1, 1, 1, 0);
            m_shapesMaterialZTest4.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_shapesMaterialZTest4.SetFloat("_ZWrite", 1.0f);
            m_shapesMaterialZTest4.enableInstancing = true;

            m_xMaterial = new Material(Shader.Find("Battlehub/RTCommon/Billboard"));
            m_xMaterial.color = Color.white;
            m_xMaterial.mainTexture = Resources.Load<Texture>("Battlehub.RuntimeHandles.x");
            m_xMaterial.enableInstancing = true;
            m_yMaterial = new Material(Shader.Find("Battlehub/RTCommon/Billboard"));
            m_yMaterial.color = Color.white;
            m_yMaterial.mainTexture = Resources.Load<Texture>("Battlehub.RuntimeHandles.y");
            m_yMaterial.enableInstancing = true;
            m_zMaterial = new Material(Shader.Find("Battlehub/RTCommon/Billboard"));
            m_zMaterial.color = Color.white;
            m_zMaterial.mainTexture = Resources.Load<Texture>("Battlehub.RuntimeHandles.z");
            m_zMaterial.enableInstancing = true;

            m_unlitColorMaterial = new Material(Shader.Find("Battlehub/RTHandles/UnlitColor"));
            m_unlitColorMaterial.enableInstancing = true;

            Axes = CreateAxes();

            Mesh selectionArrowMesh = GraphicsUtility.CreateCone(m_colors.SelectionColor, m_handleScale);
            Mesh disableArrowMesh = GraphicsUtility.CreateCone(m_colors.DisabledColor, m_handleScale);

            CombineInstance yArrow = new CombineInstance();
            yArrow.mesh = selectionArrowMesh;
            yArrow.transform = Matrix4x4.TRS(Vector3.up * m_handleScale, Quaternion.identity, Vector3.one);
            SelectionArrowY = new Mesh();
            SelectionArrowY.CombineMeshes(new[] { yArrow }, true);
            SelectionArrowY.RecalculateNormals();

            yArrow.mesh = disableArrowMesh;
            yArrow.transform = Matrix4x4.TRS(Vector3.up * m_handleScale, Quaternion.identity, Vector3.one);
            DisabledArrowY = new Mesh();
            DisabledArrowY.CombineMeshes(new[] { yArrow }, true);
            DisabledArrowY.RecalculateNormals();

            yArrow.mesh = GraphicsUtility.CreateCone(m_colors.YColor, m_handleScale);
            yArrow.transform = Matrix4x4.TRS(Vector3.up * m_handleScale, Quaternion.identity, Vector3.one);
            ArrowY = new Mesh();
            ArrowY.CombineMeshes(new[] { yArrow }, true);
            ArrowY.RecalculateNormals();

            CombineInstance xArrow = new CombineInstance();
            xArrow.mesh = selectionArrowMesh;
            xArrow.transform = Matrix4x4.TRS(Vector3.right * m_handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            SelectionArrowX = new Mesh();
            SelectionArrowX.CombineMeshes(new[] { xArrow }, true);
            SelectionArrowX.RecalculateNormals();

            xArrow.mesh = disableArrowMesh;
            xArrow.transform = Matrix4x4.TRS(Vector3.right * m_handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            DisabledArrowX = new Mesh();
            DisabledArrowX.CombineMeshes(new[] { xArrow }, true);
            DisabledArrowX.RecalculateNormals();

            xArrow.mesh = GraphicsUtility.CreateCone(m_colors.XColor, m_handleScale);
            xArrow.transform = Matrix4x4.TRS(Vector3.right * m_handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            ArrowX = new Mesh();
            ArrowX.CombineMeshes(new[] { xArrow }, true);
            ArrowX.RecalculateNormals();

            Vector3 zAxis = Forward * m_handleScale;
            Quaternion zRotation = m_invertZAxis ? Quaternion.AngleAxis(-90, Vector3.right) : Quaternion.AngleAxis(90, Vector3.right);
            CombineInstance zArrow = new CombineInstance();
            zArrow.mesh = selectionArrowMesh;
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            SelectionArrowZ = new Mesh();
            SelectionArrowZ.CombineMeshes(new[] { zArrow }, true);
            SelectionArrowZ.RecalculateNormals();

            zArrow.mesh = disableArrowMesh;
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            DisabledArrowZ = new Mesh();
            DisabledArrowZ.CombineMeshes(new[] { zArrow }, true);
            DisabledArrowZ.RecalculateNormals();

            zArrow.mesh = GraphicsUtility.CreateCone(m_colors.ZColor, m_handleScale);
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            ArrowZ = new Mesh();
            ArrowZ.CombineMeshes(new[] { zArrow }, true);
            ArrowZ.RecalculateNormals();

            yArrow.mesh = GraphicsUtility.CreateCone(m_colors.YColor, m_handleScale);
            xArrow.mesh = GraphicsUtility.CreateCone(m_colors.XColor, m_handleScale);
            zArrow.mesh = GraphicsUtility.CreateCone(m_colors.ZColor, m_handleScale);
            Arrows = new Mesh();
            Arrows.CombineMeshes(new[] { yArrow, xArrow, zArrow }, true);
            Arrows.RecalculateNormals();

            Quad = GraphicsUtility.CreateWireQuad(0.2f * m_handleScale, 0.2f * m_handleScale);
            Quads = CreatePositionHandleQuads();
            WireQuads = CreatePositionHandleWireQuads();

            SelectionCube = GraphicsUtility.CreateCube(m_colors.SelectionColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);
            DisabledCube = GraphicsUtility.CreateCube(m_colors.DisabledColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);
            CubeX = GraphicsUtility.CreateCube(m_colors.XColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);
            CubeY = GraphicsUtility.CreateCube(m_colors.YColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);
            CubeZ = GraphicsUtility.CreateCube(m_colors.ZColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);
            CubeUniform = GraphicsUtility.CreateCube(m_colors.AltColor, Vector3.zero, m_handleScale, 0.1f, 0.1f, 0.1f);

            WireCircle = GraphicsUtility.CreateWireCircle();
            WireCircle11 = GraphicsUtility.CreateWireCircle(1.1f);

            SceneGizmoSelectedAxis = CreateSceneGizmoHalfAxis(m_colors.SelectionColor, Quaternion.AngleAxis(90, Vector3.right));
            SceneGizmoXAxis = CreateSceneGizmoAxis(m_colors.XColor, m_colors.AltColor, Quaternion.AngleAxis(-90, Vector3.forward));
            SceneGizmoYAxis = CreateSceneGizmoAxis(m_colors.YColor, m_colors.AltColor, Quaternion.identity);
            SceneGizmoZAxis = CreateSceneGizmoAxis(m_colors.ZColor, m_colors.AltColor, zRotation);
            SceneGizmoCube = GraphicsUtility.CreateCube(m_colors.AltColor, Vector3.zero, 1);
            SceneGizmoSelectedCube = GraphicsUtility.CreateCube(m_colors.SelectionColor, Vector3.zero, 1);
            SceneGizmoQuad = GraphicsUtility.CreateQuad();
        }

        private void Cleanup()
        {
            if (Axes != null) Destroy(Axes);
            if (Arrows != null) Destroy(Arrows);
            if (ArrowY != null) Destroy(ArrowY);
            if (ArrowZ != null) Destroy(ArrowZ);
            if (SelectionArrowY != null) Destroy(SelectionArrowY);
            if (SelectionArrowX != null) Destroy(SelectionArrowX);
            if (SelectionArrowZ != null) Destroy(SelectionArrowZ);
            if (DisabledArrowY != null) Destroy(DisabledArrowY);
            if (DisabledArrowX != null) Destroy(DisabledArrowX);
            if (DisabledArrowZ != null) Destroy(DisabledArrowZ);
            if (Quad != null) Destroy(Quad);
            if (WireQuads != null) Destroy(WireQuads);
            if (Quads != null) Destroy(Quads);

            if (SelectionCube != null) Destroy(SelectionCube);
            if (DisabledCube != null) Destroy(DisabledCube);
            if (CubeX != null) Destroy(CubeX);
            if (CubeY != null) Destroy(CubeY);
            if (CubeZ != null) Destroy(CubeZ);
            if (CubeUniform != null) Destroy(CubeUniform);

            if (WireCircle != null) Destroy(WireCircle);
            if (WireCircle11 != null) Destroy(WireCircle11);

            if (SceneGizmoSelectedAxis != null) Destroy(SceneGizmoSelectedAxis);
            if (SceneGizmoXAxis != null) Destroy(SceneGizmoXAxis);
            if (SceneGizmoYAxis != null) Destroy(SceneGizmoYAxis);
            if (SceneGizmoZAxis != null) Destroy(SceneGizmoZAxis);
            if (SceneGizmoCube != null) Destroy(SceneGizmoCube);
            if (SceneGizmoSelectedCube != null) Destroy(SceneGizmoSelectedCube);
            if (SceneGizmoQuad != null) Destroy(SceneGizmoQuad);

            if (m_shapesMaterialZTest != null) Destroy(m_shapesMaterialZTest);
            if (m_shapesMaterialZTest2 != null) Destroy(m_shapesMaterialZTest2);
            if (m_shapesMaterialZTest3 != null) Destroy(m_shapesMaterialZTest3);
            if (m_shapesMaterialZTest4 != null) Destroy(m_shapesMaterialZTest4);
            if (m_shapesMaterialZTestOffset != null) Destroy(m_shapesMaterialZTestOffset);
            
            if (m_linesMaterial != null) Destroy(m_linesMaterial);
            if (m_linesClipMaterial != null) Destroy(m_linesClipMaterial);
            if (m_linesClipUsingClipPlaneMaterial != null) Destroy(m_linesClipUsingClipPlaneMaterial);
            if (m_linesBillboardMaterial != null) Destroy(m_linesBillboardMaterial);
            if (m_xMaterial != null) Destroy(m_xMaterial);
            if (m_yMaterial != null) Destroy(m_yMaterial);
            if (m_zMaterial != null) Destroy(m_zMaterial);
            if (m_unlitColorMaterial != null) Destroy(m_unlitColorMaterial);
        }
        
        private static Mesh CreateSceneGizmoHalfAxis(Color color, Quaternion rotation)
        {
            const float scale = 0.1f;
            Mesh cone1 = GraphicsUtility.CreateCone(color, 1);

            CombineInstance cone1Combine = new CombineInstance();
            cone1Combine.mesh = cone1;
            cone1Combine.transform = Matrix4x4.TRS(Vector3.up * scale, Quaternion.AngleAxis(180, Vector3.right), Vector3.one);

            Mesh result = new Mesh();
            result.CombineMeshes(new[] { cone1Combine }, true);

            CombineInstance rotateCombine = new CombineInstance();
            rotateCombine.mesh = result;
            rotateCombine.transform = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

            result = new Mesh();
            result.CombineMeshes(new[] { rotateCombine }, true);
            result.RecalculateNormals();
            return result;
        }

        private static Mesh CreateSceneGizmoAxis(Color axisColor, Color altColor, Quaternion rotation)
        {
            const float scale = 0.1f;
            Mesh cone1 = GraphicsUtility.CreateCone(axisColor, 1);
            Mesh cone2 = GraphicsUtility.CreateCone(altColor, 1);

            CombineInstance cone1Combine = new CombineInstance();
            cone1Combine.mesh = cone1;
            cone1Combine.transform = Matrix4x4.TRS(Vector3.up * scale,  Quaternion.AngleAxis(180, Vector3.right), Vector3.one);

            CombineInstance cone2Combine = new CombineInstance();
            cone2Combine.mesh = cone2;
            cone2Combine.transform = Matrix4x4.TRS(Vector3.down * scale, Quaternion.identity, Vector3.one);

            Mesh result = new Mesh();
            result.CombineMeshes(new[] { cone1Combine, cone2Combine }, true);

            CombineInstance rotateCombine = new CombineInstance();
            rotateCombine.mesh = result;
            rotateCombine.transform = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

            result = new Mesh();
            result.CombineMeshes(new[] { rotateCombine }, true);
            result.RecalculateNormals();
            return result;
        }

        private Mesh CreateAxes()
        {
            Vector3 x = Vector3.right * 0.95f;
            Vector3 y = Vector3.up * 0.95f;
            Vector3 z = Forward * 0.95f;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                Vector3.zero,
                y,
                Vector3.zero,
                z
            };
            mesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);
            mesh.SetIndices(new[] { 2, 3 }, MeshTopology.Lines, 1);
            mesh.SetIndices(new[] { 4, 5 }, MeshTopology.Lines, 2);
            return mesh;
        }

        private Mesh CreatePositionHandleWireQuads()
        {
            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;

            Vector3 xy = x + y;
            Vector3 xz = x + z;
            Vector3 yz = y + z;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                y,
                z,
                xy,
                xz,
                yz,
            };
            mesh.SetIndices(new[] { 0, 2, 2, 4, 4, 1, 1, 0 }, MeshTopology.Lines, 0);
            mesh.SetIndices(new[] { 0, 1, 1, 5, 5, 3, 3, 0 }, MeshTopology.Lines, 1);
            mesh.SetIndices(new[] { 0, 3, 3, 6, 6, 2, 2, 0 }, MeshTopology.Lines, 2);

            return mesh;
        }

        private Mesh CreatePositionHandleQuads()
        {
            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;

            Vector3 xy = x + y;
            Vector3 xz = x + z;
            Vector3 yz = y + z;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                y,
                z,
                xy,
                xz,
                yz,
            };
            mesh.SetIndices(new[] { 0, 2, 4, 1 }, MeshTopology.Quads, 0);
            mesh.SetIndices(new[] { 0, 1, 5, 3 }, MeshTopology.Quads, 1);
            mesh.SetIndices(new[] { 0, 3, 6, 2 }, MeshTopology.Quads, 2);

            return mesh;
        }

        public static float GetScreenScale(Vector3 position, Camera camera)
        {
            return GraphicsUtility.GetScreenScale(position, camera);
        }

        private void DoAxes(CommandBuffer commandBuffer, MaterialPropertyBlock[] propertyBlocks, Matrix4x4 transform, RuntimeHandleAxis selectedAxis, bool xLocked, bool yLocked, bool zLocked)
        {
            if (xLocked)
            {
                if(m_colors.DisabledColor.a > 0)
                {
                    propertyBlocks[0].SetColor("_Color", m_colors.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 0, 0, propertyBlocks[0]);
                }
            }
            else
            {
                propertyBlocks[0].SetColor("_Color", (selectedAxis & RuntimeHandleAxis.X) == 0 ? m_colors.XColor : m_colors.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 0, 0, propertyBlocks[0]);
            }

            if (yLocked)
            {
                if(m_colors.DisabledColor.a > 0)
                {
                    propertyBlocks[1].SetColor("_Color", m_colors.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 1, 0, propertyBlocks[1]);
                }
            }
            else
            {
                propertyBlocks[1].SetColor("_Color", (selectedAxis & RuntimeHandleAxis.Y) == 0 ? m_colors.YColor : m_colors.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 1, 0, propertyBlocks[1]);
            }

            if (zLocked)
            {
                if(m_colors.DisabledColor.a > 0)
                {
                    propertyBlocks[2].SetColor("_Color", m_colors.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 2, 0, propertyBlocks[2]);
                }
            }
            else
            {
                propertyBlocks[2].SetColor("_Color", (selectedAxis & RuntimeHandleAxis.Z) == 0 ? m_colors.ZColor : m_colors.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, m_linesMaterial, 2, 0, propertyBlocks[2]);
            }
        }
      
        public void DoPositionHandle(CommandBuffer commandBuffer, MaterialPropertyBlock[] propertyBlocks, Camera camera, Vector3 position, Quaternion rotation, 
            RuntimeHandleAxis selectedAxis = RuntimeHandleAxis.None, bool snapMode = false, LockObject lockObject = null)
        {
            float screenScale = GetScreenScale(position, camera);
            Matrix4x4 linesTransform = Matrix4x4.TRS(position, rotation, new Vector3(screenScale, screenScale, screenScale) * m_handleScale);

            bool xLocked = lockObject != null && lockObject.PositionX;
            bool yLocked = lockObject != null && lockObject.PositionY;
            bool zLocked = lockObject != null && lockObject.PositionZ;

            DoAxes(commandBuffer, propertyBlocks, linesTransform, selectedAxis, xLocked, yLocked, zLocked);

            Matrix4x4 transform = Matrix4x4.TRS(position, rotation, new Vector3(screenScale, screenScale, screenScale));
            if (snapMode)
            {

                if (selectedAxis == RuntimeHandleAxis.Snap)
                {
                    propertyBlocks[4].SetColor("_Color", m_colors.SelectionColor);
                }
                else
                {
                    propertyBlocks[4].SetColor("_Color", m_colors.AltColor);
                }

                commandBuffer.DrawMesh(Quad, transform, m_linesBillboardMaterial, 0, 0, propertyBlocks[4]);
            }
            else
            {
                if(!PositionHandleArrowOnly)
                {
                    Vector3 toCam = transform.inverse.MultiplyVector(camera.transform.position - position);

                    Matrix4x4 quadTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.right)) * 0.2f, 
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.up)) * 0.2f, 
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.forward)) * 0.2f));

                    Matrix4x4 matrix = linesTransform * quadTransform;

                    if (!xLocked && !yLocked)
                    {
                        Color32 color = m_colors.ZColor;
                        color.a = 128;
                        propertyBlocks[5].SetColor("_Color", selectedAxis != RuntimeHandleAxis.XY ? color : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, m_unlitColorMaterial, 0, 0, propertyBlocks[5]);

                        propertyBlocks[6].SetColor("_Color", selectedAxis != RuntimeHandleAxis.XY ? m_colors.ZColor : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, m_linesMaterial, 0, 0, propertyBlocks[6]);
                    }

                    if (!xLocked && !zLocked)
                    {
                        Color32 color = m_colors.YColor;
                        color.a = 128;
                        propertyBlocks[7].SetColor("_Color", selectedAxis != RuntimeHandleAxis.XZ ? color : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, m_unlitColorMaterial, 1, 0, propertyBlocks[7]);

                        propertyBlocks[8].SetColor("_Color", selectedAxis != RuntimeHandleAxis.XZ ? m_colors.YColor : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, m_linesMaterial, 1, 0, propertyBlocks[8]);
                    }

                    if (!yLocked && !zLocked)
                    {
                        Color32 color = m_colors.XColor;
                        color.a = 128;
                        propertyBlocks[9].SetColor("_Color", selectedAxis != RuntimeHandleAxis.YZ ? color : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, m_unlitColorMaterial, 2, 0, propertyBlocks[9]);

                        propertyBlocks[10].SetColor("_Color", selectedAxis != RuntimeHandleAxis.YZ ? m_colors.XColor : m_colors.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, m_linesMaterial, 2, 0, propertyBlocks[10]);
                    }
                }
            }

            if (!xLocked && !yLocked && !zLocked)
            {
                commandBuffer.DrawMesh(Arrows, transform, m_shapesMaterialZTest, 0, 0);
                if ((selectedAxis & RuntimeHandleAxis.X) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowX, transform, m_shapesMaterialZTest, 0, 0);
                }
                if ((selectedAxis & RuntimeHandleAxis.Y) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowY, transform, m_shapesMaterialZTest, 0, 0);
                }
                if ((selectedAxis & RuntimeHandleAxis.Z) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowZ, transform, m_shapesMaterialZTest, 0, 0);
                }
            }
            else
            {
                if (xLocked)
                {
                    commandBuffer.DrawMesh(DisabledArrowX, transform, m_shapesMaterialZTest, 0, 0);
                }
                else
                {
                    if ((selectedAxis & RuntimeHandleAxis.X) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowX, transform, m_shapesMaterialZTest, 0, 0);
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowX, transform, m_shapesMaterialZTest, 0, 0);
                    }
                }

                if (yLocked)
                {
                    commandBuffer.DrawMesh(DisabledArrowY, transform, m_shapesMaterialZTest, 0, 0);
                }
                else 
                {
                    if ((selectedAxis & RuntimeHandleAxis.Y) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowY, transform, m_shapesMaterialZTest, 0, 0); 
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowY, transform, m_shapesMaterialZTest, 0, 0);
                    }     
                }

                if (zLocked)
                {
                    commandBuffer.DrawMesh(DisabledArrowZ, transform, m_shapesMaterialZTest, 0, 0);
                }
                else 
                {
                    if ((selectedAxis & RuntimeHandleAxis.Z) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowZ, transform, m_shapesMaterialZTest, 0, 0);
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowZ, transform, m_shapesMaterialZTest, 0, 0);
                    }    
                }
            }
        }

        public void DoRotationHandle(CommandBuffer commandBuffer, MaterialPropertyBlock[] propertyBlocks, Camera camera, Quaternion rotation, Vector3 position, RuntimeHandleAxis selectedAxis = RuntimeHandleAxis.None, LockObject lockObject = null, bool cameraFacingBillboardMode = true)
        {
            float screenScale = GetScreenScale(position, camera);
            float radius = m_handleScale;
            Vector3 scale = new Vector3(screenScale, screenScale, screenScale) * radius;
            Matrix4x4 xTranform = Matrix4x4.TRS(Vector3.zero, rotation * Quaternion.AngleAxis(-90, Vector3.up), Vector3.one);
            Matrix4x4 yTranform = Matrix4x4.TRS(Vector3.zero, rotation * Quaternion.AngleAxis(-90, Vector3.right), Vector3.one);
            Matrix4x4 zTranform = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            Matrix4x4 objToWorld = Matrix4x4.TRS(position, Quaternion.identity, scale);

            bool xLocked = lockObject != null && lockObject.RotationX;
            bool yLocked = lockObject != null && lockObject.RotationY;
            bool zLocked = lockObject != null && lockObject.RotationZ;
            bool freeLocked = lockObject != null && lockObject.RotationFree;
            bool screenLocked = lockObject != null && lockObject.RotationScreen;

            Matrix4x4 matrix;
            Material material;

            if (cameraFacingBillboardMode)
            {
                material = m_linesMaterial;
                matrix = Matrix4x4.TRS(position, Quaternion.LookRotation(camera.transform.position - position), scale);
            }
            else
            {
                material = m_linesBillboardMaterial;
                matrix = objToWorld;
            }

            if (freeLocked)
                propertyBlocks[0].SetColor("_Color", m_colors.DisabledColor);
            else
                propertyBlocks[0].SetColor("_Color", selectedAxis != RuntimeHandleAxis.Free ? m_colors.AltColor : m_colors.SelectionColor);
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, matrix, material, propertyBlocks[0]);

            if (screenLocked)
                propertyBlocks[1].SetColor("_Color", m_colors.DisabledColor);
            else
                propertyBlocks[1].SetColor("_Color", selectedAxis != RuntimeHandleAxis.Screen ? m_colors.AltColor : m_colors.SelectionColor);
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle11, matrix, material, propertyBlocks[1]);

            if(cameraFacingBillboardMode)
            {
                material = m_linesClipUsingClipPlaneMaterial;
            }
            else
            {
                material = m_linesClipMaterial;
            }
            
            if(xLocked)
                propertyBlocks[2].SetColor("_Color", m_colors.DisabledColor);
            else
                propertyBlocks[2].SetColor("_Color", selectedAxis != RuntimeHandleAxis.X ? m_colors.XColor : m_colors.SelectionColor);
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * xTranform, material, propertyBlocks[2]);

            if (yLocked)
                propertyBlocks[3].SetColor("_Color", m_colors.DisabledColor);
            else
                propertyBlocks[3].SetColor("_Color", selectedAxis != RuntimeHandleAxis.Y ? m_colors.YColor : m_colors.SelectionColor);
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * yTranform, material, propertyBlocks[3]);
            
            if (zLocked)
                propertyBlocks[4].SetColor("_Color", m_colors.DisabledColor);
            else
                propertyBlocks[4].SetColor("_Color", selectedAxis != RuntimeHandleAxis.Z ? m_colors.ZColor : m_colors.SelectionColor);
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * zTranform, material, propertyBlocks[4]);
        }

        public void DoScaleHandle(CommandBuffer commandBuffer, MaterialPropertyBlock[] propertyBlocks, Camera camera, Vector3 scale, Vector3 position, Quaternion rotation, RuntimeHandleAxis selectedAxis = RuntimeHandleAxis.None,  LockObject lockObject = null)
        {
            float sScale = GetScreenScale(position, camera);
            Matrix4x4 linesTransform = Matrix4x4.TRS(position, rotation, scale * sScale * m_handleScale);

            bool xLocked = lockObject != null && lockObject.ScaleX;
            bool yLocked = lockObject != null && lockObject.ScaleY;
            bool zLocked = lockObject != null && lockObject.ScaleZ;

            DoAxes(commandBuffer, propertyBlocks, linesTransform, selectedAxis, xLocked, yLocked, zLocked);
                     
            Matrix4x4 rotM = Matrix4x4.TRS(Vector3.zero, rotation, scale);
            Vector3 screenScale = new Vector3(sScale, sScale, sScale);
            Vector3 xOffset = rotM.MultiplyVector(Vector3.right) * sScale * m_handleScale;
            Vector3 yOffset = rotM.MultiplyVector(Vector3.up) * sScale * m_handleScale;
            Vector3 zOffset = rotM.MultiplyPoint(Forward) * sScale * m_handleScale;
            if (selectedAxis == RuntimeHandleAxis.X)
            {  
                commandBuffer.DrawMesh(xLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + xOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(xLocked && yLocked && zLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), m_shapesMaterialZTest, 0, 0);
            }
            else if (selectedAxis == RuntimeHandleAxis.Y)
            {
                commandBuffer.DrawMesh(xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(yLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + yOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(xLocked && yLocked && zLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), m_shapesMaterialZTest, 0, 0);
            }
            else if (selectedAxis == RuntimeHandleAxis.Z)
            {
                commandBuffer.DrawMesh(xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(zLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + zOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(xLocked && yLocked && zLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), m_shapesMaterialZTest, 0, 0);
            }
            else if (selectedAxis == RuntimeHandleAxis.Free)
            {
                commandBuffer.DrawMesh(xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(xLocked && yLocked && zLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), m_shapesMaterialZTest, 0, 0);
            }
            else
            {
                commandBuffer.DrawMesh(xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), m_shapesMaterialZTest, 0, 0);
                commandBuffer.DrawMesh(xLocked && yLocked && zLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), m_shapesMaterialZTest, 0, 0);
            }
        }

        public void DoSceneGizmo(CommandBuffer commandBuffer, Camera camera, Vector3 position, Quaternion rotation, Vector3 selection, float gizmoScale, Color textColor, float xAlpha = 1.0f, float yAlpha = 1.0f, float zAlpha = 1.0f)
        {
            float sScale = GetScreenScale(position, camera) * gizmoScale;
            Vector3 screenScale = new Vector3(sScale, sScale, sScale);

            const float billboardScale = 0.125f;
            float billboardOffset = 0.4f;
            if (camera.orthographic)
            {
                billboardOffset = 0.42f;
            }
            
            const float cubeScale = 0.15f;

            if (selection != Vector3.zero)
            {
                if (selection == Vector3.one)
                {
                    commandBuffer.DrawMesh(SceneGizmoSelectedCube, Matrix4x4.TRS(position, rotation, screenScale * cubeScale), m_shapesMaterialZTestOffset, 0);
                }
                else
                {
                    if ((xAlpha == 1.0f || xAlpha == 0.0f) && 
                        (yAlpha == 1.0f || yAlpha == 0.0f) && 
                        (zAlpha == 1.0f || zAlpha == 0.0f))
                    {
                        commandBuffer.DrawMesh(SceneGizmoSelectedAxis, Matrix4x4.TRS(position, rotation * Quaternion.LookRotation(selection, Vector3.up), screenScale), m_shapesMaterialZTestOffset, 0);
                    }
                }
            }

            m_shapesMaterialZTest.color = Color.white;
            commandBuffer.DrawMesh(SceneGizmoCube, Matrix4x4.TRS(position, rotation, screenScale * cubeScale), m_shapesMaterialZTest, 0);
            if (xAlpha == 1.0f && yAlpha == 1.0f && zAlpha == 1.0f)
            {
                m_shapesMaterialZTest3.color = new Color(1, 1, 1, 1);
                commandBuffer.DrawMesh(SceneGizmoXAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest3, 0);
                m_shapesMaterialZTest4.color = new Color(1, 1, 1, 1);
                commandBuffer.DrawMesh(SceneGizmoYAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest4, 0);
                m_shapesMaterialZTest2.color = new Color(1, 1, 1, 1);
                commandBuffer.DrawMesh(SceneGizmoZAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest2, 0);
            }
            else
            {
                if (xAlpha < 1)
                {
                    m_shapesMaterialZTest3.color = new Color(1, 1, 1, yAlpha);
                    commandBuffer.DrawMesh(SceneGizmoYAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest3, 0);

                    m_shapesMaterialZTest4.color = new Color(1, 1, 1, zAlpha);
                    commandBuffer.DrawMesh(SceneGizmoZAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest4, 0);
                    
                    m_shapesMaterialZTest2.color = new Color(1, 1, 1, xAlpha);
                    commandBuffer.DrawMesh(SceneGizmoXAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest2, 0);
                   
                }
                else if (yAlpha < 1)
                {
                    m_shapesMaterialZTest4.color = new Color(1, 1, 1, zAlpha);
                    commandBuffer.DrawMesh(SceneGizmoZAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest4, 0);
                    
                    m_shapesMaterialZTest2.color = new Color(1, 1, 1, xAlpha);
                    commandBuffer.DrawMesh(SceneGizmoXAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest2, 0);

                    m_shapesMaterialZTest3.color = new Color(1, 1, 1, yAlpha);
                    commandBuffer.DrawMesh(SceneGizmoYAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest3, 0);
                }
                else
                {
                    m_shapesMaterialZTest2.color = new Color(1, 1, 1, xAlpha);
                    commandBuffer.DrawMesh(SceneGizmoXAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest2, 0);

                    m_shapesMaterialZTest3.color = new Color(1, 1, 1, yAlpha);
                    commandBuffer.DrawMesh(SceneGizmoYAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest3, 0);
                    
                    m_shapesMaterialZTest4.color = new Color(1, 1, 1, zAlpha);
                    commandBuffer.DrawMesh(SceneGizmoZAxis, Matrix4x4.TRS(position, rotation, screenScale), m_shapesMaterialZTest4, 0);
                }
            }

            Color c = textColor;
            m_xMaterial.color = new Color(c.r, c.b, c.g, xAlpha);
            DragSceneGizmoAxis(commandBuffer, m_xMaterial, camera, position, rotation, Vector3.right, gizmoScale, billboardScale, billboardOffset, sScale);
            
            m_yMaterial.color = new Color(c.r, c.b, c.g, yAlpha);
            DragSceneGizmoAxis(commandBuffer, m_yMaterial, camera, position, rotation, Vector3.up, gizmoScale, billboardScale, billboardOffset, sScale);

            m_zMaterial.color = new Color(c.r, c.b, c.g, zAlpha);
            DragSceneGizmoAxis(commandBuffer, m_zMaterial, camera, position, rotation, Forward, gizmoScale, billboardScale, billboardOffset, sScale);
        }

        private void DragSceneGizmoAxis(CommandBuffer cmdBuffer, Material material, Camera camera, Vector3 position, Quaternion rotation, Vector3 axis, float gizmoScale, float billboardScale, float billboardOffset, float sScale)
        {
            Vector3 reflectionOffset;

            reflectionOffset = Vector3.Reflect(camera.transform.forward, axis) * 0.1f;
            float dotAxis = Vector3.Dot(camera.transform.forward, axis);
            if (dotAxis > 0)
            {
                if(camera.orthographic)
                {
                    reflectionOffset += axis * dotAxis * 0.4f;
                }
                else
                {
                    reflectionOffset = axis * dotAxis * 0.7f;
                }
                
            }
            else
            {
                if (camera.orthographic)
                {
                    reflectionOffset -= axis * dotAxis * 0.1f;
                }
                else
                {
                    reflectionOffset = Vector3.zero;
                }
            }

            Vector3 pos = position + (axis + reflectionOffset) * billboardOffset * sScale;
            float scale = GetScreenScale(pos, camera) * gizmoScale;
            Vector3 scaleVector = new Vector3(scale, scale, scale);
            cmdBuffer.DrawMesh(SceneGizmoQuad, Matrix4x4.TRS(pos, rotation, scaleVector * billboardScale), material, 0);
        }

        public Mesh CreateGridMesh(Color color, float spacing, int linesCount = 150)
        {
            int count = linesCount / 2;

            Mesh mesh = new Mesh();
            mesh.name = "Grid " + spacing;

            int index = 0;
            int[] indices = new int[count * 8];
            Vector3[] vertices = new Vector3[count * 8];
            Color[] colors = new Color[count * 8];
            
            for(int i = -count; i < count; ++i)
            {
                vertices[index] = new Vector3(i * spacing, 0, -count * spacing);
                vertices[index + 1] = new Vector3(i * spacing, 0, count * spacing);

                vertices[index + 2] = new Vector3(-count * spacing, 0, i * spacing);
                vertices[index + 3] = new Vector3(count * spacing, 0, i * spacing);

                indices[index] = index;
                indices[index + 1] = index + 1;
                indices[index + 2] = index + 2;
                indices[index + 3] = index + 3;

                colors[index] = colors[index + 1] = colors[index + 2] = colors[index + 3] = color;

                index += 4;
            }

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.colors = colors;

            return mesh;
        }
    }
}
