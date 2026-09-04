//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
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
    public class SettingsTest
    {
        // Reloading the rules must publish a NEW tree, not empty and refill the
        // one lookups are already using.
        //
        // This asserts the mechanism rather than the symptom, deliberately. The
        // symptom is a race -- a lookup on the keyboard hook thread landing
        // inside a reload on the UI thread and finding a half-loaded tree -- and
        // a test that tried to hit that window would be timing-dependent and
        // would pass most of the time whether or not the bug was there. What is
        // deterministic is the property that makes the race impossible: a
        // published tree is never mutated, so a reader either sees all of the
        // old one or all of the new one. That is what this pins.
        //
        // It fails against the previous implementation, which called
        // SequenceTree.Clear() on the live tree and reloaded into it, so both
        // calls returned the same instance.
        [TestMethod]
        public void TestReloadPublishesANewTreeRatherThanEmptyingTheOldOne()
        {
            Settings.LoadSequences();
            var first = Settings.GetSequenceList();

            // Guard against the assertions below passing over an empty tree: the
            // rule sets are embedded resources of the wincompose assembly, so
            // they load here even though the on-disk rule files do not.
            Assert.IsTrue(first.Count > 0,
                          "expected the embedded rule resources to load");

            Settings.LoadSequences();
            var second = Settings.GetSequenceList();

            Assert.AreNotSame(first, second,
                              "a reload must publish a new tree, so that a lookup "
                              + "already walking the old one keeps a consistent view");

            // And the tree a reader was holding is still whole afterwards, rather
            // than having been emptied under it.
            Assert.IsTrue(first.Count > 0,
                          "the previously published tree must not be mutated by a reload");
            Assert.AreEqual(first.Count, second.Count,
                            "both loads read the same rules, so they agree on the count");
        }
    }
}
