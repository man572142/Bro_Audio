using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

public class SerializedTreeViewItem : TreeViewItem
{
    public SerializedProperty SerializedProperty { get; set; }
}
