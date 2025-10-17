namespace Ami.Extension.Reflection
{
	[System.Flags]
	public enum ExposedParameterType
	{
		None = 0,
		Volume = 1 << 0,
		Pitch = 1 << 1,
		EffectSend = 1 << 2,

		All = Volume | Pitch | EffectSend,
	}

	public static class ExposedParameterTypeExtension
	{
		public static bool Contains(this ExposedParameterType type, ExposedParameterType targetType)
		{
			return ((int)type & (int)targetType) != 0;
		}
	}
}