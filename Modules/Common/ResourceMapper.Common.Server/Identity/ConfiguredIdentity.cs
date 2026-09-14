using Microsoft.Extensions.Configuration;

namespace ResourceMapper.Common.Server.Identity
{
    /// <summary>
    /// Single-owner identity, read once from <c>ResourceMapper:Identity:OwnerId</c>. Falls back to
    /// <see cref="DefaultOwnerId"/> when the key is missing or blank, which is accurate: until an
    /// identity provider exists, every request really is the same person.
    /// </summary>
    public class ConfiguredIdentity : ICurrentIdentity
    {
        public const string ConfigKey = "ResourceMapper:Identity:OwnerId";
        public const string DefaultOwnerId = "anonymous";

        public ConfiguredIdentity(IConfiguration configuration)
        {
            // Read once: the value is configuration and cannot vary per request while there is a
            // single owner, which is also why this is registered as a singleton.
            var configured = configuration[ConfigKey];
            OwnerId = string.IsNullOrWhiteSpace(configured) ? DefaultOwnerId : configured.Trim();
        }

        public string OwnerId { get; }
    }
}
