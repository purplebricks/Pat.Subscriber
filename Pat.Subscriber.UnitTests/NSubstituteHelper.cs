using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.UnitTests
{
    public static class NSubstituteHelper
    {
        public static void IgnoreAwaitForNSubstituteAssertion(this Task task)
        {

        }
    }
}