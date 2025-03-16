namespace Ami.Extension
{
	public interface IRecyclable<T> where T : IRecyclable<T>
	{
        void Recycle();
	} 
}