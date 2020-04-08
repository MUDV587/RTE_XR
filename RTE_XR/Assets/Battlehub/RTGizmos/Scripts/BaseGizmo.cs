﻿using UnityEngine;
using Battlehub.RTCommon;

namespace Battlehub.RTGizmos
{
    [DefaultExecutionOrder(-50)]
    public abstract class BaseGizmo : RTEComponent
    {
        public float GridSize = 1.0f;
        public Color LineColor = new Color(0.0f, 1, 0.0f, 0.75f);
        public Color HandlesColor = new Color(0.0f, 1, 0.0f, 0.75f);
        public Color SelectionColor = new Color(1.0f, 1.0f, 0, 1.0f);

        private MaterialPropertyBlock m_lineProperties;
        protected MaterialPropertyBlock LineProperties
        {
            get { return m_lineProperties; }
        }
        private MaterialPropertyBlock m_handleProperties;
        protected MaterialPropertyBlock HandleProperties
        {
            get { return m_handleProperties; }
        }
        
        private MaterialPropertyBlock m_selectionProperties;
        protected MaterialPropertyBlock SelectionProperties
        {
            get { return m_selectionProperties; }
        }

        public bool EnableUndo = true;

        /// <summary>
        /// Key which activates Unit Snapping
        /// </summary>
        public KeyCode UnitSnapKey = KeyCode.LeftControl;
        public Camera SceneCamera;

        /// <summary>
        /// Screen space selection margin in pixels
        /// </summary>
        public float SelectionMargin = 10;

        public Transform Target;
        private bool m_isDragging;
        private int m_dragIndex;
        private Plane m_dragPlane;
        private Vector3 m_prevPoint;
        private Vector3 m_normal;

        private Vector3 m_prevPosition;
        private Quaternion m_prevRotation;
        private Vector3 m_prevScale;

        private Vector3 m_prevCamPosition;
        private Quaternion m_prevCamRotation;
        private bool m_prevOrthographic;

        private bool m_refreshOnCameraChanged;
        protected bool RefreshOnCameraChanged
        {
            get { return m_refreshOnCameraChanged; }
            set { m_refreshOnCameraChanged = value; }
        }

        protected int DragIndex
        {
            get { return m_dragIndex; }
        }

        protected bool IsDragging
        {
            get { return m_isDragging; }
        }

        protected abstract Matrix4x4 HandlesTransform
        {
            get;
        }

        protected virtual Matrix4x4 HandlesTransformInverse
        {
            get { return Matrix4x4.TRS(Target.position, Target.rotation, Target.lossyScale).inverse; }
        }

        private Vector3[] m_handlesNormals;
        private Vector3[] m_handlesPositions;
        protected virtual Vector3[] HandlesPositions
        {
            get { return m_handlesPositions; }
        }

        protected virtual Vector3[] HandlesNormals
        {
            get { return m_handlesNormals; }
        }

        private Matrix4x4 m_handlesTransform;
        private Matrix4x4 m_handlesInverseTransform;

        public override RuntimeWindow Window
        {
            get { return m_window; }
            set
            {
                m_window = value;
                m_editor = IOC.Resolve<IRTE>();
            }
        }

        private IRTECamera m_rteCamera;
        protected IRTECamera RTECamera
        {
            get { return m_rteCamera; }
        }

        private void Start()
        {
            BaseGizmoInput input = GetComponent<BaseGizmoInput>();
            if (input == null || input.Gizmo != this)
            {
                input = gameObject.AddComponent<BaseGizmoInput>();
                input.Gizmo = this;
            }

            if (SceneCamera == null)
            {
                SceneCamera = Window.Camera;
            }

            if (SceneCamera == null)
            {
                SceneCamera = Camera.main;
            }

            if(Target == null)
            {
                Target = transform;
            }

            if (EnableUndo)
            {
                if (!RuntimeUndoInput.IsInitialized)
                {
                    GameObject runtimeUndo = new GameObject();
                    runtimeUndo.name = "RuntimeUndo";
                    runtimeUndo.AddComponent<RuntimeUndoInput>();
                }
            }

            IRTEGraphicsLayer graphicsLayer = IOC.Resolve<IRTEGraphicsLayer>();
            if (graphicsLayer != null)
            {
                m_rteCamera = graphicsLayer.Camera;
            }

            if (m_rteCamera == null && SceneCamera != null)
            {
                m_rteCamera = SceneCamera.GetComponent<IRTECamera>();
                if(m_rteCamera == null)
                {
                    m_rteCamera = SceneCamera.gameObject.AddComponent<RTECamera>();
                }
            }

            if(m_rteCamera != null)
            {
                m_prevPosition = transform.position;
                m_prevRotation = transform.rotation;
                m_prevScale = transform.localScale;

                m_prevCamPosition = m_rteCamera.Camera.transform.position;
                m_prevCamRotation = m_rteCamera.Camera.transform.rotation;
                m_prevOrthographic = m_rteCamera.Camera.orthographic;

                m_rteCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            StartOverride();
        }

        private void OnEnable()
        {
            if (m_rteCamera != null)
            {
                m_rteCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            OnEnableOverride();
        }

        private void OnDisable()
        {
            if (m_rteCamera != null)
            {
                m_rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            OnDisableOverride();
        }

        private void Update()
        {
            if (m_isDragging)
            {
                Vector3 point;
                if (GetPointOnDragPlane(Window.Pointer.ScreenPoint, out point))
                {
                    Vector3 offset = m_handlesInverseTransform.MultiplyVector(point - m_prevPoint);
                    offset = Vector3.Project(offset, m_normal);
                    if (Window.Editor.Input.GetKey(UnitSnapKey) || Window.Editor.Tools.UnitSnapping)
                    {
                        Vector3 gridOffset = Vector3.zero;
                        if (Mathf.Abs(offset.x * 1.5f) >= GridSize)
                        {
                            gridOffset.x = GridSize * Mathf.Sign(offset.x);
                        }

                        if (Mathf.Abs(offset.y * 1.5f) >= GridSize)
                        {
                            gridOffset.y = GridSize * Mathf.Sign(offset.y);
                        }

                        if (Mathf.Abs(offset.z * 1.5f) >= GridSize)
                        {
                            gridOffset.z = GridSize * Mathf.Sign(offset.z);
                        }

                        if (gridOffset != Vector3.zero)
                        {
                            if (OnDrag(m_dragIndex, gridOffset))
                            {
                                m_prevPoint = point;
                                if(m_rteCamera != null)
                                {
                                    m_rteCamera.RefreshCommandBuffer();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (OnDrag(m_dragIndex, offset))
                        {
                            m_prevPoint = point;
                            if(m_rteCamera != null)
                            {
                                m_rteCamera.RefreshCommandBuffer();
                            }
                            
                        }
                    }
                }
            }
            else
            {
                if (m_rteCamera != null)
                {
                    if (m_prevPosition != transform.position || m_prevRotation != transform.rotation || m_prevScale != transform.localScale)
                    {
                        m_prevPosition = transform.position;
                        m_prevRotation = transform.rotation;
                        m_prevScale = transform.localScale;

                        m_rteCamera.RefreshCommandBuffer();
                    }  
                }
            }

            UpdateOverride();
        }

        private void LateUpdate()
        {
            if (!m_isDragging)
            {
                if (m_rteCamera != null && m_refreshOnCameraChanged)
                {
                    Camera camera = m_rteCamera.Camera;
                    if (m_prevCamPosition != camera.transform.position || m_prevCamRotation != camera.transform.rotation || m_prevOrthographic != camera.orthographic)
                    {
                        m_prevCamPosition = camera.transform.position;
                        m_prevCamRotation = camera.transform.rotation;
                        m_prevOrthographic = camera.orthographic;

                        m_rteCamera.RefreshCommandBuffer();
                    }
                }
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_handlesPositions = RuntimeGizmos.GetHandlesPositions();
            m_handlesNormals = RuntimeGizmos.GetHandlesNormals();

            m_lineProperties = new MaterialPropertyBlock();
            m_handleProperties = new MaterialPropertyBlock();
            m_selectionProperties = new MaterialPropertyBlock();            
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            BaseGizmoInput gizmoInput = GetComponent<BaseGizmoInput>();
            if (gizmoInput)
            {
                Destroy(gizmoInput);
            }

            if (Window.Editor.Tools.ActiveTool == this)
            {
                Window.Editor.Tools.ActiveTool = null;
            }

            if (m_rteCamera != null)
            {
                m_rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }
        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void OnEnableOverride()
        {

        }

        protected virtual void OnDisableOverride()
        {

        }

        protected virtual void UpdateOverride()
        {
          
        }

        protected virtual void BeginRecordOverride()
        {

        }

        protected virtual void EndRecordOverride()
        {

        }

        protected override void OnActiveWindowChanged(RuntimeWindow deactivatedWindow)
        {
            if (Editor.ActiveWindow != null && Editor.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                Window = Editor.ActiveWindow;
                SceneCamera = Window.Camera;
            }

            base.OnActiveWindowChanged(deactivatedWindow);
        }

        protected virtual bool OnBeginDrag(int index)
        {
            return true;
        }

        protected virtual bool OnDrag(int index, Vector3 offset)
        {
            return true;
        }

        protected virtual void OnDrop()
        {

        }

        protected virtual void OnCommandBufferRefresh(IRTECamera camera)
        {
            HandleProperties.SetColor("_Color", HandlesColor);
            LineProperties.SetColor("_Color", LineColor);
            SelectionProperties.SetColor("_Color", SelectionColor);
        }

        protected virtual bool HitOverride(int index, Vector3 vertex, Vector3 normal)
        {
            return true;
        }

        private int Hit(Vector2 pointer, Vector3[] vertices, Vector3[] normals)
        {
            float minMag = float.MaxValue;
            int index = -1;
            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 normal = normals[i];
                normal = HandlesTransform.MultiplyVector(normal);
                Vector3 vertex = vertices[i];
                Vector3 vertexWorld = HandlesTransform.MultiplyPoint(vertices[i]);

                if (Mathf.Abs(Vector3.Dot((SceneCamera.transform.position - vertexWorld).normalized, normal.normalized)) > 0.999f)
                {
                    continue;
                }

                if (!HitOverride(i, vertex, normal))
                {
                    continue;
                }

                Vector2 vertexScreen = SceneCamera.WorldToScreenPoint(vertexWorld);
                float distance = (vertexScreen - pointer).magnitude;
                if(distance < minMag && distance <= SelectionMargin)
                {
                    minMag = distance;
                    index = i;
                }
            }

            return index;
        }

        protected Plane GetDragPlane()
        {
            Vector3 toCam = SceneCamera.transform.position - HandlesTransform.MultiplyPoint(HandlesPositions[m_dragIndex]); // SceneCamera.cameraToWorldMatrix.MultiplyVector(Vector3.forward); 
            Vector3 dragPlaneVector = toCam.normalized;

            Vector3 position = m_handlesTransform.MultiplyPoint(Vector3.zero); 
           
            Plane dragPlane = new Plane(dragPlaneVector, position);
            return dragPlane;
        }

        protected bool GetPointOnDragPlane(Vector3 screenPos, out Vector3 point)
        {
            return GetPointOnDragPlane(m_dragPlane, screenPos, out point);
        }

        protected bool GetPointOnDragPlane(Plane dragPlane, Vector3 screenPos, out Vector3 point)
        {
            Ray ray = SceneCamera.ScreenPointToRay(screenPos);
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        public void BeginDrag()
        {
            if (!IsWindowActive)
            {
                return;
            }

            if (SceneCamera == null)
            {
                return;
            }

            if (Window.Editor.Tools.IsViewing)
            {
                return;
            }

            if (Window.Editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (Window.Camera != null && (!Window.IsPointerOver || Window.WindowType != RuntimeWindowType.Scene))
            {
                return;
            }

            Vector2 pointer = Window.Pointer.ScreenPoint;
            m_dragIndex = Hit(pointer, HandlesPositions, HandlesNormals);
            if (m_dragIndex >= 0 && OnBeginDrag(m_dragIndex))
            {
                m_handlesTransform = HandlesTransform;
                m_handlesInverseTransform = HandlesTransformInverse;
                m_dragPlane = GetDragPlane();
                m_isDragging = GetPointOnDragPlane(Window.Pointer.ScreenPoint, out m_prevPoint);
                m_normal = HandlesNormals[m_dragIndex].normalized;
                if (m_isDragging)
                {
                    Window.Editor.Tools.ActiveTool = this;
                }
                if (EnableUndo)
                {
                    BeginRecordOverride();
                }
            }
        }

        public void EndDrag()
        {
            if (m_isDragging)
            {
                OnDrop();
                bool isRecording = Window.Editor.Undo.IsRecording;
                if (!isRecording)
                {
                    Window.Editor.Undo.BeginRecord();
                }
                EndRecordOverride();
                if (!isRecording)
                {
                    Window.Editor.Undo.EndRecord();
                }
                m_isDragging = false;
                Window.Editor.Tools.ActiveTool = null;
                m_rteCamera.RefreshCommandBuffer();
            }
        }

    }
}


