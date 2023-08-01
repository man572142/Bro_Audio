using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditorDrawLineCounter
{
    public float SingleLineSpace { get; }
    public int DrawLineCount { get; set;}
}
