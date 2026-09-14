// ReSharper disable InconsistentNaming

using Microsoft.Extensions.Configuration;
using ResourceMapper.Common.Server.Identity;

namespace ResourceMapper.Common.Server.Tests.Identity
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Identity")]
    [Trait("Category", "ResourceMapper/Common/Server/Identity/ConfiguredIdentity")]
    public class ConfiguredIdentityTests
    {
        [Fact]
        public void OwnerId_KeyConfigured_ReturnsConfiguredValue()
        {
            var sut = new ConfiguredIdentity(Config("steve"));

            sut.OwnerId.Should().Be("steve", "because a configured owner wins over the default");
        }

        [Fact]
        public void OwnerId_KeyMissing_ReturnsAnonymous()
        {
            var sut = new ConfiguredIdentity(Config(null));

            sut.OwnerId.Should().Be("anonymous",
                "because there is no identity provider yet and the fallback must still be a stable owner");
        }

        [Fact]
        public void OwnerId_KeyBlank_ReturnsAnonymous()
        {
            var sut = new ConfiguredIdentity(Config("   "));

            sut.OwnerId.Should().Be("anonymous", "because whitespace is not an owner");
        }

        [Fact]
        public void OwnerId_KeyPadded_IsTrimmed()
        {
            var sut = new ConfiguredIdentity(Config("  steve  "));

            sut.OwnerId.Should().Be("steve",
                "because the owner is a database key and stray whitespace would create a second owner");
        }

        #region helpers

        private static IConfiguration Config(string? ownerId)
        {
            var config = new Mock<IConfiguration>();
            config.Setup(c => c[ConfiguredIdentity.ConfigKey]).Returns(ownerId);
            return config.Object;
        }

        #endregion
    }
}
