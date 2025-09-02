using Microsoft.Extensions.Primitives;

namespace HT.Collections
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Merge one or more dictionaries into <paramref name="targetCollection"/> updating any existing values.  If <paramref name="targetCollection"/> is null return null.  If <paramref name="sourceCollections"/> is null return <paramref name="targetCollection"/>
        /// </summary>
        /// <param name="targetCollection">Collection that will have it's members added or updated</param>
        /// <param name="sourceCollections">Collections whose key-values will be added or updated in <paramref name="targetCollection"/></param>
        /// <returns></returns>
        public static IDictionary<string, object> Merge(this IDictionary<string, object> targetCollection, params IDictionary<string, object>[] sourceCollections)
        {
            if (targetCollection == null) return null;
            if (sourceCollections == null || sourceCollections.Length == 0) return targetCollection;
            foreach (var sourceCollection in sourceCollections)
            {
                if (sourceCollection == null) continue;
                foreach (var sourceItem in sourceCollection)
                {
                    if (targetCollection.ContainsKey(sourceItem.Key))
                    {
                        targetCollection[sourceItem.Key] = sourceItem.Value;
                    }
                    else
                    {
                        targetCollection.Add(sourceItem.Key, sourceItem.Value);
                    }
                }
            }
            return targetCollection;
        }

        /// <summary>
        /// Merge one or more dictionaries into <paramref name="targetCollection"/> updating any existing values.  If <paramref name="targetCollection"/> is null return null.  If <paramref name="sourceCollections"/> is null return <paramref name="targetCollection"/>
        /// </summary>
        /// <param name="targetCollection">Collection that will have it's members added or updated</param>
        /// <param name="sourceCollections">Collections whose key-values will be added to <paramref name="targetCollection"/></param>
        /// <returns></returns>
        public static IDictionary<string, string> Merge(this IDictionary<string, string> targetCollection, params IDictionary<string, string>[] sourceCollections)
        {
            if (targetCollection == null) return null;
            if (sourceCollections == null || sourceCollections.Length == 0) return targetCollection;
            foreach (var sourceCollection in sourceCollections)
            {
                if (sourceCollection == null) continue;
                foreach (var sourceItem in sourceCollection)
                {
                    if (targetCollection.ContainsKey(sourceItem.Key))
                    {
                        targetCollection[sourceItem.Key] = sourceItem.Value;
                    }
                    else
                    {
                        targetCollection.Add(sourceItem.Key, sourceItem.Value);
                    }
                }
            }
            return targetCollection;
        }

        public static IDictionary<string, object> Upsert(this IDictionary<string, object> targetDictionary, string key, object value)
        {
            if (targetDictionary == null && string.IsNullOrWhiteSpace(key)) return null;
            if (targetDictionary == null && !string.IsNullOrWhiteSpace(key))
            {
                targetDictionary = new Dictionary<string, object>();
            }

            if (targetDictionary.ContainsKey(key))
            {
                targetDictionary[key] = value;
            }
            else
            {
                targetDictionary.Add(key, value);
            }
            return targetDictionary;
        }
        public static IDictionary<string, object> AddIfNotExists(this IDictionary<string, object> targetDictionary, string key, object value)
        {
            return targetDictionary.Upsert(key, value);
        }
        /// <summary>
        /// Merge one or more dictionaries into <paramref name="targetCollection"/> updating any existing values.  If <paramref name="targetCollection"/> is null return null.  If <paramref name="sourceCollections"/> is null return <paramref name="targetCollection"/>
        /// </summary>
        /// <param name="targetCollection">Collection that will have it's members added or updated</param>
        /// <param name="sourceCollections">Collections whose key-values will be added to <paramref name="targetCollection"/></param>
        /// <returns></returns>
        public static IDictionary<string, string> Merge(this IDictionary<string, string> targetCollection, string prefix, params IDictionary<string, string>[] sourceCollections)
        {
            if (targetCollection == null) return null;
            if (sourceCollections == null || sourceCollections.Length == 0) return targetCollection;
            foreach (var sourceCollection in sourceCollections)
            {
                if (sourceCollection == null) continue;
                foreach (var sourceItem in sourceCollection)
                {
                    var key = $"{prefix}{sourceItem.Key}";
                    if (targetCollection.ContainsKey(key))
                    {
                        targetCollection[key] = sourceItem.Value;
                    }
                    else
                    {
                        targetCollection.Add(key, sourceItem.Value);
                    }
                }
            }
            return targetCollection;
        }

        public static Dictionary<string, StringValues> Merge(this Dictionary<string, StringValues> target, Dictionary<string, StringValues> source)
        {
            if (target == null)
            {
                target = new Dictionary<string, StringValues>();
            }
            if (source == null || source.Count == 0)
            {
                return target;
            }

            foreach (var sourceItem in source)
            {
                var key = sourceItem.Key;
                if (target.ContainsKey(key))
                {
                    StringValues targetItem = target[key];
                    targetItem = StringValues.Concat(targetItem, sourceItem.Value);
                }
                else
                {
                    var valueList = sourceItem.Value.ToArray();
                    target.Add(key, new StringValues(valueList));
                }
            }
            return target;
        }


        public static void SetValue(this IDictionary<string, string> dictionary, string key, string value)
        {
            var dictionaryKey = dictionary.Keys.FirstOrDefault(k => string.Compare(k, key, true) == 0);
            if (dictionaryKey == null)
            {
                dictionary.Add(key, value);
            }
            else
            {
                dictionary[dictionaryKey] = value;
            }

        }

        public static string GetValue(this IDictionary<string, string> dictionary, string key)
        {
            if (dictionary == null) return null;
            if (key == null) return null;
            try
            {
                if (dictionary.ContainsKey(key))
                {
                    return dictionary[key];
                }
                return null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public static int FindIndex<T>(this IList<T> source, int startIndex,
                               Predicate<T> match)
        {
            if (source == null || source.Count == 0) return -1;
            for (int i = startIndex; i < source.Count; i++)
            {
                if (match(source[i]))
                {
                    return i;
                }
            }
            return -1;
        }
        public static int FindIndex<T>(this IList<T> source,
                              Predicate<T> match)
        {
            if (source == null || source.Count == 0) return -1;
            return source.FindIndex(0, match);
        }

        /// <summary>
        /// Inserts an item after the provided index.  If <paramref name="index"/> is greater than elements in <paramref name="source"/>, then <paramref name="value"/> will be appended to collection
        /// </summary>
        /// <typeparam name="T">Type of element in list</typeparam>
        /// <param name="source">List of elements into which target will be inserted</param>
        /// <param name="index">Pivot value immediately after which target will be inserted. -1 means to add to beginning of collection</param>
        /// <param name="value">Target value to insert into collection</param>
        /// <returns>Index of value in collection after it's been inserted</returns>
        public static int InsertAfterIndex<T>(this IList<T> source,
                             int index, T value)
        {

            if (source == null)
            {
                throw new ArgumentNullException($"{nameof(source)} is null.  Collection modification operations are now allowed on null reference");
            }

            // add to empty collection
            if (source.Count == 0 && index < 0)
            {
                source.Add(value);
                return source.Count - 1;
            }

            // insert into beginning of collection
            if (index < 0)
            {
                source.Insert(0, value);
                return 0;
            }

            // index exceeds count so just append
            if (index >= source.Count - 1)
            {
                source.Add(value);
                return source.Count - 1;
            }

            source.Insert(index + 1, value);
            return index + 1;
        }

        /// <summary>
        /// Resolved value{$...} tokens, first in <paramref name="source"/> then in <paramref name="secondarylookupSources"/>; first found wins
        /// </summary>
        /// <param name="source"></param>
        /// <param name="secondarylookupSources"></param>
        public static void ResolveTokens(this IDictionary<string, string> source, params IDictionary<string, string>[] secondarylookupSources)
        {
            if (source == null || source.Count == 0) return; // nothing to do

            var replacementCount = 0;
            do
            {
                replacementCount = 0;
                foreach (var sourceKey in source.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value) && kvp.Value.Contains("{$")).Select(kvp => kvp.Key).ToList()) // select only keys that have '{$' in them
                {
                    var sourceValue = source[sourceKey];

                    if (sourceValue == null) continue;
                    string token = findTokenInString(sourceValue);
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    string replacementValue = findValueForToken(token, source, secondarylookupSources);
                    if (string.Compare(token, replacementValue, true) == 0) continue; //couldn't find value for token;

                    var newValue = sourceValue.Replace($"{{${token}}}", replacementValue);

                    source[sourceKey] = newValue;
                    replacementCount++;
                }
            } while (replacementCount > 0);
        }

        private static string findValueForToken(string token, IDictionary<string, string> source, IDictionary<string, string>[] secondarylookupSources)
        {
            foreach (var entry in source) //lookup token using case insenstive comparison, so we can't use TryGetValue()
            {
                if (string.Compare(entry.Key, token, true) == 0)
                {
                    return entry.Value;
                }
            }

            foreach (var lookupSource in secondarylookupSources) //lookup token using case insenstive comparison, so we can't use TryGetValue()
            {
                foreach (var entry in lookupSource)
                {
                    if (string.Compare(entry.Key, token, true) == 0)
                    {
                        return entry.Value;
                    }
                }
            }
            return token; // token not found so just return token
        }

        private static string findTokenInString(string sourceValue)
        {
            if (string.IsNullOrWhiteSpace(sourceValue)) return null;
            var startingTokenIndex = sourceValue.LastIndexOf("{$");
            if (startingTokenIndex == -1) return null;
            var endingTokenIndex = sourceValue.IndexOf('}', startingTokenIndex);
            if (endingTokenIndex == -1) return null; //no matching '}' found so ignore

            return sourceValue.Substring(startingTokenIndex + 2, endingTokenIndex - startingTokenIndex - 2);

        }
    }
}
