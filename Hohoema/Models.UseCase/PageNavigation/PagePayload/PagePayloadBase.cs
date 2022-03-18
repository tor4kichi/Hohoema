﻿using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public abstract class PagePayloadBase : ObservableObject
	{
		public string ToParameterString()
		{
            return JsonSerializer.Serialize(this);
		}

		public static T FromParameterString<T>(string json)
		{
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException)
            {
                return default(T);
            }
        }
	}
}
