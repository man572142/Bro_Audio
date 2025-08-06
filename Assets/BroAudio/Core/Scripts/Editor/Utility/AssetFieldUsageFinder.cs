using System.Collections.Generic;
using Ami.BroAudio;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor;
using Ami.BroAudio.Tools;
using UnityEditor;

public class AssetFieldUsageFinder : FieldUsageFinder
{
    [MenuItem(BroName.MenuItem_BroAudio + "Others/Find SoundID Usage")]
    public static new void ShowWindow()
    {
        var window = GetWindow<AssetFieldUsageFinder>("SoundID Usage Finder");
    }

    private Dictionary<int, IEntityIdentity> _broAudioEntities = new Dictionary<int, IEntityIdentity>();

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadBroAudioEntities();

        // Automatically set the type to SoundID
        SetTargetType(typeof(SoundID));
    }


    private void LoadBroAudioEntities()
    {
        _broAudioEntities.Clear();

        if (BroEditorUtility.TryGetCoreData(out var data))
        {
            foreach (var asset in data.Assets)
            {
                if (asset == null)
                    continue;

                foreach(var identity in asset.GetAllAudioEntities())
                {
                    if (!identity.Validate())
                        continue;

                    if (!_broAudioEntities.ContainsKey(identity.ID))
                    {
                        _broAudioEntities.Add(identity.ID, identity);
                    }
                }
            }
        }
    }

    protected override string GetValueString(object value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is SoundID id && _broAudioEntities.TryGetValue(id, out var entity))
        {
            return $"{id.ID}, {entity.Name}";
        }
        return base.GetValueString(value);
    }
    
}