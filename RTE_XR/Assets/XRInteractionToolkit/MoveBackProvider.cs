using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Battlehub.XRInteractionToolkit
{
    public class MoveBackProvider : LocomotionProvider
    {
        [SerializeField]
        [Tooltip("The input usage that triggers a mobe")]
        InputHelpers.Button m_usage = InputHelpers.Button.PrimaryAxis2DDown;
        /// <summary>Gets or sets the usage to use for detecting selection.</summary>
        public InputHelpers.Button Usage { get { return m_usage; } set { m_usage = value; } }

        [SerializeField]
        [Tooltip("A list of controllers that allow Move Back.  If an XRController is not enabled, or does not have input actions enabled. Move Back will not work.")]
        List<XRController> m_controllers = new List<XRController>();

        /// <summary>
        /// The XRControllers that allow SnapTurn.  An XRController must be enabled in order to Move Back
        /// </summary>
        public List<XRController> controllers { get { return m_controllers; } set { m_controllers = value; } }

        [SerializeField]
        [Tooltip("The number of meters")]
        float m_moveAmount = 0.5f;
        /// <summary>
        /// The number of degrees clockwise to rotate when snap turning clockwise.
        /// </summary>
        public float moveAmount { get { return m_moveAmount; } set { m_moveAmount = value; } }

        [SerializeField]
        [Tooltip("The amount of time that the system will wait before starting another move.")]
        float m_debounceTime = 0.5f;
        /// <summary>
        /// The amount of time that the system will wait before starting another snap turn.
        /// </summary>
        public float debounceTime { get { return m_debounceTime; } set { m_debounceTime = value; } }

        [SerializeField]
        [Tooltip("The deadzone that the controller movement will have to be above to trigger a move back.")]
        float m_deadZone = 0.75f;
        /// <summary>
        /// The deadzone that the controller movement will have to be above to trigger a snap turn.
        /// </summary>
        public float deadZone { get { return m_deadZone; } set { m_deadZone = value; } }

        // state data
        float m_currentMoveAmount = 0.0f;
        float m_timeStarted = 0.0f;

        List<bool> m_controllersWereActive = new List<bool>();

        private void Update()
        {
            // wait for a certain amount of time before allowing another turn.
            if (m_timeStarted > 0.0f && (m_timeStarted + m_debounceTime < Time.time))
            {
                m_timeStarted = 0.0f;
                return;
            }

            if (m_controllers.Count > 0)
            {
                EnsureControllerDataListSize();

                for (int i = 0; i < m_controllers.Count; i++)
                {
                    XRController controller = m_controllers[i];
                    if (controller != null)
                    {
                        if (controller.enableInputActions && m_controllersWereActive[i])
                        {
                            InputDevice device = controller.inputDevice;

                            bool pressed = false;
                            device.IsPressed(m_usage, out pressed, m_deadZone);

                            if (pressed)
                            {
                                StartMove(m_moveAmount);
                            }
                        }
                        else //This adds a 1 frame delay when enabling input actions, so that the frame it's enabled doesn't trigger a snap turn.
                        {
                            m_controllersWereActive[i] = controller.enableInputActions;
                        }
                    }
                }
            }

            if (Math.Abs(m_currentMoveAmount) > 0.0f && BeginLocomotion())
            {
                var xrRig = system.xrRig;
                if (xrRig != null)
                {
                    xrRig.transform.position = xrRig.transform.position - xrRig.transform.forward * m_currentMoveAmount;
                }
                m_currentMoveAmount = 0.0f;
                EndLocomotion();
            }
        }

        void EnsureControllerDataListSize()
        {
            if (m_controllers.Count != m_controllersWereActive.Count)
            {
                while (m_controllersWereActive.Count < m_controllers.Count)
                {
                    m_controllersWereActive.Add(false);
                }

                while (m_controllersWereActive.Count < m_controllers.Count)
                {
                    m_controllersWereActive.RemoveAt(m_controllersWereActive.Count - 1);
                }
            }
        }

        private void StartMove(float amount)
        {
            if (m_timeStarted != 0.0f)
                return;

            if (!CanBeginLocomotion())
                return;

            m_timeStarted = Time.time;
            m_currentMoveAmount = amount;
        }
    }
}

