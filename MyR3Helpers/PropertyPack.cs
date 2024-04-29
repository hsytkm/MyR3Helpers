namespace MyR3Helpers;

/// <summary>
/// Represents property and instance package.
/// </summary>
/// <typeparam name="TInstance">Type of instance</typeparam>
/// <typeparam name="TValue">Type of property value</typeparam>
public sealed record PropertyPack<TInstance, TValue>
{
    /// <summary>
    /// Gets instance which has property.
    /// </summary>
    public TInstance Instance { get; }

    /// <summary>
    /// Gets target property name.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets target property value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Create instance.
    /// </summary>
    /// <param name="instance">Target instance</param>
    /// <param name="property">Target property info</param>
    /// <param name="value">Property value</param>
    internal PropertyPack(TInstance instance, string propertyName, TValue value)
    {
        Instance = instance;
        PropertyName = propertyName;
        Value = value;
    }
}

/// <summary>
/// Provides PropertyPack static members.
/// </summary>
internal static class PropertyPack
{
    /// <summary>
    /// Create instance.
    /// </summary>
    /// <param name="instance">Target instance</param>
    /// <param name="propertyName">Target property name</param>
    /// <param name="value">Property value</param>
    /// <returns>Created instance</returns>
    public static PropertyPack<TInstance, TValue> Create<TInstance, TValue>(TInstance instance, string propertyName, TValue value) =>
        new(instance, propertyName, value);
}
