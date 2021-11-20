using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RaidBattlesBot.Model;

public static class EntityEx
{
  public static void SetNotNullProperties<TEntity>(this EntityEntry<TEntity> entry, [CanBeNull] TEntity entity)
    where TEntity : class
  {
    foreach (var entryProperty in entry.Properties)
    {
      var value = entryProperty.Metadata.PropertyInfo?.GetValue(entity);
      if (value != null)
      {
        entryProperty.CurrentValue = value;
      }
    }
  }
}