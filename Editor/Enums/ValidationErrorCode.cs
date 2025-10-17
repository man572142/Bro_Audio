namespace Ami.BroAudio.Editor
{
	public enum ValidationErrorCode
	{
		NoError,
		IsNullOrEmpty,
		StartWithNumber,
		ContainsInvalidWord,
		ContainsWhiteSpace,
		IsDuplicate,
	}
}
