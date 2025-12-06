using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class GUIUtilityx
{

    static FieldInfo getLastControlIdField;
    public static int GetLastControlId()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            if (getLastControlIdField == null)
                getLastControlIdField = typeof(UnityEditor.EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
            if (getLastControlIdField != null)
                return (int)getLastControlIdField.GetValue(null);
        }
#endif
        return 0;
    }
    public static string DelayedTextField(Rect rect, string value, GUIStyle style = null, Func<string, string> textField = null)
    {
        string current;
        return DelayedTextField(rect, value, out current, style: style, textField: textField);
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="value"></param>
    /// <param name="current">PlaceholderField 使用到</param>
    /// <param name="style"></param>
    /// <param name="textField">EditorGUI.TextField 输入没有延迟</param>
    /// <returns></returns>
    public static string DelayedTextField(Rect rect, string value, out string current, GUIStyle style = null, Func<string, string> textField = null, Action<bool> onEnd = null)
    {
        var evt = Event.current;
        int ctrlId = GUIUtility.GetControlID(FocusType.Keyboard, rect);
        var state = (DelayedTextFieldState)GUIUtility.GetStateObject(typeof(DelayedTextFieldState), ctrlId);

        if (style == null)
            style = "textfield";

        Action<bool> submit = (b) =>
        {
            state.isEditing = false;
            if (GUIUtility.keyboardControl == state.inputControlId)
                GUIUtility.keyboardControl = 0;
            if (b)
            {
                if (!string.Equals(value, state.value))
                {
                    value = state.value;
                    GUI.changed = true;
                }
            }
            onEnd?.Invoke(b);
        };
        if (state.isEditing)
        {

            if (evt.type == EventType.KeyDown)
            {

                if (evt.keyCode == KeyCode.Escape)
                {
                    evt.Use();
                    submit(false);
                }
                else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.Use();
                    submit(true);
                }
            }
            else if (evt.type == EventType.MouseDown)
            {
                if (!rect.Contains(evt.mousePosition))
                {
                    evt.Use();
                    submit(true);
                }
            }

            if (state.isEditing && GUIUtility.keyboardControl != state.inputControlId)
            {
                submit(true);
            }
        }


        if (!state.isEditing)
        {
            state.value = value;
        }
        bool changed = GUI.changed;
        if (textField != null)
            state.value = textField(state.value);
        else
            state.value = GUI.TextField(rect, state.value, style);
        //GUILayout.Label(ctrlId + ", " + GetLastControlId() + "");
        if (!state.isEditing && GUIUtility.keyboardControl > 0)
        {
            int textCtrlId = GetLastControlId();
            if ((textCtrlId != 0 && GUIUtility.keyboardControl == textCtrlId) || (GUIUtility.keyboardControl == ctrlId + 1 || GUIUtility.keyboardControl == ctrlId + 2))
            {
                state.isEditing = true;
                state.value = value;
                state.inputControlId = GUIUtility.keyboardControl;
            }
        }

        GUI.changed = changed;
        current = state.value;
        return value;
    }



    class DelayedTextFieldState
    {
        public int inputControlId;
        public string value;
        public bool isEditing;
    }


    public static string SearchTextField(string text, GUIContent placeholder, params GUILayoutOption[] options)
    {
        Rect rect;//= EditorGUILayout.GetControlRect(true, options);
        GUIStyle searchTextFieldStyle;
        searchTextFieldStyle = "SearchTextField";

        rect = GUILayoutUtility.GetRect(GUIContent.none, searchTextFieldStyle, options);
        return SearchTextField(rect, text, placeholder);
    }
    public static string SearchTextField(Rect rect, string text, GUIContent placeholder)
    {
        GUIStyle searchTextFieldStyle;
        GUIStyle searchCancelButtonStyle;
        GUIStyle searchCancelButtonEmptyStyle;

        searchTextFieldStyle = "SearchTextField";
        searchCancelButtonStyle = "SearchCancelButton";
        searchCancelButtonEmptyStyle = "SearchCancelButtonEmpty";



        bool isEmpty = string.IsNullOrEmpty(text);
        GUIStyle cancelButtonStyle = !isEmpty ? searchCancelButtonStyle : searchCancelButtonEmptyStyle;

        float cancelButtonWidth = 0f;
#if UNITY_2019_3_OR_NEWER
#else
            cancelButtonWidth = cancelButtonStyle.fixedWidth;
#endif


        Rect cancelButtonRect = new Rect(rect.xMax - cancelButtonStyle.fixedWidth, rect.y, cancelButtonStyle.fixedWidth, cancelButtonStyle.fixedHeight);
        if (GUI.Button(cancelButtonRect, GUIContent.none, GUIStyle.none) && !isEmpty)
        {
            text = string.Empty;
            GUI.changed = true;
            GUIUtility.keyboardControl = 0;
        }
        if (Event.current.type == EventType.MouseMove)
        {
            if (cancelButtonRect.Contains(Event.current.mousePosition))
            {
            }
        }

        text = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - cancelButtonWidth, rect.height), text, searchTextFieldStyle);

        if (Event.current.type == EventType.Repaint)
        {
            cancelButtonStyle.Draw(cancelButtonRect, GUIContent.none, true, false, false, false);
            if (isEmpty)
            {
                GUIStyle placeholderStyle = new GUIStyle("label");
                placeholderStyle.normal.textColor = Color.grey;
                placeholderStyle.fontSize = (int)searchTextFieldStyle.lineHeight - 2;
                placeholderStyle.clipping = TextClipping.Overflow;
                placeholderStyle.padding = new RectOffset();
                placeholderStyle.margin = new RectOffset();

#if UNITY_2019_3_OR_NEWER

                placeholderStyle.padding.top = searchTextFieldStyle.margin.top + searchTextFieldStyle.padding.top;

#else
                    
                        placeholderStyle.padding.top = searchTextFieldStyle.padding.top;
                    
#endif
                placeholderStyle.Draw(new Rect(rect.x + searchTextFieldStyle.padding.left, rect.y, rect.width - searchTextFieldStyle.padding.horizontal, rect.height), placeholder, false, false, false, false);
            }
        }



        return text;
    }


    public const int EditableLabelClickCount = 1;

    public static string DelayedEditableLabel(string text, params GUILayoutOption[] options)
    {
        return DelayedEditableLabel(text, "label", "textfield", clickCount: 2, options);
    }
    public static string DelayedEditableLabel(string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = EditableLabelClickCount, GUILayoutOption[] options = null)
    {
        if (labelStyle == null)
            labelStyle = "label";
        if (options == null)
            options = new GUILayoutOption[0];
        Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), labelStyle, options);
        return DelayedEditableLabel(rect, text, labelStyle: labelStyle, textStyle: textStyle, clickCount: clickCount);
    }
    public static string DelayedEditableLabel(Rect rect, string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = EditableLabelClickCount)
    {
        if (labelStyle == null)
            labelStyle = "label";
        if (textStyle == null)
            textStyle = "textfield";
        //DelayedTextField: 切换焦点时值错误
        return DelayedEditableLabel(rect, text, clickCount: clickCount, labelStyle: labelStyle, textStyle: textStyle,
            textField: (o) => EditorGUI.TextField(rect, o, textStyle), onStart: () =>
            {
                //EditorGUIUtility.editingTextField = false; 
                EditorGUIUtility.editingTextField = true;
            }, onEnd: (b) => EditorGUIUtility.editingTextField = false);
        ;
    }
    public static string DelayedEditableLabel(Rect rect, string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = EditableLabelClickCount, Func<string, string> textField = null, Action onStart = null, Action<bool> onEnd = null)
    {
        int ctrlId = GUIUtility.GetControlID(FocusType.Passive, rect);
        var state = (EditableLabelState)GUIUtility.GetStateObject(typeof(EditableLabelState), ctrlId);
        var evt = Event.current;

        if (state.isEditing)
        {
            if (textStyle == null)
                textStyle = "textfield";
            string current;

            text = DelayedTextField(rect, text, out current, textStyle, textField: textField, onEnd: (b) =>
            {
                state.isEditing = false;
                onEnd?.Invoke(b);
            });
            if (state.first)
            {
                state.first = false;
                GUIUtility.keyboardControl = GetLastControlId();
                onStart?.Invoke();
            }
        }
        else
        {
            if (labelStyle == null)
                labelStyle = "label";
            GUI.Label(rect, text, labelStyle);


            if (rect.Contains(evt.mousePosition))
            {
                if (evt.clickCount == clickCount)
                {
                    state.value = text;
                    state.isEditing = true;
                    state.first = true;
                    evt.Use();
                }
            }
        }

        //GUILayout.Label("" + ctrlId);
        return text;
    }

    class EditableLabelState
    {
        public bool isEditing;
        public string value;
        public bool first;
    }
    public static string DelayedPlaceholderField(string text, out string current, GUIContent placeholder, params GUILayoutOption[] options)
    {
        return DelayedPlaceholderField(text, out current, placeholder, textStyle: null, placeholderStyle: null, options);
    }
    public static string DelayedPlaceholderField(string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, GUILayoutOption[] options = null)
    {
        if (textStyle == null)
            textStyle = "textfield";
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, textStyle, options);
        return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle);
    }
    public static string DelayedPlaceholderField(Rect rect, string text, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null)
    {
        string current;
        return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle);
    }

    public static string DelayedPlaceholderField(Rect rect, string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null)
    {
        return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle,
               textField: o => EditorGUI.TextField(rect, o)); ;
    }
    public static string DelayedPlaceholderField(Rect rect, string text, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, Func<string, string> textField = null)
    {
        string current;
        return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle, textField: textField);
    }

    public static string DelayedPlaceholderField(Rect rect, string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, Func<string, string> textField = null)
    {
        if (textStyle == null)
            textStyle = "textfield";

        bool isEmpty = false;

        text = DelayedTextField(rect, text, out current, textStyle, textField: textField);
        isEmpty = string.IsNullOrEmpty(current);

        if (isEmpty)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (placeholderStyle == null)
                    placeholderStyle = Styles.Placeholder;
                placeholderStyle.Draw(rect, placeholder, false, false, false, false);
            }
        }
        return text;
    }
    public class Styles
    {

        static GUIStyle placeholder;
        public static GUIStyle Placeholder
        {
            get
            {
                if (placeholder == null)
                {
                    placeholder = new GUIStyle("label");
                    placeholder.normal.textColor = Color.grey;
                    placeholder.fontSize -= 1;
                    placeholder.padding.left++;
                    placeholder.padding.top++;
                }
                return placeholder;
            }
        }

        static GUIStyle ellipsis;
        public static GUIStyle Ellipsis
        {
            get
            {
                if (ellipsis == null)
                {
                    ellipsis = new GUIStyle("label");
                }
                return ellipsis;
            }
        }

        static GUIStyle toggleLabel;
        public static GUIStyle ToggleLabel
        {
            get
            {
                if (toggleLabel == null)
                {
                    toggleLabel = new GUIStyle("button");
                    toggleLabel.onNormal.background = toggleLabel.normal.background;
                    toggleLabel.stretchWidth = false;
                    toggleLabel.wordWrap = false;

                }
                return toggleLabel;
            }
        }
    }
    public class Scopes
    {


        public abstract class ValueScope<T> : GUI.Scope
        {
            private T originValue;
            public ValueScope(T value)
            {
                originValue = Value;
                Value = value;
            }
            protected abstract T Value { get; set; }

            protected override void CloseScope()
            {
                Value = originValue;
            }
        }

        public class ColorScope : ValueScope<Color>
        {
            public ColorScope(Color value)
                : base(value)
            { }

            protected override Color Value { get => GUI.color; set => GUI.color = value; }
        }
        public class ContentColorScope : ValueScope<Color>
        {
            public ContentColorScope(Color value)
                : base(value)
            { }

            protected override Color Value { get => GUI.contentColor; set => GUI.contentColor = value; }
        }
        public class BackgroundColorScope : ValueScope<Color>
        {
            public BackgroundColorScope(Color value)
                : base(value)
            { }

            protected override Color Value { get => GUI.backgroundColor; set => GUI.backgroundColor = value; }
        }

        /// <summary>
        /// 恢复之前的 changed
        /// </summary>
        public class ChangedScope : GUI.Scope
        {
            private bool oldChanged;
            private bool closed;
            public ChangedScope()
            {
                oldChanged = GUI.changed;
                GUI.changed = false;
            }

            /// <summary>
            /// CloseScope
            /// </summary>
            public bool changed
            {
                get
                {
                    CloseScope();
                    return GUI.changed;
                }
            }

            protected override void CloseScope()
            {
                if (closed)
                    return;
                closed = true;
                GUI.changed = oldChanged;
            }

        }


    }
}
