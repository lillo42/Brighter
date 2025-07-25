﻿using System;

namespace Paramore.Brighter.Transformers.JustSaying.Extensions;

internal static class RequestContextExtensions
{
    public static object? GetFromBag(this IRequestContext? context, string key, object? defaultValue = null)
    {
        if (context != null && context.Bag.TryGetValue(key, out var val))
        {
            return val;
        }

        return defaultValue;
    }
    
    
    public static T? GetFromBag<T>(this IRequestContext? context, string key, T? defaultValue = default)
    {
        if (context != null && context.Bag.TryGetValue(key, out var val) && val is T)
        {
            return (T)val;
        }

        return defaultValue;
    }

    public static Uri? GetUriFromBag(this IRequestContext? context, string key)
    {
        if (context == null || !context.Bag.TryGetValue(key, out var val))
        {
            return null;
        }
        
        if (val is Uri uri || val is string uriString && Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri!))
        {
            return uri;
        }

        return null;
    }

    public static Id? GetIdFromBag(this IRequestContext? context, string key, Id? defaultValue = null)
    {
        if (context == null || !context.Bag.TryGetValue(key, out var val))
        {
            return defaultValue;
        }

        return val switch
        {
            Id id => id,
            string valString when !string.IsNullOrEmpty(valString) => valString,
            _ => defaultValue
        };
    }
}
