using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEntityIDContainer
{
	public int GetUniqueID(AudioType audioType);
	public bool RemoveID(AudioType audioType, int id);
}
