using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinCompose
{
    [TestClass]
    public class SequenceTreeTest
    {
        // Unicode entry mode -- Compose u 0 0 5 7 space -> W -- used to live in
        // SequenceTree, and this test used to ask an empty tree about it. It has
        // since moved to Settings.IsValidGenericPrefix / GetGenericSequenceResult,
        // so the test was asking the wrong object about a feature it no longer
        // implements. Nothing noticed because nothing ran the test.
        //
        // The contract moved with it: a codepoint is now completed by a space or
        // Enter rather than being complete on its own.

        [TestMethod]
        public void TestUnicodeInputPrefixes()
        {
            // Every leading fragment of a codepoint keeps the sequence alive.
            Assert.IsTrue(Settings.IsValidGenericPrefix(Chars("u")));
            Assert.IsTrue(Settings.IsValidGenericPrefix(Chars("u0")));
            Assert.IsTrue(Settings.IsValidGenericPrefix(Chars("u0057")));
            Assert.IsTrue(Settings.IsValidGenericPrefix(Chars("u10FFFF")));

            // Nothing else does.
            Assert.IsFalse(Settings.IsValidGenericPrefix(Chars("x")));
            Assert.IsFalse(Settings.IsValidGenericPrefix(Chars("ug")));
            Assert.IsFalse(Settings.IsValidGenericPrefix(Chars("u1234567")));
        }

        [TestMethod]
        public void TestUnicodeInputProducesTheCharacter()
        {
            AssertUnicode("u0020 ", " ");
            AssertUnicode("u0057 ", "W");
            AssertUnicode("u00FC ", "ü");
            AssertUnicode("u00fc ", "ü");   // hex digits are case-insensitive
            AssertUnicode("u03c0 ", "π");   // Greek small letter pi
            AssertUnicode("u0430 ", "а");   // Cyrillic small letter a
            AssertUnicode("u2013 ", "–");   // en dash

            // Above the basic plane, so the result is a surrogate pair.
            AssertUnicode("u1F600 ", char.ConvertFromUtf32(0x1F600));
        }

        [TestMethod]
        public void TestUnicodeInputAcceptsEnterAsTheTerminator()
        {
            var seq = Chars("u0057");
            seq.Add(new Key(VK.RETURN));

            Assert.IsTrue(Settings.GetGenericSequenceResult(seq, out var result));
            Assert.AreEqual("W", result);
        }

        [TestMethod]
        public void TestUnicodeInputRejectsUnusableCodepoints()
        {
            // Half of a surrogate pair is not a character on its own.
            AssertNoUnicode("ud800 ");
            AssertNoUnicode("udfff ");

            // Past the last plane.
            AssertNoUnicode("u110000 ");

            // Not terminated, so not a result yet.
            AssertNoUnicode("u0057");
        }

        static void AssertUnicode(string keys, string expected)
        {
            Assert.IsTrue(Settings.GetGenericSequenceResult(Chars(keys), out var result),
                          "\"" + keys + "\" should produce a character");
            Assert.AreEqual(expected, result);
        }

        static void AssertNoUnicode(string keys)
        {
            Assert.IsFalse(Settings.GetGenericSequenceResult(Chars(keys), out _),
                           "\"" + keys + "\" should not produce a character");
        }

        // A key sequence built one character at a time, the way the composer
        // accumulates one.
        static KeySequence Chars(string keys)
        {
            var ret = new KeySequence();
            foreach (var ch in keys)
                ret.Add(Key.FromKeySymOrChar(ch.ToString()));
            return ret;
        }
    }
}
