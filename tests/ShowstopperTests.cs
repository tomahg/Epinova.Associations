using Epinova.Associations;
using NUnit.Framework;

namespace Epinova.AssociationsTests
{
    [TestFixture]
    public class ShowstopperTests
    {
        [Test]
        public void StopShowFor_KnownNumberGiven_IsShowStoppedForReturnsTrueForSameNumber()
        {
            var showstopper = new Showstopper();
            showstopper.StopShowFor(1);
            var result = showstopper.IsShowStoppedFor(1);

            Assert.IsTrue(result);
        }

        [Test]
        public void StopShowFor_KnownNumberGiven_IsShowStoppedForReturnsFalseForDifferentNumber()
        {
            var showstopper = new Showstopper();
            showstopper.StopShowFor(1);
            var result = showstopper.IsShowStoppedFor(2);

            Assert.IsFalse(result);
        }

        [Test]
        public void StartShow_KnownNumberGiven_IsShowStoppedForReturnsFalseForSameNumber()
        {
            var showstopper = new Showstopper();
            showstopper.StopShowFor(1);
            showstopper.StartShow();
            var result = showstopper.IsShowStoppedFor(1);

            Assert.IsFalse(result);
        }
    }
}
