﻿using Battlehub.RTCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class RuntimeAnimationClip : ScriptableObject
    {
        private readonly List<RuntimeAnimationProperty> m_properties;
        private AnimationClip m_clip;

        public AnimationClip Clip
        {
            get { return m_clip; }
        }

        public ICollection<RuntimeAnimationProperty> Properties
        {
            get { return m_properties; }
        }

        public void Add(RuntimeAnimationProperty property)
        {
            m_properties.Add(property);

            if(property.Children != null)
            {
                for(int i = 0; i < property.Children.Count; ++i)
                {
                    SetCurve(property.Children[i]);
                }
            }
            else
            {
                SetCurve(property);
            }
        }

        public void Remove(RuntimeAnimationProperty property)
        {
            m_properties.Remove(property);
            ClearCurve(property);
        }

        public void Clear()
        {
            if(m_properties == null)
            {
                return;
            }

            if(m_clip != null)
            {
                for (int i = 0; i < m_properties.Count; ++i)
                {
                    RuntimeAnimationProperty property = m_properties[i];
                    if (property != null)
                    {
                        ClearCurve(property);
                    }
                }
            }
     
            m_properties.Clear();
        }

        private void SetCurve(RuntimeAnimationProperty property)
        {
            Type componentType = property.ComponentType;
            if (componentType != null && property.Children == null)
            {
                SetCurveLinear(property.Curve);
                m_clip.SetCurve("", componentType, property.AnimationPropertyPath, property.Curve); 
            }
        }

        private static void SetCurveLinear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                float intangent = 0;
                float outtangent = 0;
                bool intangent_set = false;
                bool outtangent_set = false;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[i];

                if (i == 0)
                {
                    intangent = 0; intangent_set = true;
                }

                if (i == curve.keys.Length - 1)
                {
                    outtangent = 0; outtangent_set = true;
                }

                if (!intangent_set)
                {
                    point1.x = curve.keys[i - 1].time;
                    point1.y = curve.keys[i - 1].value;
                    point2.x = curve.keys[i].time;
                    point2.y = curve.keys[i].value;

                    deltapoint = point2 - point1;

                    intangent = deltapoint.y / deltapoint.x;
                }
                if (!outtangent_set)
                {
                    point1.x = curve.keys[i].time;
                    point1.y = curve.keys[i].value;
                    point2.x = curve.keys[i + 1].time;
                    point2.y = curve.keys[i + 1].value;

                    deltapoint = point2 - point1;

                    outtangent = deltapoint.y / deltapoint.x;
                }

                key.inTangent = intangent;
                key.outTangent = outtangent;
                curve.MoveKey(i, key);
            }
        }

        private void ClearCurve(RuntimeAnimationProperty property)
        {
            Type componentType = property.ComponentType;
            if (componentType != null && property.Parent == null)
            {
                m_clip.SetCurve("", componentType, property.AnimationPropertyPath, null);
            }
        }

        public RuntimeAnimationClip()
        {
            m_properties = new List<RuntimeAnimationProperty>();
        }

        private void OnEnable()
        {
            m_clip = new AnimationClip();
            m_clip.name = "AnimationClip";
            m_clip.legacy = true;
        }

        public void Refresh()
        {
            for (int i = 0; i < m_properties.Count; ++i)
            {
                RuntimeAnimationProperty property = m_properties[i];
                ClearCurve(property);

                if (property.Children != null)
                {
                    for (int j = 0; j < property.Children.Count; ++j)
                    {
                        SetCurve(property.Children[j]);
                    }
                }
                else
                {
                    SetCurve(property);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public class RuntimeAnimation : MonoBehaviour
    {
        public event Action ClipIndexChanged;
        public event Action ClipsChanged;

        private Animation m_animation;

        [SerializeField]
        private int m_clipIndex = -1;
        [SerializeField]
        private List<RuntimeAnimationClip> m_rtClips = new List<RuntimeAnimationClip>();

        public int ClipsCount
        {
            get { return m_rtClips.Count; }
        }

        public int ClipIndex
        {
            get { return m_clipIndex; }
            set
            {
                if (m_clipIndex != value)
                {
                    SetClipIndex(value);
                    if (ClipIndexChanged != null)
                    {
                        ClipIndexChanged();
                    }
                }
            }
        }

        private void SetClipIndex(int value)
        {
            if (m_clipIndex != value)
            {
                IsPlaying = false;
                m_clipIndex = value;
                Refresh();
            }
        }

        [SerializeField]
        private bool m_playOnAwake;
        public bool PlayOnAwake
        {
            get { return m_playOnAwake; }
            set { m_playOnAwake = value; }
        }

        [SerializeField]
        private bool m_loop;
        public bool Loop
        {
            get { return m_loop; }
            set
            {
                m_loop = value;
                if (m_animation != null && m_isPlaying)
                {
                    m_animation.wrapMode = m_loop ? WrapMode.Loop : WrapMode.ClampForever;
                }
            }
        }

        public List<RuntimeAnimationClip> Clips
        {
            get { return m_rtClips.ToList();  }
            set
            {
                if(value == null)
                {
                    if(m_rtClips != null)
                    {
                        for(int i = 0; i < m_rtClips.Count; ++i)
                        {
                            RuntimeAnimationClip clip = m_rtClips[i];
                            if(clip != null)
                            {
                                RemoveClip(clip);
                            }
                        }
                    }
                    SetClipIndex(-1);
                    m_rtClips = new List<RuntimeAnimationClip>();
                }
                else
                {
                    if(m_animation.clip != null)
                    {
                        m_animation.RemoveClip(m_animation.clip.name);
                    }
                    m_animation.clip = null;
                    m_rtClips = value.ToList();

                    if (m_rtClips.Count == 1)
                    {
                        ClipIndex = 0;
                    }

                    if (ClipIndex >= m_rtClips.Count)
                    {
                        SetClipIndex(m_rtClips.Count - 1);
                    }
                }

                if(ClipsChanged != null)
                {
                    ClipsChanged();
                }
            }
        }

        public AnimationState State
        {
            get
            {
                if(ClipIndex < 0)
                {
                    Debug.LogWarning("ClipIndex < 0");
                    return null;
                }

                RuntimeAnimationClip clip = m_rtClips[ClipIndex];
                if(clip == null)
                {
                    //Debug.LogWarning("Clip == null");
                    return null;
                }

                if(!m_animation.isPlaying)
                {
                   // Debug.LogWarning("!m_animation.IsPlaying");
                    return null;
                }

                AnimationState state = m_animation[clip.Clip.name];
                if(state == null)
                {
                    Debug.LogWarning("state == null");
                    return null;
                }

                return state;
            }
        }
        
        public bool IsInPreviewMode
        {
            get { return m_animation.isPlaying; }
            set
            {
                if (value)
                {
                    if(!m_animation.isPlaying)
                    {
                        Refresh();
                    }
                }
                else
                {
                    m_animation.Stop();
                }
            }
        }

        [SerializeField]
        private bool m_isPlaying;
        public bool IsPlaying
        {
            get { return m_isPlaying; }
            set
            {
                if(m_isPlaying != value)
                {
                    m_isPlaying = value;
                    if(m_isPlaying)
                    {
                        if(!m_animation.isPlaying)
                        {
                            Refresh();
                            if(!m_animation.isPlaying)
                            {
                                m_isPlaying = false;
                                return;
                            }
                            else
                            {
                                m_isPlaying = true;
                            }
                        }

                        AnimationState state = State;
                        if(state != null)
                        {
                            state.speed = 1;
                        }
                    }
                    else
                    {
                        AnimationState state = State;
                        if (state != null)
                        {
                            state.speed = 0;
                        }
                    }
                }
            }
        }

        public float NormalizedTime
        {
            get
            {
                AnimationState state = State;
                if(state != null)
                {
                    return state.normalizedTime;
                }
                return 0.0f;
            }
            set
            {
                if (!m_animation.isPlaying)
                {
                    Refresh();
                }

                AnimationState state = State;
                if (state != null)
                {
                    state.normalizedTime = value;
                }

            }
        }
        
        private void RemoveClip(RuntimeAnimationClip rtClip)
        {
            int index = m_rtClips.IndexOf(rtClip);
            if (index == ClipIndex)
            {
                m_animation.Stop();
                m_isPlaying = false;
            }

            if (index >= 0)
            {
                m_rtClips.RemoveAt(index);
                if(m_animation.clip == rtClip.Clip)
                {
                    m_animation.RemoveClip(m_animation.clip.name);
                    m_animation.clip = null;
                }
            }

            if (ClipIndex >= m_rtClips.Count)
            {
                ClipIndex = m_rtClips.Count - 1;
            }
        }

        private void Awake()
        {
            m_animation = GetComponent<Animation>();
            if (m_animation == null)
            {
                m_animation = gameObject.AddComponent<Animation>();
            }

            m_animation.playAutomatically = false;

            if(!m_runningInRuntimeEditor)
            {
                if (PlayOnAwake || IsPlaying)
                {
                    m_isPlaying = false;
                    IsPlaying = true;
                }
            }
        }

        private bool m_runningInRuntimeEditor;
        private void EditorAwake()
        {
            m_runningInRuntimeEditor = true;
        }

        private void OnDestroy()
        {
            if(m_animation != null)
            {
                Destroy(m_animation);
            }
        }

        public void Refresh()
        {
            if (ClipIndex >= 0)
            {
                RuntimeAnimationClip clip = m_rtClips[ClipIndex];
                if(clip == null)
                {
                    m_isPlaying = false;
                    m_animation.Stop();
                    return;
                }
                clip.Refresh();
                clip.Clip.wrapMode = WrapMode.ClampForever;
                

                float speed = 0;
                float normalizedTime = 0;
                AnimationState animationState;
                if (m_animation.isPlaying)
                {
                    animationState = m_animation[clip.Clip.name];
                    if (animationState != null)
                    {
                        speed = animationState.speed;
                        if(!m_isPlaying)
                        {
                            speed = 0;
                        }
                        normalizedTime = animationState.normalizedTime;
                    }
                }

                m_animation.Stop();
                if(m_animation.clip != null)
                {
                    m_animation.RemoveClip(m_animation.clip.name);
                }
                m_animation.clip = null;
                if (clip.Clip != null)
                {
                    m_animation.AddClip(clip.Clip, clip.Clip.name);
                }
                m_animation.clip = clip.Clip;
                m_animation.Play(clip.Clip.name);

                if(m_animation.isPlaying)
                {
                    m_isPlaying = !clip.Clip.empty && speed > 0;
                    animationState = m_animation[clip.Clip.name];
                    animationState.speed = speed;
                    if(float.IsInfinity(normalizedTime) || float.IsNaN(normalizedTime))
                    {
                        normalizedTime = 0;
                    }
                    animationState.normalizedTime = normalizedTime;
                }
                else
                {
                    m_isPlaying = false;
                }
            }
            else
            {
                m_isPlaying = false;
                m_animation.Stop();
            }

            m_animation.wrapMode = m_loop ? WrapMode.Loop : WrapMode.ClampForever;
        }

        public void SetClips(IList<RuntimeAnimationClip> clips, int currentClipIndex)
        {
            m_isPlaying = false;

            if (m_animation == null)
            {
                m_animation = gameObject.GetComponent<Animation>();
                if(m_animation == null)
                {
                    m_animation = gameObject.AddComponent<Animation>();
                }
                m_animation.playAutomatically = false;
                m_animation.Stop();
                if(m_animation.clip != null)
                {
                    m_animation.RemoveClip(m_animation.clip.name);
                }
                m_animation.clip = null;
            }
        
            m_rtClips.Clear();

            if(clips != null)
            {
                foreach (RuntimeAnimationClip clip in clips)
                {
                    m_rtClips.Add(clip);
                }
                m_clipIndex = currentClipIndex;
                RuntimeAnimationClip currentClip = m_rtClips[m_clipIndex];
                if(currentClip != null)
                {
                    if(currentClip.Clip != null)
                    {
                        m_animation.AddClip(currentClip.Clip, currentClip.Clip.name);
                    }
                    m_animation.clip = currentClip.Clip;
                }
            }
            else
            {
                m_clipIndex = -1;
            }
        }

        public void Sample()
        {
            m_animation.Sample();
        }
    }
}

