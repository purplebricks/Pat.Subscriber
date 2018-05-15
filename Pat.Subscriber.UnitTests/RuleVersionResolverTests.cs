using System;
using System.Linq;
using Pat.Subscriber.SubscriberRules;
using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class RuleVersionResolverTests
    {
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
