using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinCompose
{
    [TestClass]
    public class SequenceTreeTest
    {
        SequenceTree tree = new SequenceTree();

        [TestMethod]
        public void TestUnicodeSequences()
        {
            AssertUnicodeSequence("u0020", " ");
            AssertUnicodeSequence("u0057", "W");
            AssertUnicodeSequence("u0043", "C");
            AssertUnicodeSequence("u00FC", "ü");
            AssertUnicodeSequence("u00fc", "ü");
            AssertShortUnicodeSequence("ufc", "ü");
            AssertShortUnicodeSequence("udc", "Ü");
            AssertUnicodeSequence("u03c0", "π"); // Greek lowercase letter pi
            AssertUnicodeSequence("u0430", "а"); // Cyrillic small letter a
            AssertShortUnicodeSequence("u1e60", "\u1e60"); // Latin capital letter s with dot above
            AssertShortUnicodeSequence("u2013", "–"); // En-dash
            AssertUnicodeSequence("u328c", "\u328c"); // Circled ideograph water
            AssertUnicodeSequence("u4de1", "\u4de1"); // Hexagram for great power
            AssertUnicodeSequence("u5000", "\u5000");
            AssertUnicodeSequence("u6000", "\u6000");
            AssertUnicodeSequence("u7000", "\u7000");
            AssertUnicodeSequence("u8000", "\u8000");
            AssertUnicodeSequence("u9000", "\u9000");
            AssertUnicodeSequence("ua000", "\ua000");
            AssertUnicodeSequence("ub000", "\ub000");
            AssertUnicodeSequence("uc000", "\uc000");
            AssertUnicodeSequence("ud000", "\ud000");
            AssertShortUnicodeSequence("ue000", "\ue000");
            AssertShortUnicodeSequence("uFEFF", "\uFEFF"); // Zero width non-joiner

            AssertUnicodeSequence("u1F600", char.ConvertFromUtf32(0x1F600)); // Grinning face

            AssertInvalidPrefix("x");
            AssertInvalidSequence("u"); // Too short
            AssertInvalidSequence("u3"); // Too short
            AssertInvalidSequence("uf"); // Too short
            AssertInvalidSequence("ud800"); // Part of a surrogate pair
            AssertInvalidSequence("udbff"); // Part of a surrogate pair
            AssertInvalidSequence("udc00"); // Part of a surrogate pair
            AssertInvalidSequence("udfff"); // Part of a surrogate pair
        }

        void AssertValidPrefix(string sequence)
        {
            var keySequence = UnicodeKeySequence(sequence);
            if (!tree.IsValidPrefix(keySequence, false))
                Assert.Fail("Prefix \"" + keySequence + "\" must be valid.");
        }

        void AssertInvalidPrefix(string sequence)
        {
            var keySequence = UnicodeKeySequence(sequence);
            if (tree.IsValidPrefix(keySequence, false))
                Assert.Fail("Prefix \"" + keySequence + "\" must be invalid.");
        }

        void AssertValidSequence(string sequence)
        {
            var keySequence = UnicodeKeySequence(sequence);
            if (!tree.IsValidSequence(keySequence, false))
                Assert.Fail("Sequence \"" + keySequence + "\" must be valid.");
        }

        void AssertInvalidSequence(string sequence)
        {
            var keySequence = UnicodeKeySequence(sequence);
            if (tree.IsValidSequence(keySequence, false))
                Assert.Fail("Sequence \"" + keySequence + "\" must be invalid.");
        }

        void AssertUnicodeSequence(string sequence, string result)
        {
            for (var i = 1; i < sequence.Length - 1; i++)
                AssertValidPrefix(sequence.Substring(0, i));
            AssertInvalidPrefix(sequence);
            AssertValidSequence(sequence);
            Assert.AreEqual(result, tree.GetSequenceResult(UnicodeKeySequence(sequence), false));
        }

        // These sequences could be continued to form a longer Unicode
        // sequence, therefore they are not terminated automatically 
        // but have to be terminated by pressing the Compose key again.
        void AssertShortUnicodeSequence(string sequence, string result)
        {
            for (var i = 1; i < sequence.Length - 1; i++)
                AssertValidPrefix(sequence.Substring(0, i));
            AssertValidSequence(sequence);
            Assert.AreEqual(result, tree.GetSequenceResult(UnicodeKeySequence(sequence), false));
        }

        KeySequence UnicodeKeySequence(string keys)
        {
            var keySequence = new KeySequence();
            foreach (var ch in keys)
                keySequence.Add(Key.FromKeySymOrChar(Convert.ToString(ch)));
            return keySequence;
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
