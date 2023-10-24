using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Ami.BroAudio.Data;
using Ami.Extension;
using System;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
	public class LibraryTreeView : TreeView
	{
        public enum HierarchyDepth
		{
            Asset = 0,
            Entity = 1,
            AudioClip = 2,
		}

		public event Action<AudioAssetEditor, string> OnRename;
		private Action<HierarchyDepth,SerializedProperty> _onSelectItem;

		private IReadOnlyDictionary<string, AudioAssetEditor> _assetEditorDict;

		public LibraryTreeView(TreeViewState state, IReadOnlyDictionary<string, AudioAssetEditor> assetEditorDict
		, Action<HierarchyDepth,SerializedProperty> onSelectItem) : base(state)
		{
			showBorder = true;
			extraSpaceBeforeIconAndLabel = EditorGUIUtility.singleLineHeight;
			_assetEditorDict = assetEditorDict;
			_onSelectItem = onSelectItem;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var rootItems = new List<TreeViewItem>();

			int assetTreeId = 0;
            foreach(var editor in _assetEditorDict.Values)
			{
                TreeViewItem assetItem = new TreeViewItem { id = assetTreeId, depth = (int)HierarchyDepth.Asset, displayName = editor.Asset.AssetName };
				SerializedProperty entitiesArrayProp = editor.serializedObject.FindProperty(nameof(AudioAsset.Entities));
                for(int i = 0; i < entitiesArrayProp.arraySize;i++)
                {
                    SerializedProperty entityProp = entitiesArrayProp.GetArrayElementAtIndex(i);
                    TreeViewItem entityItem = CreateEntityItem(entityProp);

                    SerializedProperty clipsArrayProp = entityProp.FindPropertyRelative(nameof(AudioEntity.Clips));
                    for (int clipIndex = 0; clipIndex < clipsArrayProp.arraySize; clipIndex++)
                    {
                        CreateClipItem(clipsArrayProp, clipIndex, clipItem => entityItem.AddChild(clipItem));
                    }
                    assetItem.AddChild(entityItem);
                }
                rootItems.Add(assetItem);
				assetTreeId++;
			}
            SetupParentsAndChildrenFromDepths(root, rootItems);

            return root;
        }

        private SerializedTreeViewItem CreateEntityItem(SerializedProperty entityProp)
        {
            int entityID = entityProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID))).intValue;
            string entityName = entityProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Name))).stringValue;
            var entityItem = new SerializedTreeViewItem { id = entityID, depth = (int)HierarchyDepth.Entity, displayName = entityName };
            entityItem.SerializedProperty = entityProp;
            return entityItem;
        }

        private void CreateClipItem(SerializedProperty clipsArrayProp, int index, Action<TreeViewItem> onCreateClipItem)
        {
            SerializedProperty clipProp = clipsArrayProp.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(BroAudioClip.AudioClip));
            if (clipProp.objectReferenceValue != null)
            {
                // hack: id with GetHashCode() might have collision?
                var clipItem = new SerializedTreeViewItem { id = clipProp.GetHashCode(), depth = (int)HierarchyDepth.AudioClip, displayName = clipProp.objectReferenceValue.name };
                clipItem.SerializedProperty = clipProp;
				onCreateClipItem?.Invoke(clipItem);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
		{
            HierarchyDepth hierarchyDepth = (HierarchyDepth)args.item.depth;
			Rect iconRect = new Rect(args.rowRect);
			iconRect.width = iconRect.height;
			iconRect.x += (args.item.depth + 1) * iconRect.width;

			Texture icon = EditorGUIUtility.IconContent(GetIconName(hierarchyDepth)).image;

			if(Event.current.type == EventType.Repaint)
			{
				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
			}

			if(args.selected && args.focused && args.item is SerializedTreeViewItem item)
			{
				_onSelectItem?.Invoke(hierarchyDepth, item.SerializedProperty);
			}
			base.RowGUI(args);
		}

		private string GetIconName(HierarchyDepth hierarchyDepth)
		{
			switch (hierarchyDepth)
			{
				case HierarchyDepth.Asset:
					return IconConstant.ScriptableObject;
				case HierarchyDepth.Entity:
					return IconConstant.AudioMixer;
				case HierarchyDepth.AudioClip:
					return IconConstant.AudioClip;
			}
			return null;
		}

		protected override bool CanRename(TreeViewItem item)
		{
			return true;
		}

		protected override void RenameEnded(RenameEndedArgs args)
		{
			Debug.Log(args.newName);
			base.RenameEnded(args);
		}
	}
}
