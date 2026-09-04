//
//  WinCompose - a compose key for Windows - http://wincompose.info/
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinCompose
{
    [TestClass]
    public class UpdaterTest
    {
        [TestMethod]
        public void TestVersionComparisonStopsAtTheFirstDifference()
        {
            // The regression. The comparison must decide on the first
            // component that differs; testing only for "less than" and
            // carrying on let a later component overrule an earlier one, so a
            // 0.10.0 install was offered 0.9.20 as an update.
            Assert.IsFalse(Updater.IsNewerVersion("0.10.0", "0.9.20"));
            Assert.IsFalse(Updater.IsNewerVersion("1.0.0", "0.9.99"));

            // Same shape, and the one a beta would have hit: 1.2.3 sorts above
            // 1.2.3beta1, so it must not be offered it.
            Assert.IsFalse(Updater.IsNewerVersion("1.2.3", "1.2.3beta1"));
        }

        [TestMethod]
        public void TestOrdinaryVersionComparison()
        {
            Assert.IsFalse(Updater.IsNewerVersion("0.9.16", "0.9.16"));
            Assert.IsTrue(Updater.IsNewerVersion("0.9.16", "0.9.17"));
            Assert.IsFalse(Updater.IsNewerVersion("0.9.17", "0.9.16"));
            Assert.IsTrue(Updater.IsNewerVersion("0.9.20", "0.10.0"));

            // Absent components read as zero, so these are the same version.
            Assert.IsFalse(Updater.IsNewerVersion("1.0", "1.0.0.0"));
            Assert.IsTrue(Updater.IsNewerVersion("1.0", "1.0.0.1"));
        }

        [TestMethod]
        public void TestBetaSortsBelowItsReleaseAndAboveThePreviousPatch()
        {
            Assert.IsTrue(Updater.IsNewerVersion("1.2.3beta1", "1.2.3"));
            Assert.IsTrue(Updater.IsNewerVersion("1.2.2.5", "1.2.3beta1"));
            Assert.IsFalse(Updater.IsNewerVersion("1.2.3beta2", "1.2.3beta1"));
        }
    }
}
