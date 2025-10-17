namespace Ami.BroAudio
{
	public enum Transition
	{
		/// <summary>
		/// Follow the clip's setting in LibraryManager.
		/// </summary>
		Default,

		/// <summary>
		/// Ignore the clip's setting in LibraryManger.Stop immediately and play immediately.
		/// </summary>
		Immediate,

		/// <summary>
		/// Stop immediately, and play with the clip's FadeIn setting in LibraryManger.
		/// </summary>
		OnlyFadeIn,

		/// <summary>
		/// Stop with the clip's FadeOut setting in LibraryManger, and play immediately.
		/// </summary>
		OnlyFadeOut,

		/// <summary>
		/// Stop previous and play new one at the same time , and do both fade in and fade out.   
		/// </summary>
		CrossFade
	} 
}