using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CTO.Price.Shared.Extensions
{
    public static class JsonExtensions
    {
        public static readonly JsonSerializerSettings DefaultJsonSerializerSettings = 
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

        /// <summary>Converts given object to JSON string.</summary>
        /// <returns></returns>
        public static string ToJsonString(this object obj)
        {
            return obj.ToJsonString(DefaultJsonSerializerSettings);
        }

        /// <summary>
        /// Converts given object to JSON string using custom <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
        /// </summary>
        /// <returns></returns>
        public static string ToJsonString(this object obj, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        /// <summary>
        /// Returns deserialized string using default <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T FromJsonString<T>(this string value)
        {
            return value.FromJsonString<T>(new JsonSerializerSettings());
        }

        /// <summary>
        /// Returns deserialized string using custom <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T FromJsonString<T>(this string value, JsonSerializerSettings settings)
        {
#pragma warning disable 8603
            return JsonConvert.DeserializeObject<T>(value, settings);
#pragma warning restore 8603
        }
    }
}