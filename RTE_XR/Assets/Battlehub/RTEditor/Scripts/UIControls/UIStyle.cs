using Battlehub.RTEditor;
using Battlehub.UIControls.MenuControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public class UIStyle : MonoBehaviour
    {
        public string Name;

        public void ApplyMainButtonColor(Color normal, Color pointerOver, Color focused)
        {
            MainMenuButton mainMenuButton = GetComponent<MainMenuButton>();
            if (mainMenuButton != null)
            {
                mainMenuButton.NormalColor = normal;
                mainMenuButton.PointerOverColor = pointerOver;
                mainMenuButton.FocusedColor = focused;
            }
        }

        public void ApplyMenuItemColor(Color selectionColor, Color textColor, Color disabledSelectionColor, Color disabledTextColor)
        {
            MenuItem menuItem = GetComponent<MenuItem>();
            if (menuItem != null)
            {
                menuItem.SelectionColor = selectionColor;
                menuItem.TextColor = textColor;
                menuItem.DisabledSelectionColor = disabledSelectionColor;
                menuItem.DisableTextColor = disabledTextColor;
            }
        }

        public void ApplyImageColor(Color color)
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        public void ApplyOutlineColor(Color color)
        {
            Outline outline = GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = color;
            }
        }

        public void ApplyTextColor(Color color)
        {
            TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = color;
            }
            else
            {
                Text uitext = GetComponent<Text>();
                if (uitext != null)
                {
                    uitext.color = color;
                }
            }
        }

        public void ApplyInputFieldColor(Color normalColor, Color highlighedColor, Color pressedColor, Color disabledColor, Color selectedColor)
        {
            TMP_InputField inputField = GetComponent<TMP_InputField>();
            if (inputField != null)
            {
                ColorBlock colors = inputField.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                colors.selectedColor = highlighedColor;
                inputField.colors = colors;
                inputField.selectionColor = selectedColor;
            }
            else
            {
                InputField uiInputField = GetComponent<InputField>();

                ColorBlock colors = uiInputField.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                colors.selectedColor = highlighedColor;
                uiInputField.colors = colors;
                uiInputField.selectionColor = selectedColor;
            }
        }

        public void ApplySelectableColor(Color normalColor, Color highlighedColor, Color pressedColor, Color disabledColor, Color selectedColor)
        {
            Selectable selectable = GetComponent<Selectable>();
            if (selectable != null)
            {
                ColorBlock colors = selectable.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                colors.selectedColor = highlighedColor;
                selectable.colors = colors;

            }
        }

        public void ApplyTimelineControlBackgroundColor(Color background)
        {
            TimelineControl timelineControl = GetComponent<TimelineControl>();
            if(timelineControl != null)
            {
                timelineControl.BackgroundColor = background;
            }
        }

        public void ApplyHierarchyColors(Color enabledItem, Color disabledItem)
        {
            HierarchyView hierarchy = GetComponent<HierarchyView>();
            if (hierarchy != null)
            {
                hierarchy.EnabledItemColor = enabledItem;
                hierarchy.DisabledItemColor = disabledItem;
            }
        }

        public void ApplyToolCmdItemColor(Color normalColor, Color pointerOverColor, Color pressedColor)
        {
            ToolCmdItem cmdItem = GetComponent<ToolCmdItem>();
            if(cmdItem != null)
            {
                cmdItem.NormalColor = normalColor;
                cmdItem.PointerOverColor = pointerOverColor;
                cmdItem.PressedColor = pressedColor;
            }
        }
    }
}