using UnityEngine;

namespace Ami.Extension
{
	public class ListElementName : PropertyAttribute
	{
		public readonly string InspectorName = null;
		public readonly bool IsStringFormat = false;
		public readonly bool IsStartFromZero = false;
		public readonly bool IsUsingFirstPropertyValueAsName = false;

		public ListElementName()
		{
			IsUsingFirstPropertyValueAsName = true;
		}

		public ListElementName(string inspectorName, bool isStringFormat = false, bool indexStartFromZero = true)
		{
			InspectorName = inspectorName;
			IsStringFormat = isStringFormat;
			IsStartFromZero = indexStartFromZero;
			IsUsingFirstPropertyValueAsName = false;
		}
	}
}