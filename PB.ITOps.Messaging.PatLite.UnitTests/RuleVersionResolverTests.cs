using PB.ITOps.Messaging.PatLite.SubscriberRules;
using System;
using System.Linq;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests
{
    public class RuleVersionResolverTests
    {
        public RuleVersionResolverTests()
        {

        }

        [Fact]
        public void ReturnHighestAssemblyVersionNumberWhenEntryAssemblyIsNull()
        {
            var assemblies = new[]
            {
                typeof(NSubstitute.Arg).Assembly,
                typeof(Xunit.AssemblyTraitAttribute).Assembly,
                typeof(int).Assembly
            };
            var expectedVersion = assemblies.Select(assembly => assembly.GetName().Version).Max();

            var resolver = new RuleVersionResolver(assemblies);
            var actualVersion = resolver.GetVersion();

            Assert.Equal(expectedVersion, actualVersion);
        }

        [Fact]
        public void ThrowExceptionWhenNoAssemblyCanBeFound()
        {
            var resolver = new RuleVersionResolver();

            Assert.Throws<InvalidOperationException>(() => resolver.GetVersion());
        }
    }
}
