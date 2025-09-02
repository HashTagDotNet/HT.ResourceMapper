using Microsoft.Extensions.Configuration;

namespace HT.Msft.IConfiguration.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class IConfigurationExtensions
    {
        #region Existing String Methods
        
        public static string? GetString(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, bool isRequired = true)
        {
            var value = configuration[key];
            if (isRequired && string.IsNullOrWhiteSpace(value))
            {
                throw new KeyNotFoundException($"Configuration value for key '{key}' is required.");
            }
            return value;
        }

        public static string GetString(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, string defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }
            return value;
        }

        #endregion

        #region Integer Methods

        /// <summary>
        /// Gets an integer configuration value with optional requirement validation
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="isRequired">Whether the value is required (throws if missing)</param>
        /// <returns>The configuration value or null if not found and not required</returns>
        /// <exception cref="KeyNotFoundException">Thrown when required key is missing</exception>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as integer</exception>
        public static int? GetInt(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, bool isRequired = true)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                if (isRequired)
                {
                    throw new KeyNotFoundException($"Configuration value for key '{key}' is required.");
                }
                return null;
            }

            if (!int.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid integer. Got: '{value}'");
            }

            return result;
        }

        /// <summary>
        /// Gets an integer configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as integer</exception>
        public static int GetInt(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, int defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!int.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid integer. Got: '{value}'");
            }

            return result;
        }

        #endregion

        #region Boolean Methods

        /// <summary>
        /// Gets a boolean configuration value with optional requirement validation
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="isRequired">Whether the value is required (throws if missing)</param>
        /// <returns>The configuration value or null if not found and not required</returns>
        /// <exception cref="KeyNotFoundException">Thrown when required key is missing</exception>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as boolean</exception>
        public static bool? GetBool(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, bool isRequired = true)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                if (isRequired)
                {
                    throw new KeyNotFoundException($"Configuration value for key '{key}' is required.");
                }
                return null;
            }

            if (!bool.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid boolean. Got: '{value}'");
            }

            return result;
        }

        /// <summary>
        /// Gets a boolean configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as boolean</exception>
        public static bool GetBoolOrDefault(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, bool defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!bool.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid boolean. Got: '{value}'");
            }

            return result;
        }

        #endregion

        #region Double/Decimal Methods

        /// <summary>
        /// Gets a double configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as double</exception>
        public static double GetDouble(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, double defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!double.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid double. Got: '{value}'");
            }

            return result;
        }

        /// <summary>
        /// Gets a decimal configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as decimal</exception>
        public static decimal GetDecimal(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, decimal defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!decimal.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid decimal. Got: '{value}'");
            }

            return result;
        }

        #endregion

        #region TimeSpan Methods

        /// <summary>
        /// Gets a TimeSpan configuration value from various string formats
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as TimeSpan</exception>
        public static TimeSpan GetTimeSpan(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, TimeSpan defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!TimeSpan.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid TimeSpan. Got: '{value}'");
            }

            return result;
        }

        /// <summary>
        /// Gets a TimeSpan configuration value in seconds
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultSeconds">The default value in seconds to return if key is missing</param>
        /// <returns>The configuration value as TimeSpan or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as integer</exception>
        public static TimeSpan GetTimeSpanFromSeconds(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, int defaultSeconds)
        {
            var seconds = configuration.GetInt(key, defaultSeconds);
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Gets a TimeSpan configuration value in milliseconds
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultMilliseconds">The default value in milliseconds to return if key is missing</param>
        /// <returns>The configuration value as TimeSpan or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as integer</exception>
        public static TimeSpan GetTimeSpanFromMilliseconds(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, int defaultMilliseconds)
        {
            var milliseconds = configuration.GetInt(key, defaultMilliseconds);
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        #endregion

        #region DateTime Methods

        /// <summary>
        /// Gets a DateTime configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as DateTime</exception>
        public static DateTime GetDateTime(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, DateTime defaultValue)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!DateTime.TryParse(value, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid DateTime. Got: '{value}'");
            }

            return result;
        }

        #endregion

        #region Connection String Methods

        /// <summary>
        /// Gets a connection string from the ConnectionStrings section
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="name">The connection string name</param>
        /// <param name="isRequired">Whether the connection string is required (throws if missing)</param>
        /// <returns>The connection string or null if not found and not required</returns>
        /// <exception cref="KeyNotFoundException">Thrown when required connection string is missing</exception>
        public static string? GetConnectionString(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string name, bool isRequired = true)
        {
            var connectionString = configuration.GetConnectionString(name);
            if (isRequired && string.IsNullOrWhiteSpace(connectionString))
            {
                throw new KeyNotFoundException($"Connection string '{name}' is required but not found in configuration.");
            }
            return connectionString;
        }

        #endregion

        #region Strongly-Typed Section Methods

        /// <summary>
        /// Gets a strongly-typed configuration section
        /// </summary>
        /// <typeparam name="T">The type to bind the configuration section to</typeparam>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="sectionName">The section name</param>
        /// <param name="isRequired">Whether the section is required (throws if missing)</param>
        /// <returns>The bound configuration object or null if not found and not required</returns>
        /// <exception cref="KeyNotFoundException">Thrown when required section is missing</exception>
        public static T? GetTypedSection<T>(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string sectionName, bool isRequired = true) where T : class, new()
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                if (isRequired)
                {
                    throw new KeyNotFoundException($"Configuration section '{sectionName}' is required but not found.");
                }
                return null;
            }

            var result = new T();
            global::Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(section, result);
            return result;
        }

        /// <summary>
        /// Gets a strongly-typed configuration section with default values
        /// </summary>
        /// <typeparam name="T">The type to bind the configuration section to</typeparam>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="sectionName">The section name</param>
        /// <param name="defaultValue">The default instance to return if section is missing</param>
        /// <returns>The bound configuration object or default value</returns>
        public static T GetTypedSection<T>(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string sectionName, T defaultValue) where T : class, new()
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                return defaultValue;
            }

            var result = new T();
            global::Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(section, result);
            return result;
        }

        #endregion

        #region Enum Methods

        /// <summary>
        /// Gets an enum configuration value with a default fallback
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value to return if key is missing</param>
        /// <param name="ignoreCase">Whether to ignore case when parsing</param>
        /// <returns>The configuration value or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as the specified enum</exception>
        public static TEnum GetEnum<TEnum>(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, TEnum defaultValue, bool ignoreCase = true) where TEnum : struct, Enum
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!Enum.TryParse<TEnum>(value, ignoreCase, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid {typeof(TEnum).Name}. Got: '{value}'. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}");
            }

            return result;
        }

        #endregion

        #region Array/List Methods

        /// <summary>
        /// Gets a string array configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="separator">The separator character(s) to split on</param>
        /// <param name="defaultValue">The default array to return if key is missing</param>
        /// <returns>The configuration value as array or default value</returns>
        public static string[] GetStringArray(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, string separator = ",", string[]? defaultValue = null)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue ?? Array.Empty<string>();
            }

            return value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Gets an integer array configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="separator">The separator character(s) to split on</param>
        /// <param name="defaultValue">The default array to return if key is missing</param>
        /// <returns>The configuration value as array or default value</returns>
        /// <exception cref="FormatException">Thrown when any value cannot be parsed as integer</exception>
        public static int[] GetIntArray(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, string separator = ",", int[]? defaultValue = null)
        {
            var stringArray = configuration.GetStringArray(key, separator, null);
            if (stringArray.Length == 0)
            {
                return defaultValue ?? Array.Empty<int>();
            }

            var result = new int[stringArray.Length];
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (!int.TryParse(stringArray[i], out result[i]))
                {
                    throw new FormatException($"Configuration value for key '{key}' contains invalid integer: '{stringArray[i]}'");
                }
            }

            return result;
        }

        #endregion

        #region URL/URI Methods

        /// <summary>
        /// Gets a URI configuration value with a default fallback
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default URI to return if key is missing</param>
        /// <returns>The configuration value as URI or default value</returns>
        /// <exception cref="FormatException">Thrown when value cannot be parsed as URI</exception>
        public static Uri? GetUri(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, Uri? defaultValue = null)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var result))
            {
                throw new FormatException($"Configuration value for key '{key}' must be a valid URI. Got: '{value}'");
            }

            return result;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates that all required configuration keys are present
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="requiredKeys">Array of required configuration keys</param>
        /// <exception cref="ArgumentException">Thrown when one or more required keys are missing</exception>
        public static void ValidateRequiredKeys(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, params string[] requiredKeys)
        {
            var missingKeys = new List<string>();
            
            foreach (var key in requiredKeys)
            {
                var value = configuration[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingKeys.Add(key);
                }
            }

            if (missingKeys.Count > 0)
            {
                throw new ArgumentException($"Missing required configuration keys: {string.Join(", ", missingKeys)}");
            }
        }

        /// <summary>
        /// Validates that all required connection strings are present
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="requiredConnectionStrings">Array of required connection string names</param>
        /// <exception cref="ArgumentException">Thrown when one or more required connection strings are missing</exception>
        public static void ValidateRequiredConnectionStrings(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, params string[] requiredConnectionStrings)
        {
            var missingConnectionStrings = new List<string>();
            
            foreach (var name in requiredConnectionStrings)
            {
                var connectionString = configuration.GetConnectionString(name);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    missingConnectionStrings.Add(name);
                }
            }

            if (missingConnectionStrings.Count > 0)
            {
                throw new ArgumentException($"Missing required connection strings: {string.Join(", ", missingConnectionStrings)}");
            }
        }

        #endregion

        #region Environment-Specific Methods

        /// <summary>
        /// Gets the current environment name
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="defaultEnvironment">The default environment name if not found</param>
        /// <returns>The environment name</returns>
        public static string GetEnvironmentName(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string defaultEnvironment = "Production")
        {
            return configuration.GetString("ASPNETCORE_ENVIRONMENT", false) 
                ?? configuration.GetString("DOTNET_ENVIRONMENT", false) 
                ?? configuration.GetString("ENVIRONMENT", false) 
                ?? defaultEnvironment;
        }

        /// <summary>
        /// Checks if the current environment matches the specified name
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="environmentName">The environment name to check</param>
        /// <param name="ignoreCase">Whether to ignore case when comparing</param>
        /// <returns>True if the environment matches, false otherwise</returns>
        public static bool IsEnvironment(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string environmentName, bool ignoreCase = true)
        {
            var currentEnvironment = configuration.GetEnvironmentName();
            return string.Equals(currentEnvironment, environmentName, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the current environment is Development
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <returns>True if in Development environment, false otherwise</returns>
        public static bool IsDevelopment(this global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return configuration.IsEnvironment("Development");
        }

        /// <summary>
        /// Checks if the current environment is Production
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <returns>True if in Production environment, false otherwise</returns>
        public static bool IsProduction(this global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return configuration.IsEnvironment("Production");
        }

        /// <summary>
        /// Checks if the current environment is Staging
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <returns>True if in Staging environment, false otherwise</returns>
        public static bool IsStaging(this global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            return configuration.IsEnvironment("Staging");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets all configuration keys that start with the specified prefix
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="prefix">The prefix to search for</param>
        /// <returns>Dictionary of keys and values that start with the prefix</returns>
        public static Dictionary<string, string?> GetByPrefix(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string prefix)
        {
            var result = new Dictionary<string, string?>();
            
            foreach (var child in configuration.AsEnumerable())
            {
                if (child.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result[child.Key] = child.Value;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Checks if a configuration key exists and has a non-empty value
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key to check</param>
        /// <returns>True if the key exists and has a value, false otherwise</returns>
        public static bool HasValue(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key)
        {
            var value = configuration[key];
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Gets a configuration value or throws a descriptive exception with suggestions
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="key">The configuration key</param>
        /// <param name="suggestions">Suggested keys if the requested key is not found</param>
        /// <returns>The configuration value</returns>
        /// <exception cref="KeyNotFoundException">Thrown when key is not found, includes suggestions</exception>
        public static string GetRequiredString(this global::Microsoft.Extensions.Configuration.IConfiguration configuration, string key, params string[] suggestions)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                var suggestionText = suggestions.Length > 0 ? $" Did you mean: {string.Join(", ", suggestions)}?" : "";
                throw new KeyNotFoundException($"Configuration value for key '{key}' is required but not found.{suggestionText}");
            }
            return value;
        }

        #endregion
    }
}
