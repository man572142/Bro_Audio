using System.Reflection;

namespace Ami.BroAudio.Tests.Fixtures
{
    /// <summary>
    /// Production data types (AudioEntity, DefaultPlaybackGroup, BroAudioClip, ...) expose most of their
    /// configuration as `[field: SerializeField] { get; private set; }` or private fields, since they're
    /// normally authored through the Library Manager, not code. Reflection lets fixtures configure them
    /// without adding test-only setters to shipped production types.
    /// </summary>
    internal static class ReflectionUtility
    {
        private const BindingFlags InstanceAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceAll);
            field.SetValue(target, value);
        }

        internal static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceAll);
            property.SetValue(target, value);
        }
    }
}
