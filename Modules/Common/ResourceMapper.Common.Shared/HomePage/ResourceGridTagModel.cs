using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Shared.HomePage
{
    public class ResourceGridTagModel
    {
        public string TagUid { get; set; }

        /// <summary>Internal TagDefinitionKey. Identity only — filters and URL tokens key off it.</summary>
        public string TagKey { get; set; }

        /// <summary>
        /// User-facing label: the definition's DisplayName, falling back to <see cref="TagKey"/>.
        /// Everything the user reads must use this (PL-48) — the key is internal vocabulary.
        /// </summary>
        public string TagDisplayName { get; set; }

        public string ContentType { get; set; }
        public string TagValue { get; set; }
    }
}
