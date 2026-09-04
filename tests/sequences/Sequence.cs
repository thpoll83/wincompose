using System;
using System.IO;
using System.Text;
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

        // A sequence may contain the colon key, and keys may follow it. The
        // rule regex used to accept <:> without allowing whitespace after it,
        // so the key group could not continue past a colon and the whole line
        // failed to match -- silently, since a non-match is not logged. Every
        // emoji whose name contains a colon was lost that way: 260 skin-tone
        // and hair variants in Emoji.txt, e.g. "flexed biceps: light skin tone".
        [TestMethod]
        public void TestColonKeyInSequence()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(path,
                    "<Multi_key> <a> <:> <b> : \"X\" # colon between two keys\n" +
                    "<Multi_key> <c> <:> : \"Y\" # colon as the last key\n",
                    Encoding.UTF8);

                var loaded = new SequenceTree();
                loaded.LoadFile(path);

                Assert.AreEqual("X", loaded.GetSequenceResult(Seq("a", ":", "b"), false));
                Assert.AreEqual("Y", loaded.GetSequenceResult(Seq("c", ":"), false));
            }
            finally
            {
                File.Delete(path);
            }
        }

        static KeySequence Seq(params string[] keys)
        {
            var ret = new KeySequence();
            foreach (var k in keys)
                ret.Add(Key.FromKeySymOrChar(k));
            return ret;
        }
    }
}
