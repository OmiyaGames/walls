using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using OmiyaGames.Translations;

namespace OmiyaGames.UI.Translations
{
    ///-----------------------------------------------------------------------
    /// <copyright file="TranslationPreviewEditor.cs" company="Omiya Games">
    /// The MIT License (MIT)
    /// 
    /// Copyright (c) 2014-2018 Omiya Games
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy
    /// of this software and associated documentation files (the "Software"), to deal
    /// in the Software without restriction, including without limitation the rights
    /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    /// copies of the Software, and to permit persons to whom the Software is
    /// furnished to do so, subject to the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included in
    /// all copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    /// THE SOFTWARE.
    /// </copyright>
    /// <author>Taro Omiya</author>
    /// <date>10/9/2018</date>
    ///-----------------------------------------------------------------------
    /// <summary>
    /// Helper editor class to display preview in <code>TranslatedStringEditor</code>.
    /// </summary>
    /// <seealso cref="TranslatedStringEditor"/>
    public class TranslationPreviewEditor
    {
        protected const float VerticalSpace = 4;
        protected const float ExpandLength = 75f;
        protected const float WordWrapLength = 95f;
        protected const bool WordWrapEnabledDefault = false;

        protected readonly string keyLabel;
        protected readonly string valueLabel;
        protected readonly GUIStyle wrappedTextArea = new GUIStyle(EditorStyles.textArea);
        SerializedProperty element;
        float width;
        int languageIndex = 0;
        string text = null;

        public TranslationPreviewEditor(SupportedLanguages supportedLanguages) : this(supportedLanguages, "Language", "Text") { }

        protected TranslationPreviewEditor(SupportedLanguages supportedLanguages, string keyLabel, string valueLabel)
        {
            // Setup member variables
            SupportedLanguages = supportedLanguages;
            this.keyLabel = keyLabel;
            this.valueLabel = valueLabel;
        }

        #region Properties
        public virtual bool SetMininmumHeightToPreview
        {
            get
            {
                return true;
            }
        }

        public virtual float ExpandTranslationsLeft
        {
            get
            {
                return 0f;
            }
        }

        public static float PreviewHeight
        {
            get
            {
                return EditorGUIUtility.singleLineHeight * 2 + VerticalSpace;
            }
        }

        public bool IsWordWrapEnabled
        {
            get;
            protected set;
        } = WordWrapEnabledDefault;

        public virtual bool IsExpanded
        {
            get;
            protected set;
        } = false;

        public virtual int LanguageIndex
        {
            get
            {
                return languageIndex;
            }
            set
            {
                if (languageIndex != value)
                {
                    languageIndex = value;
                    IsLanguageIndexChanged = true;
                }
            }
        }

        public virtual string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (text != value)
                {
                    text = value;
                    IsTextChanged = true;
                }
            }
        }

        public bool IsLanguageIndexChanged
        {
            get;
            protected set;
        }

        public bool IsTextChanged
        {
            get;
            protected set;
        }

        protected float Width
        {
            get
            {
                return width;
            }
            set
            {
                // Not sure why we need this, but it fixes a lot of problems.
                if (value > 0)
                {
                    width = value;
                }
            }
        }

        public SupportedLanguages SupportedLanguages
        {
            get;
            set;
        }
        #endregion

        public virtual float CalculateHeight(Dictionary<int, int> frequencyInLanguageAppearance, bool isTextBolded)
        {
            // Calculate the language height
            float height = EditorGUIUtility.singleLineHeight;

            // Add one for Translation label and toggles
            height += EditorGUIUtility.singleLineHeight;

            // If so, calculate the height of translations
            bool isExpandable;
            height += GetTextAreaHeight(Text, Width, IsExpanded, isTextBolded, out isExpandable);
            return height;
        }

        public virtual void DrawGui(Rect rect, bool isTextBolded)
        {
            // Draw the key field
            DrawKeyField(ref rect);

            // Draw the translation list
            DrawText(ref rect, isTextBolded);
        }

        #region Helper Methods
        protected void DrawKeyField(ref Rect rect)
        {
            // Hold onto the original rect position
            float originalX = rect.x;
            float originalWidth = rect.width;

            // Adjust the values 
            rect.height = EditorGUIUtility.singleLineHeight;

            // Draw the key text field
            IsLanguageIndexChanged = false;
            int oldLanguageIndex = LanguageIndex;
            LanguageIndex = SupportedLanguagesEditor.DrawSupportedLanguages(rect, keyLabel, LanguageIndex, SupportedLanguages, true);

            // Re-adjust the rectangle, full-width for the next part
            rect.x = originalX;
            rect.y += rect.height;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width = originalWidth;
        }

        protected virtual void DrawText(ref Rect rect, bool isTextBolded)
        {
            float originalX = rect.x;
            float originalWidth = rect.width;

            // Calculate the Expand toggle bound (to be used later)
            rect.x = (originalX + originalWidth);
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width = ExpandLength;
            rect.x -= rect.width;
            Rect expandToggleRect = new Rect(rect);

            // Draw the word-wrap toggle
            rect.width = WordWrapLength;
            rect.x -= rect.width;
            IsWordWrapEnabled = EditorGUI.ToggleLeft(rect, "Word Wrap", IsWordWrapEnabled);

            // Draw the label of the field
            rect.width = originalWidth - Mathf.Abs((originalX + originalWidth) - rect.x);
            rect.x = originalX - ExpandTranslationsLeft;
            GUIStyle labelStyle = EditorStyles.label;
            if (isTextBolded == true)
            {
                labelStyle = EditorStyles.boldLabel;
            }
            EditorGUI.LabelField(rect, valueLabel, labelStyle);

            // Offset the text area
            rect.x = originalX - ExpandTranslationsLeft;
            rect.width = originalWidth + ExpandTranslationsLeft;
            Width = rect.width;
            rect.y += rect.height;

            // Calculate range of warning
            string oldText = Text;
            bool isExpandable;
            rect.height = GetTextAreaHeight(oldText, Width, IsExpanded, isTextBolded, out isExpandable);

            // Draw the translations list
            IsTextChanged = false;
            Text = EditorGUI.TextArea(rect, oldText, GetTextAreaStyle(isTextBolded));

            // Draw the toggle, enabled only if the area is expandable
            GUI.enabled = isExpandable;
            IsExpanded = EditorGUI.ToggleLeft(expandToggleRect, "Expand", IsExpanded);
            GUI.enabled = true;

            // Adjust the rectangle
            rect.x = originalX;
            rect.width = originalWidth;
            rect.y += rect.height;
        }

        public string GetWarning(Dictionary<int, int> frequencyInLanguageAppearance)
        {
            // Check what warning to display
            string message = null;
            if (SupportedLanguages != null)
            {
                int langaugeIndex = LanguageIndex;
                if ((langaugeIndex < 0) || (langaugeIndex >= SupportedLanguages.NumberOfLanguages))
                {
                    message = "Language is not set to a valid value.";
                }
                else if ((frequencyInLanguageAppearance != null) && (frequencyInLanguageAppearance[langaugeIndex] > 1))
                {
                    message = "Multiple texts for the same language exists in this set.";
                }
            }
            return message;
        }

        protected float GetTextAreaHeight(string text, float viewWidth, bool isExpanded, bool isBolded, out bool isExpandable)
        {
            return GetTextAreaHeight(text, viewWidth, (isExpanded ? 1 : 0), isBolded, out isExpandable);
        }

        protected float GetTextAreaHeight(string text, float viewWidth, float fadeValue, bool isBolded, out bool isExpandable)
        {
            var content = new GUIContent(text);

            // Get the minimum and maximum measurement
            float min = PreviewHeight;
            float max = GetTextAreaStyle(isBolded).CalcHeight(content, viewWidth);
            if (max < min)
            {
                isExpandable = false;
                if (SetMininmumHeightToPreview == true)
                {
                    return min;
                }
                else
                {
                    return max;
                }
            }
            else
            {
                isExpandable = true;
                return Mathf.Lerp(min, max, fadeValue);
            }
        }

        private GUIStyle GetTextAreaStyle(bool isTextBolded)
        {
            wrappedTextArea.wordWrap = IsWordWrapEnabled;
            wrappedTextArea.fontStyle = FontStyle.Normal;
            if (isTextBolded == true)
            {
                wrappedTextArea.fontStyle = FontStyle.Bold;
            }
            return wrappedTextArea;
        }
        #endregion
    }
}
