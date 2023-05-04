using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAudioLibraryEditorProperty
{
	public bool IsShowClipPreview { get; set; }
	public MulticlipsPlayMode MulticlipsPlayMode { get; set; }
}